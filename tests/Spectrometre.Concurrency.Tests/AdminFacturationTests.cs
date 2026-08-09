using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Modules;
using Spectrometre.Modules.Admin.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

[Collection("Base de données partagée")]
public sealed class AdminFacturationTests(ServiceFixture fixture)
{
    private static ClaimsPrincipal AdminCaller() =>
        new(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, PlatformRoles.Admin), new Claim(ClaimTypes.NameIdentifier, "admin-factu-test")],
            "Test"));

    [Fact]
    public async Task EnregistrerPaiementAsync_PasseStatutActiveEtMetAJourRenewalDate()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var company = await fixture.CreateCompanyAsync($"Factu Co {suffix}", $"factu-owner-{suffix}");

        using (var scope = fixture.Services.CreateScope())
        {
            var core = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var sub = await core.TenantSubscriptions.FirstAsync(s => s.CompanyId == company.Id);
            sub.Status = SubscriptionStatus.Essai;
            sub.RenewalDate = DateTimeOffset.UtcNow.AddDays(-10);
            await core.SaveChangesAsync();
        }

        var periodeFin = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddMonths(1));

        using (var scope = fixture.Services.CreateScope())
        {
            var admin = scope.ServiceProvider.GetRequiredService<IAdminService>();
            await admin.EnregistrerPaiementAsync(
                AdminCaller(),
                ModuleActivationSubjectType.Company,
                company.Id,
                PlanCodes.Standard,
                49m,
                "EUR",
                DateOnly.FromDateTime(DateTime.UtcNow.Date),
                "Virement",
                DateOnly.FromDateTime(DateTime.UtcNow.Date),
                periodeFin);

            var historique = await admin.GetHistoriquePaiementsAsync(
                AdminCaller(), ModuleActivationSubjectType.Company, company.Id, page: 1, pageSize: 50);
            Assert.Contains(historique.Items, p => p.Montant == 49m && p.Moyen == "Virement");
        }

        using (var scope = fixture.Services.CreateScope())
        {
            var core = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var sub = await core.TenantSubscriptions.AsNoTracking().FirstAsync(s => s.CompanyId == company.Id);
            Assert.Equal(SubscriptionStatus.Active, sub.Status);
            Assert.NotNull(sub.RenewalDate);
            Assert.Equal(periodeFin, DateOnly.FromDateTime(sub.RenewalDate.Value.UtcDateTime));
        }
    }

    [Fact]
    public async Task GetAbonnementsEnRetardAsync_InclutActiveAvecRenewalDatePassee()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var company = await fixture.CreateCompanyAsync($"Retard Co {suffix}", $"retard-owner-{suffix}");

        using (var scope = fixture.Services.CreateScope())
        {
            var core = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var sub = await core.TenantSubscriptions.FirstAsync(s => s.CompanyId == company.Id);
            sub.Status = SubscriptionStatus.Active;
            sub.RenewalDate = DateTimeOffset.UtcNow.AddDays(-5);
            await core.SaveChangesAsync();
        }

        using var scope2 = fixture.Services.CreateScope();
        var admin = scope2.ServiceProvider.GetRequiredService<IAdminService>();
        var retard = await admin.GetAbonnementsEnRetardAsync(AdminCaller(), page: 1, pageSize: 50);
        Assert.Contains(retard.Items, a =>
            a.SubjectType == ModuleActivationSubjectType.Company && a.SubjectId == company.Id);
    }

    [Fact]
    public async Task GetAbonnementsFacturationAsync_Pagine_NeRetourneQueLaTailleDemandee()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var companies = new List<int>();
        for (var i = 0; i < 3; i++)
        {
            var company = await fixture.CreateCompanyAsync($"Page Co {suffix}-{i}", $"page-owner-{suffix}-{i}");
            companies.Add(company.Id);
        }

        using var scope = fixture.Services.CreateScope();
        var admin = scope.ServiceProvider.GetRequiredService<IAdminService>();
        var caller = AdminCaller();

        var page1 = await admin.GetAbonnementsFacturationAsync(caller, page: 1, pageSize: 2);
        Assert.Equal(2, page1.Items.Count);
        Assert.True(page1.TotalCount >= 3);
        Assert.Equal(2, page1.PageSize);

        var page2 = await admin.GetAbonnementsFacturationAsync(caller, page: 2, pageSize: 2);
        Assert.True(page2.Items.Count >= 1);

        var keys1 = page1.Items.Select(a => (a.SubjectType, a.SubjectId)).ToHashSet();
        Assert.DoesNotContain(page2.Items, a => keys1.Contains((a.SubjectType, a.SubjectId)));
    }
}
