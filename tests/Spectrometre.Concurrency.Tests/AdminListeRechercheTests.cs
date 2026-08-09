using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Invitations;
using Spectrometre.Core.Modules;
using Spectrometre.Modules.Admin.Services;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.ProfilCandidat.Services;
using Spectrometre.Modules.ProfilCoach.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>Recherche serveur sur les listes Admin — filtre réellement appliqué avant pagination.</summary>
[Collection("Base de données partagée")]
public sealed class AdminListeRechercheTests(ServiceFixture fixture)
{
    private static ClaimsPrincipal AdminCaller() =>
        new(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, PlatformRoles.Admin), new Claim(ClaimTypes.NameIdentifier, "admin-recherche-test")],
            "Test"));

    private async Task<ApplicationUser> CreateTemporaryUserAsync(IServiceScope scope, string emailPrefix)
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = $"{emailPrefix}-{Guid.NewGuid():N}@example.test";
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, "Str0ng!Passw0rd");
        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(e => e.Description)));
        fixture.TrackUserForCleanup(user.Id);
        return user;
    }

    [Fact]
    public async Task GetEntreprisesAsync_RechercheParNom_TrouveEtExclutLeBruit()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var uniqueName = $"Recherche Ent Co {suffix}";
        var company = await fixture.CreateCompanyAsync(uniqueName, $"recherche-ent-owner-{suffix}");

        using var scope = fixture.Services.CreateScope();
        var admin = scope.ServiceProvider.GetRequiredService<IAdminService>();
        var caller = AdminCaller();

        var found = await admin.GetEntreprisesAsync(caller, page: 1, pageSize: 50, recherche: suffix);
        Assert.Contains(found.Items, c => c.Id == company.Id && c.Name == uniqueName);

        var garbage = await admin.GetEntreprisesAsync(caller, page: 1, pageSize: 50, recherche: $"zzz-no-match-{Guid.NewGuid():N}");
        Assert.DoesNotContain(garbage.Items, c => c.Id == company.Id);
    }

    [Fact]
    public async Task GetCandidatsAsync_RechercheParEmail_TrouveLeProfil()
    {
        using var scope = fixture.Services.CreateScope();
        var admin = scope.ServiceProvider.GetRequiredService<IAdminService>();
        var candidates = scope.ServiceProvider.GetRequiredService<ICandidateProfileService>();
        var caller = AdminCaller();

        var user = await CreateTemporaryUserAsync(scope, "recherche-candidat");
        var profileId = await candidates.GetOrCreateProfileIdAsync(user.Id);

        var fragment = user.Email!.Split('@')[0];
        var found = await admin.GetCandidatsAsync(caller, page: 1, pageSize: 50, recherche: fragment);
        Assert.Contains(found.Items, c => c.CandidateProfileId == profileId && c.UserId == user.Id);

        var garbage = await admin.GetCandidatsAsync(caller, page: 1, pageSize: 50, recherche: $"zzz-no-match-{Guid.NewGuid():N}");
        Assert.DoesNotContain(garbage.Items, c => c.CandidateProfileId == profileId);
    }

    [Fact]
    public async Task GetCoachsAsync_RechercheParNomAffiche_TrouveLeProfil()
    {
        using var scope = fixture.Services.CreateScope();
        var admin = scope.ServiceProvider.GetRequiredService<IAdminService>();
        var coaches = scope.ServiceProvider.GetRequiredService<ICoachProfileService>();
        var caller = AdminCaller();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var nom = $"Coach Recherche {suffix}";
        var user = await CreateTemporaryUserAsync(scope, "recherche-coach");
        await coaches.SaveProfilAsync(user.Id, nom, "Bio", "stress", visibleDansAnnuaire: true);

        var found = await admin.GetCoachsAsync(caller, page: 1, pageSize: 50, recherche: suffix);
        Assert.Contains(found.Items, c => c.UserId == user.Id && c.NomAffiche == nom);
        Assert.NotNull(found.Items.First(c => c.UserId == user.Id).ModulesActifs);

        var garbage = await admin.GetCoachsAsync(caller, page: 1, pageSize: 50, recherche: $"zzz-no-match-{Guid.NewGuid():N}");
        Assert.DoesNotContain(garbage.Items, c => c.UserId == user.Id);
    }

    [Fact]
    public async Task GetInvitationsAsync_RechercheParEmailInvite_Trouve()
    {
        using var scope = fixture.Services.CreateScope();
        var admin = scope.ServiceProvider.GetRequiredService<IAdminService>();
        var invitations = scope.ServiceProvider.GetRequiredService<IInvitationService>();
        var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var caller = AdminCaller();

        var emetteur = await CreateTemporaryUserAsync(scope, "recherche-inv-emetteur");
        var emailInvite = $"invite-recherche-{Guid.NewGuid():N}@example.test";
        var invitation = await invitations.CreerAsync(emetteur.Id, emailInvite, InvitationType.Coaching, contextId: null, coreDb);

        var fragment = emailInvite.Split('@')[0];
        var found = await admin.GetInvitationsAsync(caller, page: 1, pageSize: 50, recherche: fragment);
        Assert.Contains(found.Items, i => i.Id == invitation.Id && i.EmailInvite == emailInvite.ToLowerInvariant());

        var garbage = await admin.GetInvitationsAsync(caller, page: 1, pageSize: 50, recherche: $"zzz-no-match-{Guid.NewGuid():N}");
        Assert.DoesNotContain(garbage.Items, i => i.Id == invitation.Id);
    }

    [Fact]
    public async Task GetAdministrateursAsync_RechercheParEmail_FiltreEnMemoire()
    {
        using var scope = fixture.Services.CreateScope();
        var admin = scope.ServiceProvider.GetRequiredService<IAdminService>();
        var caller = AdminCaller();

        var user = await CreateTemporaryUserAsync(scope, "recherche-admin");
        Assert.Equal(AdminActionOutcome.Succes, await admin.PromouvoirAsync(caller, user.Id));

        var fragment = user.Email!.Split('@')[0];
        var found = await admin.GetAdministrateursAsync(caller, page: 1, pageSize: 50, recherche: fragment);
        Assert.Contains(found.Items, a => a.UserId == user.Id);

        var garbage = await admin.GetAdministrateursAsync(caller, page: 1, pageSize: 50, recherche: $"zzz-no-match-{Guid.NewGuid():N}");
        Assert.DoesNotContain(garbage.Items, a => a.UserId == user.Id);
    }

    [Fact]
    public async Task GetAbonnementsFacturationAsync_RechercheParNomEntreprise_Trouve()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var name = $"Factu Recherche Co {suffix}";
        var company = await fixture.CreateCompanyAsync(name, $"factu-recherche-owner-{suffix}");

        using var scope = fixture.Services.CreateScope();
        var admin = scope.ServiceProvider.GetRequiredService<IAdminService>();
        var caller = AdminCaller();

        var found = await admin.GetAbonnementsFacturationAsync(caller, page: 1, pageSize: 50, recherche: suffix);
        Assert.Contains(found.Items, a =>
            a.SubjectType == ModuleActivationSubjectType.Company && a.SubjectId == company.Id);

        var garbage = await admin.GetAbonnementsFacturationAsync(caller, page: 1, pageSize: 50, recherche: $"zzz-no-match-{Guid.NewGuid():N}");
        Assert.DoesNotContain(garbage.Items, a =>
            a.SubjectType == ModuleActivationSubjectType.Company && a.SubjectId == company.Id);
    }

    [Fact]
    public async Task GetAbonnementsEnRetardAsync_RechercheVide_Fonctionne_EtGarbageVide()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var company = await fixture.CreateCompanyAsync($"Retard Recherche Co {suffix}", $"retard-recherche-owner-{suffix}");

        using (var scope = fixture.Services.CreateScope())
        {
            var core = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var sub = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .FirstAsync(core.TenantSubscriptions, s => s.CompanyId == company.Id);
            sub.Status = Spectrometre.Core.Billing.SubscriptionStatus.Active;
            sub.RenewalDate = DateTimeOffset.UtcNow.AddDays(-3);
            await core.SaveChangesAsync();
        }

        using var scope2 = fixture.Services.CreateScope();
        var admin = scope2.ServiceProvider.GetRequiredService<IAdminService>();
        var caller = AdminCaller();

        var sansFiltre = await admin.GetAbonnementsEnRetardAsync(caller, page: 1, pageSize: 50);
        Assert.Contains(sansFiltre.Items, a =>
            a.SubjectType == ModuleActivationSubjectType.Company && a.SubjectId == company.Id);

        var avecNom = await admin.GetAbonnementsEnRetardAsync(caller, page: 1, pageSize: 50, recherche: suffix);
        Assert.Contains(avecNom.Items, a =>
            a.SubjectType == ModuleActivationSubjectType.Company && a.SubjectId == company.Id);

        var garbage = await admin.GetAbonnementsEnRetardAsync(caller, page: 1, pageSize: 50, recherche: $"zzz-no-match-{Guid.NewGuid():N}");
        Assert.Empty(garbage.Items);
    }

    [Fact]
    public async Task GetLiensCoachingAsync_SansRecherche_Fonctionne_EtGarbageSansMatch()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var suiviUserId = $"lien-recherche-suivi-{suffix}";
        var coachUserId = $"lien-recherche-coach-{suffix}";

        using (var scope = fixture.Services.CreateScope())
        {
            var coaches = scope.ServiceProvider.GetRequiredService<ICoachProfileService>();
            await coaches.SaveProfilAsync(coachUserId, $"Coach Lien {suffix}", "Bio", "stress", visibleDansAnnuaire: true);
            var coaching = scope.ServiceProvider.GetRequiredService<ICoachingService>();
            await coaching.DemanderCoachDepuisAnnuaireAsync(suiviUserId, coachUserId);
        }

        using var scope2 = fixture.Services.CreateScope();
        var admin = scope2.ServiceProvider.GetRequiredService<IAdminService>();
        var caller = AdminCaller();

        var sansFiltre = await admin.GetLiensCoachingAsync(caller, page: 1, pageSize: 50);
        Assert.True(sansFiltre.TotalCount >= 1);

        var garbage = await admin.GetLiensCoachingAsync(caller, page: 1, pageSize: 50, recherche: $"zzz-no-match-{Guid.NewGuid():N}");
        Assert.Equal(0, garbage.TotalCount);
        Assert.Empty(garbage.Items);
    }
}
