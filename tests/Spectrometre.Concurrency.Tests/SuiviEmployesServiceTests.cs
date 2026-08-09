using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.SuiviEmployes.Data;
using Spectrometre.Modules.SuiviEmployes.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Socle SuiviEmployes : cache IA par hash + constante seuil critique.
/// </summary>
public sealed class SuiviEmployesServiceTests(ServiceFixture fixture) : IClassFixture<ServiceFixture>
{
    [Fact]
    public async Task GenererAnalyseEmploye_CacheParHash_NeRegenerePasSansChangement()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var ownerUserId = $"suivi-owner-{suffix}";
        var company = await fixture.CreateCompanyAsync($"Entreprise Suivi Cache {suffix}", ownerUserId);

        await using var scope = fixture.Services.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var tenant = sp.GetRequiredService<ITenantContext>();
        var suiviFactory = sp.GetRequiredService<IDbContextFactory<SuiviEmployesDbContext>>();
        var core = sp.GetRequiredService<Spectrometre.Core.Data.CoreDbContext>();
        var userManager = sp.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Spectrometre.Core.Identity.ApplicationUser>>();
        var posteService = sp.GetRequiredService<Spectrometre.Modules.ProfilEntreprise.Services.IPosteService>();

        tenant.SetActiveCompany(company.Id, company.SchemaName);

        var link = new UserCompanyLink
        {
            UserId = $"suivi-emp-{suffix}",
            CompanyId = company.Id,
            Role = CompanyRole.Employe,
            PosteId = null,
        };
        core.UserCompanyLinks.Add(link);
        await core.SaveChangesAsync();

        var calls = 0;
        var fakeIa = new CountingAnalyseIa(() => Interlocked.Increment(ref calls));
        var service = new SuiviEmployesService(
            suiviFactory,
            tenant,
            core,
            userManager,
            posteService,
            fakeIa,
            sp.GetRequiredService<Spectrometre.Core.Notifications.INotificationService>());

        var first = await service.GenererAnalyseEmployeAsync(link.Id, forcerRegeneration: false);
        var second = await service.GenererAnalyseEmployeAsync(link.Id, forcerRegeneration: false);

        Assert.False(string.IsNullOrWhiteSpace(first.AnalyseMarkdown));
        Assert.Equal(1, calls);
        Assert.Equal(first.AnalyseMarkdown, second.AnalyseMarkdown);
        Assert.Equal(1, calls);

        _ = await service.GenererAnalyseEmployeAsync(link.Id, forcerRegeneration: true);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void SeuilCritique_Constantes_Definiees()
    {
        Assert.Equal(40, SuiviEmployesService.SeuilCritiqueNote);
        Assert.Equal(3, SuiviEmployesService.SeuilCritiqueConsecutive);
    }

    private sealed class CountingAnalyseIa(Func<int> onCall) : IAnalyseEmployeIaService
    {
        public Task<(string? Output, string? Error)> GenererTexteAsync(
            string systemPrompt,
            string userPrompt,
            CancellationToken cancellationToken = default)
        {
            onCall();
            return Task.FromResult<(string?, string?)>(("Analyse de test", null));
        }
    }
}
