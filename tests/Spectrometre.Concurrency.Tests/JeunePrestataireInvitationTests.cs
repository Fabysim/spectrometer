using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Invitations;
using Spectrometre.Core.JeunesPrestataires;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Notifications;
using Spectrometre.Modules.Coaching.Entities;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.GestionDuTemps.Services;
using Spectrometre.Modules.JeunesPrestataires.Entities;
using Spectrometre.Modules.JeunesPrestataires.Services;
using Spectrometre.Modules.Missions.Services;
using Spectrometre.Modules.ProfilCandidat.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

[Collection("Base de données partagée")]
public sealed class JeunePrestataireInvitationTests(ServiceFixture fixture)
{
    [Fact]
    public async Task InviterEtAccepter_CreeProfilJeuneEtLienCoachActif_SensInverseDeCoaching()
    {
        var (coachUserId, jeuneUserId, profile) = await InviterEtAccepterAsync(
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)));

        var coachingService = fixture.Services.GetRequiredService<ICoachingService>();
        var liens = await coachingService.GetLiensPourCoachAsync(coachUserId);
        var lien = liens.Single(l => l.SuiviUserId == jeuneUserId);

        Assert.Equal(coachUserId, lien.CoachUserId);
        Assert.Equal(jeuneUserId, lien.SuiviUserId);
        Assert.Equal(LienCoachingStatut.Actif, lien.Statut);
        Assert.Equal("Dupont", profile.Nom);
        Assert.Equal("Léa", profile.Prenoms);
        Assert.Equal(ProfilAccompagnement.SansExperience, profile.ProfilAccompagnement);
    }

    [Fact]
    public async Task FinaliserJeunePrestataire_RefuseUnSecondCoachActif_PremierLienInchange()
    {
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();
        var coachingService = fixture.Services.GetRequiredService<ICoachingService>();

        var (coach1UserId, jeuneUserId, _) = await InviterEtAccepterAsync(
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)));

        var liensAvant = await coachingService.GetLiensPourSuiviAsync(jeuneUserId);
        var actifAvant = Assert.Single(liensAvant, l => l.Statut == LienCoachingStatut.Actif);
        Assert.Equal(coach1UserId, actifAvant.CoachUserId);

        var coach2UserId = await CreerUtilisateurAsync($"coach2-{Guid.NewGuid()}@test.local");
        var jeune = await jeuneService.TryGetByUserIdAsync(jeuneUserId);
        Assert.NotNull(jeune);

        var invite2 = await jeuneService.InviterJeuneAsync(
            coach2UserId,
            $"jeune-relais-{Guid.NewGuid()}@test.local",
            "Dupont",
            "Léa",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)),
            "http://localhost");
        Assert.True(invite2.Success);

        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();
        var invitation2 = await coreDb.Invitations.FirstAsync(i => i.Id == invite2.Invitation!.Id);

        var lien2 = await coachingService.FinaliserJeunePrestataireDepuisInvitationAsync(invitation2, jeuneUserId);
        Assert.Null(lien2);

        var liensApres = await coachingService.GetLiensPourSuiviAsync(jeuneUserId);
        Assert.Single(liensApres, l => l.Statut == LienCoachingStatut.Actif);
        Assert.Equal(coach1UserId, liensApres.Single(l => l.Statut == LienCoachingStatut.Actif).CoachUserId);
        Assert.DoesNotContain(liensApres, l => l.CoachUserId == coach2UserId && l.Statut == LienCoachingStatut.Actif);
    }

    [Fact]
    public async Task TransfererJeunePrestataire_Immediat_ClotureAncien_OuvreNouveau_UnSeulActif()
    {
        var coachingService = fixture.Services.GetRequiredService<ICoachingService>();
        var fiche = fixture.Services.GetRequiredService<IFicheSuiviCoachService>();
        var notifService = fixture.Services.GetRequiredService<INotificationService>();

        var (coach1UserId, jeuneUserId, _) = await InviterEtAccepterAsync(
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)));
        var coach2UserId = await CreerUtilisateurAsync($"coach-cible-{Guid.NewGuid()}@test.local");
        var etrangerUserId = await CreerUtilisateurAsync($"coach-etranger-{Guid.NewGuid()}@test.local");

        Assert.False(await coachingService.TransfererJeunePrestataireAsync(etrangerUserId, jeuneUserId, coach2UserId));
        Assert.False(await coachingService.TransfererJeunePrestataireAsync(coach1UserId, jeuneUserId, coach1UserId));

        Assert.Equal(jeuneUserId, await coachingService.GetSuiviUserIdSiAutoriseAsync(jeuneUserId, coach1UserId));
        Assert.Null(await coachingService.GetSuiviUserIdSiAutoriseAsync(jeuneUserId, coach2UserId));

        Assert.True(await coachingService.TransfererJeunePrestataireAsync(coach1UserId, jeuneUserId, coach2UserId));

        Assert.Null(await coachingService.GetSuiviUserIdSiAutoriseAsync(jeuneUserId, coach1UserId));
        Assert.Equal(jeuneUserId, await coachingService.GetSuiviUserIdSiAutoriseAsync(jeuneUserId, coach2UserId));
        Assert.Null(await fiche.GetAsync(coach1UserId, jeuneUserId));
        Assert.NotNull(await fiche.GetAsync(coach2UserId, jeuneUserId));

        var liens = await coachingService.GetLiensPourSuiviAsync(jeuneUserId);
        Assert.Single(liens, l => l.Statut == LienCoachingStatut.Actif);
        var actif = liens.Single(l => l.Statut == LienCoachingStatut.Actif);
        Assert.Equal(coach2UserId, actif.CoachUserId);
        var ancien = Assert.Single(liens, l => l.CoachUserId == coach1UserId);
        Assert.Equal(LienCoachingStatut.Revoque, ancien.Statut);

        var notifsJeune = await notifService.GetRecentesAsync(jeuneUserId, 10);
        Assert.Contains(notifsJeune, n => n.TypeCode == "Coaching.JeuneTransfere");
        var notifsCible = await notifService.GetRecentesAsync(coach2UserId, 10);
        Assert.Contains(notifsCible, n => n.TypeCode == "Coaching.JeuneRecuParTransfert");
    }

    [Fact]
    public async Task DemanderCoachDepuisAnnuaire_JeuneAvecCoachActif_RefuseUnSecond_CandidatClassiquePeut()
    {
        var coachingService = fixture.Services.GetRequiredService<ICoachingService>();
        var (coach1UserId, jeuneUserId, _) = await InviterEtAccepterAsync(
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)));
        var coach2UserId = await CreerUtilisateurAsync($"coach-annuaire-{Guid.NewGuid()}@test.local");

        Assert.False(await coachingService.DemanderCoachDepuisAnnuaireAsync(jeuneUserId, coach2UserId));
        var liensJeune = await coachingService.GetLiensPourSuiviAsync(jeuneUserId);
        Assert.DoesNotContain(liensJeune, l => l.CoachUserId == coach2UserId);

        var candidatUserId = await CreerUtilisateurAsync($"candidat-multi-{Guid.NewGuid()}@test.local");
        Assert.True(await coachingService.DemanderCoachDepuisAnnuaireAsync(candidatUserId, coach1UserId));
        Assert.True(await coachingService.DemanderCoachDepuisAnnuaireAsync(candidatUserId, coach2UserId));
    }

    [Fact]
    public async Task InviterJeune_Majeur_InvitationEtAcceptationReussissent()
    {
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();

        var (_, _, profile) = await InviterEtAccepterAsync(
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-20)));

        Assert.False(jeuneService.EstMineur(profile.DateNaissance));
    }

    [Fact]
    public async Task InviterJeune_ProfilAutonome_EstCopieSurLeProfil()
    {
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();
        var (coachUserId, jeuneUserId, profile) = await InviterEtAccepterAsync(
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-17)),
            ProfilAccompagnement.Autonome);

        Assert.Equal(ProfilAccompagnement.Autonome, profile.ProfilAccompagnement);

        Assert.True(await jeuneService.MettreAJourProfilAccompagnementAsync(
            coachUserId, jeuneUserId, ProfilAccompagnement.SansExperience));
        var misAJour = await jeuneService.TryGetByUserIdAsync(jeuneUserId);
        Assert.Equal(ProfilAccompagnement.SansExperience, misAJour!.ProfilAccompagnement);

        var autreCoach = await CreerUtilisateurAsync($"autre-coach-profil-{Guid.NewGuid()}@test.local");
        Assert.False(await jeuneService.MettreAJourProfilAccompagnementAsync(
            autreCoach, jeuneUserId, ProfilAccompagnement.Autonome));
        misAJour = await jeuneService.TryGetByUserIdAsync(jeuneUserId);
        Assert.Equal(ProfilAccompagnement.SansExperience, misAJour!.ProfilAccompagnement);
    }

    [Fact]
    public async Task InviterJeune_SansResendApiKey_CreeInvitationEtRetourneLien_EmailNonEnvoye()
    {
        var coachUserId = await CreerUtilisateurAsync($"coach-{Guid.NewGuid()}@test.local");
        var jeuneEmail = $"jeune-{Guid.NewGuid()}@test.local";
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();

        var invite = await jeuneService.InviterJeuneAsync(
            coachUserId,
            jeuneEmail,
            "Martin",
            "Alex",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-17)),
            "http://localhost");

        Assert.True(invite.Success);
        Assert.NotNull(invite.Invitation);
        Assert.NotNull(invite.LienAcceptation);
        Assert.Contains(invite.Invitation!.Token, invite.LienAcceptation, StringComparison.Ordinal);
        Assert.False(invite.EmailEnvoye);
    }

    [Fact]
    public async Task GetInvitationsEnvoyeesEnAttenteAsync_NeRetourneQueLesInvitationsDuCoachDemandeur()
    {
        var coach1 = await CreerUtilisateurAsync($"coach1-{Guid.NewGuid()}@test.local");
        var coach2 = await CreerUtilisateurAsync($"coach2-{Guid.NewGuid()}@test.local");
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();
        var invitationQuery = fixture.Services.GetRequiredService<IJeunePrestataireInvitationQuery>();

        var inviteCoach1 = await jeuneService.InviterJeuneAsync(
            coach1,
            $"jeune1-{Guid.NewGuid()}@test.local",
            "Dupont",
            "Léa",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)),
            "http://localhost");

        await jeuneService.InviterJeuneAsync(
            coach2,
            $"jeune2-{Guid.NewGuid()}@test.local",
            "Martin",
            "Alex",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-17)),
            "http://localhost");

        var listeCoach1 = await invitationQuery.GetInvitationsEnvoyeesEnAttenteAsync(coach1);
        var listeCoach2 = await invitationQuery.GetInvitationsEnvoyeesEnAttenteAsync(coach2);

        Assert.Single(listeCoach1);
        Assert.Equal(inviteCoach1.Invitation!.Id, listeCoach1[0].InvitationId);
        Assert.Equal("Léa", listeCoach1[0].Prenoms);
        Assert.Equal("Dupont", listeCoach1[0].Nom);

        Assert.Single(listeCoach2);
        Assert.Equal("Alex", listeCoach2[0].Prenoms);
        Assert.NotEqual(listeCoach1[0].InvitationId, listeCoach2[0].InvitationId);
    }

    [Fact]
    public async Task GetInvitationsEnvoyeesEnAttenteAsync_InvitationAcceptee_NApparaitPlus()
    {
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();
        var invitationQuery = fixture.Services.GetRequiredService<IJeunePrestataireInvitationQuery>();
        var (coachUserId, _, _) = await InviterEtAccepterAsync(
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)));

        var liste = await invitationQuery.GetInvitationsEnvoyeesEnAttenteAsync(coachUserId);
        Assert.Empty(liste);
    }

    [Fact]
    public async Task RenvoyerInvitationAsync_AutreCoach_RefuseSilencieusement()
    {
        var coach1 = await CreerUtilisateurAsync($"coach1-{Guid.NewGuid()}@test.local");
        var coach2 = await CreerUtilisateurAsync($"coach2-{Guid.NewGuid()}@test.local");
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();

        var invite = await jeuneService.InviterJeuneAsync(
            coach1,
            $"jeune-{Guid.NewGuid()}@test.local",
            "Dupont",
            "Léa",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)),
            "http://localhost");

        var result = await jeuneService.RenvoyerInvitationAsync(
            invite.Invitation!.Id,
            coach2,
            "http://localhost");

        Assert.False(result.Success);
        Assert.False(result.EmailEnvoye);
    }

    [Fact]
    public async Task RenvoyerInvitationAsync_InvitationExpiree_CreeNouvelleInvitationPlutotQueLienMort()
    {
        var coachUserId = await CreerUtilisateurAsync($"coach-{Guid.NewGuid()}@test.local");
        var jeuneEmail = $"jeune-{Guid.NewGuid()}@test.local";
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();
        var invitationQuery = fixture.Services.GetRequiredService<IJeunePrestataireInvitationQuery>();

        var invite = await jeuneService.InviterJeuneAsync(
            coachUserId,
            jeuneEmail,
            "Dupont",
            "Léa",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)),
            "http://localhost");

        var ancienId = invite.Invitation!.Id;
        var ancienToken = invite.Invitation.Token;

        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();
        var invitation = await coreDb.Invitations.FirstAsync(i => i.Id == ancienId);
        invitation.ExpireLe = DateTimeOffset.UtcNow.AddDays(-1);
        await coreDb.SaveChangesAsync();

        var result = await jeuneService.RenvoyerInvitationAsync(ancienId, coachUserId, "http://localhost");

        Assert.True(result.Success);
        Assert.True(result.NouvelleInvitationCreee);
        Assert.NotEqual(ancienId, result.InvitationId);

        var ancienne = await coreDb.Invitations.AsNoTracking().FirstAsync(i => i.Id == ancienId);
        Assert.Equal(InvitationStatus.Revoquee, ancienne.Statut);

        var nouvelle = await coreDb.Invitations.AsNoTracking().FirstAsync(i => i.Id == result.InvitationId);
        Assert.Equal(InvitationStatus.EnAttente, nouvelle.Statut);
        Assert.NotEqual(ancienToken, nouvelle.Token);
        Assert.True(nouvelle.ExpireLe > DateTimeOffset.UtcNow);

        var liste = await invitationQuery.GetInvitationsEnvoyeesEnAttenteAsync(coachUserId);
        Assert.Single(liste);
        Assert.Equal(result.InvitationId, liste[0].InvitationId);
        Assert.False(liste[0].EstExpiree);
    }

    [Fact]
    public async Task RenvoyerInvitationAsync_InvitationEncoreValide_ConserveLeMemeToken()
    {
        var coachUserId = await CreerUtilisateurAsync($"coach-{Guid.NewGuid()}@test.local");
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();

        var invite = await jeuneService.InviterJeuneAsync(
            coachUserId,
            $"jeune-{Guid.NewGuid()}@test.local",
            "Dupont",
            "Léa",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)),
            "http://localhost");

        var result = await jeuneService.RenvoyerInvitationAsync(
            invite.Invitation!.Id,
            coachUserId,
            "http://localhost");

        Assert.True(result.Success);
        Assert.False(result.NouvelleInvitationCreee);
        Assert.Equal(invite.Invitation.Id, result.InvitationId);

        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();
        var invitation = await coreDb.Invitations.AsNoTracking().FirstAsync(i => i.Id == invite.Invitation.Id);
        Assert.Equal(invite.Invitation.Token, invitation.Token);
    }

    [Fact]
    public async Task AccepterInvitationJeune_ActiveGestionDuTempsImmediatement()
    {
        var (_, jeuneUserId, _) = await InviterEtAccepterAsync(
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)));

        var accessService = fixture.Services.GetRequiredService<IGestionDuTempsAccessService>();
        Assert.True(await accessService.HasAccessAsync(jeuneUserId));
        Assert.True(await accessService.HasCandidateAccessAsync(jeuneUserId));
    }

    private async Task<(string CoachUserId, string JeuneUserId, JeuneProfileView Profile)> InviterEtAccepterAsync(
        DateOnly dateNaissance,
        ProfilAccompagnement profil = ProfilAccompagnement.SansExperience)
    {
        var coachUserId = await CreerUtilisateurAsync($"coach-{Guid.NewGuid()}@test.local");
        var jeuneEmail = $"jeune-{Guid.NewGuid()}@test.local";

        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();
        var coachingService = fixture.Services.GetRequiredService<ICoachingService>();

        var invite = await jeuneService.InviterJeuneAsync(
            coachUserId,
            jeuneEmail,
            "Dupont",
            "Léa",
            dateNaissance,
            "http://localhost",
            profil);

        Assert.True(invite.Success);
        Assert.NotNull(invite.Invitation);

        var jeuneUserId = await CreerUtilisateurAsync(jeuneEmail);
        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();
        var invitation = await coreDb.Invitations.FirstAsync(i => i.Id == invite.Invitation!.Id);

        var profile = await jeuneService.FinaliserDepuisInvitationAsync(invitation, jeuneUserId);
        await coachingService.FinaliserJeunePrestataireDepuisInvitationAsync(invitation, jeuneUserId);
        await ActiverGestionDuTempsApresAcceptationJeuneAsync(jeuneUserId, coreDb);
        await fixture.Services.GetRequiredService<IInvitationService>().MarquerAccepteeAsync(invitation.Id, coreDb);

        return (coachUserId, jeuneUserId, profile);
    }

    /// <summary>
    /// Miroir de <c>InvitationAcceptancePage</c> + <see cref="Spectrometre.Host.Onboarding.CandidateOnboardingService"/>
    /// (le projet de test ne référence pas Host).
    /// </summary>
    private async Task ActiverGestionDuTempsApresAcceptationJeuneAsync(string jeuneUserId, CoreDbContext coreDb)
    {
        var candidateService = fixture.Services.GetRequiredService<ICandidateProfileService>();
        var moduleRegistry = fixture.Services.GetRequiredService<IModuleRegistry>();

        var candidateProfileId = await candidateService.GetOrCreateProfileIdAsync(jeuneUserId);
        if (!await coreDb.CandidateSubscriptions.AnyAsync(s => s.CandidateProfileId == candidateProfileId))
        {
            coreDb.CandidateSubscriptions.Add(new CandidateSubscription
            {
                CandidateProfileId = candidateProfileId,
                PlanCode = PlanCodes.Standard,
                Status = SubscriptionStatus.Essai,
            });
            await coreDb.SaveChangesAsync();
        }

        foreach (var moduleCode in new[] { "ProfilCandidat", "GestionDuTemps" })
        {
            if (!await moduleRegistry.IsActiveForCandidateAsync(candidateProfileId, moduleCode, coreDb))
                await moduleRegistry.ActivateForCandidateAsync(candidateProfileId, moduleCode, coreDb);
        }
    }

    private async Task<string> CreerUtilisateurAsync(string email)
    {
        using var scope = fixture.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, "TestPassword123!");
        Assert.True(result.Succeeded);
        fixture.TrackUserForCleanup(user.Id);
        return user.Id;
    }
}
