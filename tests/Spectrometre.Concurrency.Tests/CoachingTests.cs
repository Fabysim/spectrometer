using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Data;
using Spectrometre.Core.Invitations;
using Spectrometre.Core.Modules;
using Spectrometre.Modules.Coaching.Entities;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.GestionDuTemps.Services;
using Spectrometre.Modules.ProfilCoach.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Parcours complets et contrôle d'accès du module Coaching. Comme <c>CompatibiliteAccessControlTests</c>,
/// couvre les trois cas de la règle d'accès sur <see cref="ICoachingService.GetSuiviUserIdSiAutoriseAsync"/>
/// (même rôle que <c>ICompatibiliteService.GetResultatAutorisePourUtilisateurAsync</c>).
/// </summary>
/// <remarks>
/// Ne teste PAS la partie HTTP/Identity de l'inscription (création de compte via
/// <c>InvitationAcceptancePage</c>/UserManager) : <see cref="ServiceFixture"/> n'enregistre volontairement
/// pas ASP.NET Core Identity (voir sa remarque), exactement la même limite déjà en place pour
/// <c>CandidateOnboardingService</c>/<c>CompanyOnboardingService</c> — jamais exercés par cette suite non
/// plus. Le parcours "invitation par email" ci-dessous exerce donc directement la couche service
/// (IInvitationService + ICoachingService.FinaliserDepuisInvitationAsync) avec un UserId de compte déjà
/// résolu, ce qui est exactement ce que fait InvitationAcceptancePage une fois le compte créé/authentifié —
/// seule la création du compte lui-même (UserManager.CreateAsync) est hors de portée ici.
/// </remarks>
[Collection("Base de données partagée")]
public sealed class CoachingTests(ServiceFixture fixture)
{
    private IServiceScope NewScope() => fixture.Services.CreateScope();

    [Fact]
    public async Task ParcoursAnnuaire_DemandeAccepteeParLeCoach_DonneUnAccesComplet()
    {
        var suffix = Guid.NewGuid();
        var suiviUserId = $"coaching-annuaire-suivi-{suffix}";
        var coachUserId = $"coaching-annuaire-coach-{suffix}";

        using (var scope = NewScope())
        {
            // Le coach doit avoir un profil visible dans l'annuaire pour être trouvable.
            var coachProfileService = scope.ServiceProvider.GetRequiredService<ICoachProfileService>();
            await coachProfileService.SaveProfilAsync(coachUserId, "Coach Test", "Bio", "gestion du stress", visibleDansAnnuaire: true);

            // La personne suivie renseigne un peu de données Gestion du temps, pour vérifier plus bas
            // que le coach y a réellement accès une fois le lien actif.
            var gdt = scope.ServiceProvider.GetRequiredService<IGestionDuTempsService>();
            await gdt.GetOrCreateCycleActifAsync(suiviUserId);
        }

        using (var scope = NewScope())
        {
            var coachingService = scope.ServiceProvider.GetRequiredService<ICoachingService>();

            // Toujours la personne suivie qui initie — jamais le coach.
            var demandeCreee = await coachingService.DemanderCoachDepuisAnnuaireAsync(suiviUserId, coachUserId);
            Assert.True(demandeCreee);

            var liensCoach = await coachingService.GetLiensPourCoachAsync(coachUserId);
            var lien = Assert.Single(liensCoach);
            Assert.Equal(LienCoachingStatut.EnAttente, lien.Statut);

            // Avant acceptation : aucun accès, même si la demande existe.
            Assert.Null(await coachingService.GetSuiviUserIdSiAutoriseAsync(suiviUserId, coachUserId));

            var accepte = await coachingService.AccepterAsync(lien.Id, coachUserId);
            Assert.True(accepte);
        }

        using (var scope = NewScope())
        {
            var coachingService = scope.ServiceProvider.GetRequiredService<ICoachingService>();
            var coachingAccessChecker = scope.ServiceProvider.GetRequiredService<ICoachingAccessChecker>();
            var gdt = scope.ServiceProvider.GetRequiredService<IGestionDuTempsService>();

            var autorise = await coachingAccessChecker.GetSuiviUserIdSiAutoriseAsync(suiviUserId, coachUserId);
            Assert.Equal(suiviUserId, autorise);

            // Accès complet en lecture aux données Gestion du temps de la personne suivie, via l'identifiant
            // RETOURNÉ par l'accesseur (jamais construit à la main) — cycles, comme listé à l'étape 4 du cycle.
            var cycle = await gdt.GetOrCreateCycleActifAsync(autorise!);
            Assert.Equal(1, cycle.NumeroCycle);

            // Révocation par la personne suivie — immédiatement effective.
            var liensSuivi = await coachingService.GetLiensPourSuiviAsync(suiviUserId);
            var lienActif = Assert.Single(liensSuivi);
            var revoque = await coachingService.RevoquerAsync(lienActif.Id, suiviUserId);
            Assert.True(revoque);

            Assert.Null(await coachingAccessChecker.GetSuiviUserIdSiAutoriseAsync(suiviUserId, coachUserId));
        }
    }

