using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Identity;
using Spectrometre.Modules.Admin.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Vérifie la zone Admin : refus au NIVEAU SERVICE pour un appelant non-administrateur (jamais seulement
/// au niveau routage/affichage — voir la demande d'origine), accès complet pour un administrateur, et le
/// mécanisme de promotion/rétrogradation avec sa garde du dernier administrateur.
/// </summary>
/// <remarks>
/// Le <see cref="ClaimsPrincipal"/> "appelant" est fabriqué directement avec ou sans le rôle
/// <see cref="PlatformRoles.Admin"/> en claim — <c>IAdminService</c> ne vérifie que ce claim (jamais une
/// requête base sur l'appelant lui-même), exactement comme il le ferait avec le principal réel construit
/// par le cookie d'authentification en production. Le compte CIBLE d'une promotion/rétrogradation, lui,
/// doit être un compte réel (voir <see cref="CreateTemporaryUserAsync"/>) puisque ces méthodes appellent
/// vraiment <c>UserManager</c>/<c>RoleManager</c>.
/// </remarks>
[Collection("Base de données partagée")]
public sealed class AdminTests(ServiceFixture fixture)
{
    private static ClaimsPrincipal NonAdminCaller() => new(new ClaimsIdentity([], "Test"));

    private static ClaimsPrincipal AdminCaller() =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.Role, PlatformRoles.Admin)], "Test"));

    private async Task<ApplicationUser> CreateTemporaryUserAsync(IServiceScope scope, string emailPrefix)
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = $"{emailPrefix}-{Guid.NewGuid()}@example.test";
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, "Str0ng!Passw0rd");
        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(e => e.Description)));
        fixture.TrackUserForCleanup(user.Id);
        return user;
    }

    [Fact]
    public async Task AppelantNonAdmin_EstRefuseAuNiveauServicePourToutesLesMethodes()
    {
        using var scope = fixture.Services.CreateScope();
        var adminService = scope.ServiceProvider.GetRequiredService<IAdminService>();
        var caller = NonAdminCaller();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => adminService.GetEntreprisesAsync(caller));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => adminService.GetCandidatsAsync(caller));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => adminService.GetCoachsAsync(caller));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => adminService.GetLiensCoachingAsync(caller));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => adminService.GetInvitationsAsync(caller));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => adminService.GetCompteursGlobauxAsync(caller));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => adminService.RechercherParEmailAsync(caller, "quelqu-un@example.test"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => adminService.GetAdministrateursAsync(caller));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => adminService.PromouvoirAsync(caller, "un-id-quelconque"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => adminService.RetrograderAsync(caller, "un-id-quelconque"));
    }

    [Fact]
    public async Task AppelantAdmin_AccedeAToutesLesVuesDeLectureSansLever()
    {
        using var scope = fixture.Services.CreateScope();
        var adminService = scope.ServiceProvider.GetRequiredService<IAdminService>();
        var caller = AdminCaller();

        await adminService.GetEntreprisesAsync(caller);
        await adminService.GetCandidatsAsync(caller);
        await adminService.GetCoachsAsync(caller);
        await adminService.GetLiensCoachingAsync(caller);
        await adminService.GetInvitationsAsync(caller);
        await adminService.GetCompteursGlobauxAsync(caller);
        await adminService.GetAdministrateursAsync(caller);
        var resultatInexistant = await adminService.RechercherParEmailAsync(caller, $"introuvable-{Guid.NewGuid()}@example.test");
        Assert.Null(resultatInexistant);
    }

    [Fact]
    public async Task RechercherParEmail_TrouveUnCompteExistant_AvecMetadonneesParDefaut()
    {
        using var scope = fixture.Services.CreateScope();
        var adminService = scope.ServiceProvider.GetRequiredService<IAdminService>();
        var user = await CreateTemporaryUserAsync(scope, "recherche");

        var resultat = await adminService.RechercherParEmailAsync(AdminCaller(), user.Email!);

        Assert.NotNull(resultat);
        Assert.Equal(user.Id, resultat!.UserId);
        Assert.False(resultat.EstAdmin);
        Assert.False(resultat.EstCandidat);
        Assert.False(resultat.EstCoach);
        Assert.Empty(resultat.EntreprisesPossedees);
    }

    [Fact]
    public async Task PromouvoirAsync_UtilisateurIntrouvable_RetourneUtilisateurIntrouvable()
    {
        using var scope = fixture.Services.CreateScope();
        var adminService = scope.ServiceProvider.GetRequiredService<IAdminService>();

        var outcome = await adminService.PromouvoirAsync(AdminCaller(), $"id-inexistant-{Guid.NewGuid()}");

        Assert.Equal(AdminActionOutcome.UtilisateurIntrouvable, outcome);
    }

    [Fact]
    public async Task PromotionPuisRetrogradation_DunCompteNonDernierAdmin_Fonctionnent()
    {
        using var scope = fixture.Services.CreateScope();
        var adminService = scope.ServiceProvider.GetRequiredService<IAdminService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var caller = AdminCaller();

        // Deux comptes temporaires promus : quel que soit le nombre d'administrateurs déjà présents dans la
        // base de développement partagée, en retirer UN des deux ne peut jamais ramener le total à zéro
        // (l'autre reste admin) — ce test est donc déterministe indépendamment de l'état ambiant.
        var compteA = await CreateTemporaryUserAsync(scope, "promo-a");
        var compteB = await CreateTemporaryUserAsync(scope, "promo-b");

        Assert.Equal(AdminActionOutcome.Succes, await adminService.PromouvoirAsync(caller, compteA.Id));
        Assert.Equal(AdminActionOutcome.Succes, await adminService.PromouvoirAsync(caller, compteB.Id));
        Assert.True(await userManager.IsInRoleAsync(compteA, PlatformRoles.Admin));
        Assert.True(await userManager.IsInRoleAsync(compteB, PlatformRoles.Admin));

        // Déjà admin : la seconde promotion du même compte est un no-op signalé, pas une erreur.
        Assert.Equal(AdminActionOutcome.DejaAdmin, await adminService.PromouvoirAsync(caller, compteA.Id));

        Assert.Equal(AdminActionOutcome.Succes, await adminService.RetrograderAsync(caller, compteA.Id));
        Assert.False(await userManager.IsInRoleAsync(compteA, PlatformRoles.Admin));

        // Rétrograder un compte qui ne l'est déjà plus.
        Assert.Equal(AdminActionOutcome.PasAdmin, await adminService.RetrograderAsync(caller, compteA.Id));

        // Nettoyage explicite de compteB (toujours admin à ce stade) — TrackUserForCleanup supprime le
        // compte Identity en fin de suite quel que soit son rôle courant, donc rien d'autre à faire ici.
    }

    [Fact]
    public async Task Retrogradation_QuandLeCompteEstLeDernierAdministrateurGlobal_EstRefusee()
    {
        using var scope = fixture.Services.CreateScope();
        var adminService = scope.ServiceProvider.GetRequiredService<IAdminService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var caller = AdminCaller();

        var initialAdminCount = (await userManager.GetUsersInRoleAsync(PlatformRoles.Admin)).Count;

        var compte = await CreateTemporaryUserAsync(scope, "dernier-admin");
        Assert.Equal(AdminActionOutcome.Succes, await adminService.PromouvoirAsync(caller, compte.Id));

        var outcome = await adminService.RetrograderAsync(caller, compte.Id);

        // Si la base ne comptait déjà aucun administrateur avant ce test (cas normal sur une base de
        // développement fraîchement nettoyée, voir le rapport), ce compte EST réellement le dernier
        // restant : la garde doit refuser. Si d'autres administrateurs existaient déjà (ex. un compte
        // bootstrap réel laissé en place), le retirer reste sûr : les deux branches sont vérifiées pour que
        // ce test reste correct — et non fragile — quel que soit l'état ambiant de la base partagée.
        if (initialAdminCount == 0)
        {
            Assert.Equal(AdminActionOutcome.DernierAdminRestant, outcome);
            Assert.True(await userManager.IsInRoleAsync(compte, PlatformRoles.Admin));
        }
        else
        {
            Assert.Equal(AdminActionOutcome.Succes, outcome);
            Assert.False(await userManager.IsInRoleAsync(compte, PlatformRoles.Admin));
        }
    }
}
