using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Invitations;
using Spectrometre.Core.Modules;
using Spectrometre.Modules.Coaching.Entities;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.JeunesPrestataires.Services;
using Spectrometre.Modules.Missions.Data;
using Spectrometre.Modules.Missions.Entities;
using Spectrometre.Modules.Missions.Services;
using Spectrometre.Modules.ProfilCandidat.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

[Collection("Base de données partagée")]
public sealed class FicheSuiviCoachTests(ServiceFixture fixture)
{
    [Fact]
    public async Task GetAsync_AgregeIdentite_Consentement_Missions_Grille_EtGuideVide()
    {
        var fiche = fixture.Services.GetRequiredService<IFicheSuiviCoachService>();
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();
        var consentement = fixture.Services.GetRequiredService<IConsentementParentalService>();
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var grilleService = fixture.Services.GetRequiredService<IGrilleObservationService>();
        var guideService = fixture.Services.GetRequiredService<IGuideEntrevueService>();
        var coachingService = fixture.Services.GetRequiredService<ICoachingService>();

        var dateNaissance = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16));
        var (coachUserId, jeuneUserId, jeuneProfileId) = await CreerJeuneAvecCoachAsync("Martin", "Léa", dateNaissance);
        var particulierUserId = await CreerParticulierAsync();

        Assert.True(jeuneService.EstMineur(dateNaissance));
        Assert.False(await consentement.EstConsentementValideAsync(jeuneProfileId));

        var missionId = await missionService.PublierMissionAsync(
            particulierUserId,
            new PublierMissionInput(
                "Mission fiche", "Desc", null, null, MissionDifficulte.Facile, 15m, null,
                MissionCategorie.Autre, MissionNiveauEncadrement.PresentPendantMission));
        Assert.NotNull(missionId);

        Assert.True(await missionService.AccepterMissionAsync(jeuneUserId, missionId.Value));
        var acceptationId = (await missionService.GetDemandesEnAttentePourJeuneSuiviAsync(coachUserId, jeuneUserId))
            .Single().AcceptationId;
        Assert.True(await missionService.ValiderAcceptationAsync(coachUserId, acceptationId));
        Assert.True(await missionService.MarquerTermineeAsync(jeuneUserId, acceptationId));

        var evalId = await grilleService.CreerEvaluationAsync(
            coachUserId,
            jeuneProfileId,
            [
                new GrilleObservationCritereInput("ponctualite", 4, null),
                new GrilleObservationCritereInput("autonomie", 5, null),
            ],
            null);
        Assert.NotNull(evalId);

        var guide = await guideService.GetOrCreateAsync(coachUserId, jeuneProfileId);
        Assert.NotNull(guide);
        Assert.Null(guide!.Id);

        var lienActif = (await coachingService.GetLiensPourCoachAsync(coachUserId))
            .Single(l => l.SuiviUserId == jeuneUserId && l.Statut == LienCoachingStatut.Actif);

        var view = await fiche.GetAsync(coachUserId, jeuneUserId);
        Assert.NotNull(view);
        Assert.Equal(jeuneUserId, view!.SuiviUserId);
        Assert.Equal("Martin", view.Nom);
        Assert.Equal("Léa", view.Prenoms);
        Assert.Equal(dateNaissance, view.DateNaissance);
        Assert.Equal(jeuneService.CalculerAge(dateNaissance), view.Age);
        Assert.True(view.EstMineur);
        Assert.Equal(FicheSuiviConsentementStatut.MineurEnAttente, view.ConsentementStatut);
        Assert.Equal(1, view.MissionsTerminees);
        Assert.Equal(0, view.MissionsEnCours);
        Assert.Equal(4.5, view.GrilleDerniereMoyenne);
        Assert.NotNull(view.GrilleDerniereEvaluationLe);
        Assert.False(view.GuideEntrevueRempli);
        Assert.Equal(lienActif.Id, view.LienCoachingId);

        await CleanupMissionAsync(missionId.Value, particulierUserId);
    }

    [Fact]
    public async Task GetAsync_CoachNonAutorise_RetourneNull()
    {
        var fiche = fixture.Services.GetRequiredService<IFicheSuiviCoachService>();
        var (coachUserId, jeuneUserId, _) = await CreerJeuneAvecCoachAsync("Durand", "Tom",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)));
        var autreCoach = await CreerUtilisateurAsync($"autre-coach-fiche-{Guid.NewGuid()}@test.local");

        Assert.NotNull(await fiche.GetAsync(coachUserId, jeuneUserId));
        Assert.Null(await fiche.GetAsync(autreCoach, jeuneUserId));
    }

    private async Task<(string CoachUserId, string JeuneUserId, int ProfileId)> CreerJeuneAvecCoachAsync(
        string nom,
        string prenoms,
        DateOnly dateNaissance)
    {
        var coachUserId = await CreerUtilisateurAsync($"coach-fiche-{Guid.NewGuid()}@test.local");
        var jeuneEmail = $"jeune-fiche-{Guid.NewGuid()}@test.local";
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();
        var coachingService = fixture.Services.GetRequiredService<ICoachingService>();

        var invite = await jeuneService.InviterJeuneAsync(
            coachUserId,
            jeuneEmail,
            nom,
            prenoms,
            dateNaissance,
            "http://localhost");
        Assert.True(invite.Success);

        var jeuneUserId = await CreerUtilisateurAsync(jeuneEmail);
        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();
        var invitation = await coreDb.Invitations.FirstAsync(i => i.Id == invite.Invitation!.Id);

        var profile = await jeuneService.FinaliserDepuisInvitationAsync(invitation, jeuneUserId);
        await coachingService.FinaliserJeunePrestataireDepuisInvitationAsync(invitation, jeuneUserId);
        await ActiverGestionDuTempsApresAcceptationJeuneAsync(jeuneUserId, coreDb);
        await fixture.Services.GetRequiredService<IInvitationService>().MarquerAccepteeAsync(invitation.Id, coreDb);

        return (coachUserId, jeuneUserId, profile.Id);
    }

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

    private async Task<string> CreerParticulierAsync()
    {
        var userId = await CreerUtilisateurAsync($"part-fiche-{Guid.NewGuid()}@test.local");
        var particulierService = fixture.Services.GetRequiredService<IParticulierProfileService>();
        var moduleRegistry = fixture.Services.GetRequiredService<IModuleRegistry>();
        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();

        var profileId = await particulierService.GetOrCreateProfileIdAsync(userId, "Part", "Fiche");
        if (!await coreDb.ParticulierSubscriptions.AnyAsync(s => s.ParticulierProfileId == profileId))
        {
            coreDb.ParticulierSubscriptions.Add(new ParticulierSubscription
            {
                ParticulierProfileId = profileId,
                PlanCode = PlanCodes.Particulier,
                Status = SubscriptionStatus.Active,
            });
            await coreDb.SaveChangesAsync();
        }

        if (!await moduleRegistry.IsActiveForParticulierAsync(profileId, "ProfilParticulier", coreDb))
            await moduleRegistry.ActivateForParticulierAsync(profileId, "ProfilParticulier", coreDb);

        return userId;
    }

    private async Task CleanupMissionAsync(int missionId, string particulierUserId)
    {
        await using var db = await fixture.Services.GetRequiredService<IDbContextFactory<MissionsDbContext>>().CreateDbContextAsync();
        var mission = await db.Missions.Include(m => m.Acceptations).FirstOrDefaultAsync(m => m.Id == missionId);
        if (mission is not null)
        {
            db.MissionAcceptations.RemoveRange(mission.Acceptations);
            db.Missions.Remove(mission);
            await db.SaveChangesAsync();
        }

        var particulier = await db.ParticulierProfiles.FirstOrDefaultAsync(p => p.UserId == particulierUserId);
        if (particulier is not null)
        {
            await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();
            var activations = await coreDb.ModuleActivations
                .Where(a => a.SubjectType == ModuleActivationSubjectType.Particulier && a.SubjectId == particulier.Id)
                .ToListAsync();
            coreDb.ModuleActivations.RemoveRange(activations);
            var subs = await coreDb.ParticulierSubscriptions.Where(s => s.ParticulierProfileId == particulier.Id).ToListAsync();
            coreDb.ParticulierSubscriptions.RemoveRange(subs);
            await coreDb.SaveChangesAsync();
            db.ParticulierProfiles.Remove(particulier);
            await db.SaveChangesAsync();
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