    [Fact]
    public async Task ParcoursInvitationParEmail_FinalisationActiveImmediatementLeLien()
    {
        var suffix = Guid.NewGuid();
        var suiviUserId = $"coaching-invitation-suivi-{suffix}";
        var emailInvite = $"coach-invite-{suffix}@example.test";
        // Représente l'UserId résolu par InvitationAcceptancePage une fois le compte créé/authentifié —
        // voir la remarque de la classe : la création du compte elle-même n'est pas exercée ici.
        var accepteurUserId = $"coaching-invitation-coach-{suffix}";

        using var scope = NewScope();
        var invitationService = scope.ServiceProvider.GetRequiredService<IInvitationService>();
        var coachingService = scope.ServiceProvider.GetRequiredService<ICoachingService>();
        var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var invitation = await coachingService.InviterCoachParEmailAsync(suiviUserId, emailInvite);
        Assert.Equal(InvitationType.Coaching, invitation.Type);
        Assert.Equal(InvitationStatus.EnAttente, invitation.Statut);

        var valide = await invitationService.ObtenirValidePourAcceptationAsync(invitation.Token, coreDb);
        Assert.NotNull(valide);

        // Confirmer l'invitation EST l'acceptation — pas d'étape supplémentaire, le lien est actif direct.
        var lien = await coachingService.FinaliserDepuisInvitationAsync(valide!, accepteurUserId);
        Assert.Equal(LienCoachingStatut.Actif, lien.Statut);
        await invitationService.MarquerAccepteeAsync(invitation.Id, coreDb);

        Assert.Equal(suiviUserId, await coachingService.GetSuiviUserIdSiAutoriseAsync(suiviUserId, accepteurUserId));

        // Une invitation déjà acceptée ne peut plus être réutilisée pour finaliser un second lien.
        Assert.Null(await invitationService.ObtenirValidePourAcceptationAsync(invitation.Token, coreDb));
    }

    [Fact]
    public async Task LienActif_DonneAcces()
    {
        var suffix = Guid.NewGuid();
        var suiviUserId = $"coaching-access-actif-suivi-{suffix}";
        var coachUserId = $"coaching-access-actif-coach-{suffix}";

        using var scope = NewScope();
        var coachingService = scope.ServiceProvider.GetRequiredService<ICoachingService>();

        await coachingService.DemanderCoachDepuisAnnuaireAsync(suiviUserId, coachUserId);
        var lien = Assert.Single(await coachingService.GetLiensPourCoachAsync(coachUserId));
        await coachingService.AccepterAsync(lien.Id, coachUserId);

        var autorise = await coachingService.GetSuiviUserIdSiAutoriseAsync(suiviUserId, coachUserId);

        Assert.Equal(suiviUserId, autorise);
    }

