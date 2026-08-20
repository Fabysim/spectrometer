using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Invitations;
using Spectrometre.Core.Modules;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.JeunesPrestataires.Services;
using Spectrometre.Modules.Missions.Data;
using Spectrometre.Modules.Missions.Entities;
using Spectrometre.Modules.Missions.Services;
using Spectrometre.Modules.ProfilCandidat.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

[Collection("Base de données partagée")]
public sealed class MesProgresTests(ServiceFixture fixture)
{
    [Fact]
    public async Task GetAsync_AgregeMissions_Grille_EtAutoObservation()
    {
        var mesProgres = fixture.Services.GetRequiredService<IMesProgresService>();
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var grilleService = fixture.Services.GetRequiredService<IGrilleObservationService>();
        var autoObs = fixture.Services.GetRequiredService<IAutoObservationService>();

        var (coachUserId, jeuneUserId, jeuneProfileId) = await CreerJeuneAvecCoachAsync();
        var particulierUserId = await CreerParticulierAsync();

        var missionId = await missionService.PublierMissionAsync(
            particulierUserId,
            new PublierMissionInput(
                "Mission progres", "Desc", null, null, MissionDifficulte.Facile, 15m, null,
                MissionCategorie.Autre, MissionNiveauEncadrement.PresentPendantMission));
        Assert.NotNull(missionId);

        Assert.True(await missionService.AccepterMissionAsync(jeuneUserId, missionId.Value));
        var acceptationId = (await missionService.GetDemandesEnAttentePourJeuneSuiviAsync(coachUserId, jeuneUserId))
            .Single().AcceptationId;
        Assert.True(await missionService.ValiderAcceptationAsync(coachUserId, acceptationId));
        Assert.True(await missionService.MarquerTermineeAsync(jeuneUserId, acceptationId));

        // Moyenne connue : (4 + 5) / 2 = 4.5
        var evalId = await grilleService.CreerEvaluationAsync(
            coachUserId,
            jeuneProfileId,
            [
                new GrilleObservationCritereInput("ponctualite", 4, null),
                new GrilleObservationCritereInput("autonomie", 5, null),
            ],
            null);
        Assert.NotNull(evalId);

        await autoObs.SaveSectionAsync(
            jeuneUserId,
            jeuneProfileId,
            "p2.s3",
            [new AutoObservationAnswerInput("p2.s3.progresser", null, 5)]);
        var synthese = await autoObs.RegenererSyntheseAsync(jeuneUserId, jeuneProfileId);
        Assert.False(string.IsNullOrWhiteSpace(synthese));

        var view = await mesProgres.GetAsync(jeuneUserId);
        Assert.NotNull(view);
        Assert.Equal(1, view!.MissionsTerminees);
        Assert.Equal(0, view.MissionsEnCours);
        Assert.Equal(4.5, view.GrilleDerniereMoyenne);
        Assert.NotNull(view.GrilleDerniereEvaluationLe);
        Assert.True(view.AutoObsSyntheseGeneree);
        Assert.NotNull(view.AutoObsSyntheseGenereeLe);
        Assert.False(string.IsNullOrWhiteSpace(view.AutoObsSyntheseExtrait));

        Assert.Null(await mesProgres.GetAsync(coachUserId));

        await CleanupMissionAsync(missionId.Value, particulierUserId);
    }

    private async Task<(string CoachUserId, string JeuneUserId, int ProfileId)> CreerJeuneAvecCoachAsync()
    {
        var coachUserId = await CreerUtilisateurAsync($"coach-prog-{Guid.NewGuid()}@test.local");
        var jeuneEmail = $"jeune-prog-{Guid.NewGuid()}@test.local";
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();
        var coachingService = fixture.Services.GetRequiredService<ICoachingService>();

        var invite = await jeuneService.InviterJeuneAsync(
            coachUserId,
            jeuneEmail,
            "Prog",
            "Léa",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)),
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
        var userId = await CreerUtilisateurAsync($"part-prog-{Guid.NewGuid()}@test.local");
        var particulierService = fixture.Services.GetRequiredService<IParticulierProfileService>();
        var moduleRegistry = fixture.Services.GetRequiredService<IModuleRegistry>();
        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();

        var profileId = await particulierService.GetOrCreateProfileIdAsync(userId, "Part", "Prog");
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