    [Fact]
    public async Task LienEnAttenteOuRevoque_EstRefuse()
    {
        var suffix = Guid.NewGuid();
        var suiviUserId = $"coaching-access-attente-suivi-{suffix}";
        var coachUserId = $"coaching-access-attente-coach-{suffix}";

        using var scope = NewScope();
        var coachingService = scope.ServiceProvider.GetRequiredService<ICoachingService>();

        await coachingService.DemanderCoachDepuisAnnuaireAsync(suiviUserId, coachUserId);

        // Cas 1 : en attente, jamais accepté.
        Assert.Null(await coachingService.GetSuiviUserIdSiAutoriseAsync(suiviUserId, coachUserId));

        // Cas 2 : accepté puis révoqué.
        var lien = Assert.Single(await coachingService.GetLiensPourCoachAsync(coachUserId));
        await coachingService.AccepterAsync(lien.Id, coachUserId);
        Assert.NotNull(await coachingService.GetSuiviUserIdSiAutoriseAsync(suiviUserId, coachUserId));

        await coachingService.RevoquerAsync(lien.Id, suiviUserId);
        Assert.Null(await coachingService.GetSuiviUserIdSiAutoriseAsync(suiviUserId, coachUserId));
    }

    [Fact]
    public async Task AucunLien_EstRefuse()
    {
        var suffix = Guid.NewGuid();
        var suiviUserId = $"coaching-access-aucun-suivi-{suffix}";
        var tiersUserId = $"coaching-access-aucun-tiers-{suffix}";

        using var scope = NewScope();
        var coachingService = scope.ServiceProvider.GetRequiredService<ICoachingService>();

        // tiersUserId n'a jamais eu la moindre demande ni invitation vers suiviUserId.
        var result = await coachingService.GetSuiviUserIdSiAutoriseAsync(suiviUserId, tiersUserId);

        Assert.Null(result);
    }

    [Fact]
    public async Task UnUtilisateurSansCoach_ContinueDeFonctionnerNormalement()
    {
        // Non-régression : l'existence du module Coaching ne doit rien changer pour un utilisateur de
        // Gestion du temps qui n'a jamais interagi avec un coach, ni comme suivi ni comme coach.
        var suffix = Guid.NewGuid();
        var userId = $"coaching-non-regression-{suffix}";

        using var scope = NewScope();
        var gdt = scope.ServiceProvider.GetRequiredService<IGestionDuTempsService>();
        var coachingAccessChecker = scope.ServiceProvider.GetRequiredService<ICoachingAccessChecker>();

        await gdt.GetOrCreateCycleActifAsync(userId);
        var types = await gdt.GetTypesDeTempsAsync(userId);
        Assert.Equal(6, types.Count);

        var unTiersQuelconque = $"coaching-non-regression-tiers-{suffix}";
        Assert.Null(await coachingAccessChecker.GetSuiviUserIdSiAutoriseAsync(userId, unTiersQuelconque));
    }

    [Fact]
    public async Task GenererAnamneseAsync_RequiertUnLienActif_EtProduitUnRepliSansAppelReseau()
    {
        var suffix = Guid.NewGuid();
        var suiviUserId = $"coaching-anamnese-suivi-{suffix}";
        var coachUserId = $"coaching-anamnese-coach-{suffix}";
        var tiersUserId = $"coaching-anamnese-tiers-{suffix}";

        using var scope = NewScope();
        var coachingService = scope.ServiceProvider.GetRequiredService<ICoachingService>();

        await coachingService.DemanderCoachDepuisAnnuaireAsync(suiviUserId, coachUserId);
        var lien = Assert.Single(await coachingService.GetLiensPourCoachAsync(coachUserId));

        // Refusé pour un lien encore en attente.
        Assert.Null(await coachingService.GenererAnamneseAsync(lien.Id, coachUserId));

        await coachingService.AccepterAsync(lien.Id, coachUserId);

        // Refusé pour un tiers qui n'est pas le coach de ce lien.
        Assert.Null(await coachingService.GenererAnamneseAsync(lien.Id, tiersUserId));

        // FakeAiSynthesisService (voir ServiceFixture) ne renvoie jamais de contenu par défaut — jamais
        // d'appel réseau réel à Replicate — donc le texte de repli algorithmique est utilisé.
        var anamnese = await coachingService.GenererAnamneseAsync(lien.Id, coachUserId);
        Assert.NotNull(anamnese);
        Assert.False(anamnese!.GenereeParIa);
        Assert.False(string.IsNullOrWhiteSpace(anamnese.Contenu));
    }
}
