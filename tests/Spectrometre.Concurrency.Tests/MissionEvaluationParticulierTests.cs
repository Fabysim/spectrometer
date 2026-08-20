using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Invitations;
using Spectrometre.Core.Modules;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.GestionDuTemps.Services;
using Spectrometre.Modules.JeunesPrestataires.Services;
using Spectrometre.Modules.Missions.Data;
using Spectrometre.Modules.Missions.Entities;
using Spectrometre.Modules.Missions.Services;
using Spectrometre.Modules.ProfilCandidat.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

[Collection("Base de données partagée")]
public sealed class MissionEvaluationParticulierTests(ServiceFixture fixture)
{
    [Fact]
    public async Task PublierMission_CategorieAutre_ExigeTitre_SinonNon()
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var particulierUserId = await CreerParticulierAsync();

        Assert.Null(await missionService.PublierMissionAsync(
            particulierUserId,
            new PublierMissionInput(
                null, "Desc", null, null, MissionDifficulte.Facile, null, null,
                MissionCategorie.Autre, MissionNiveauEncadrement.PresentPendantMission)));

        Assert.Null(await missionService.PublierMissionAsync(
            particulierUserId,
            new PublierMissionInput(
                "   ", "Desc", null, null, MissionDifficulte.Facile, null, null,
                MissionCategorie.Autre, MissionNiveauEncadrement.PresentPendantMission)));

        var idAutre = await missionService.PublierMissionAsync(
            particulierUserId,
            new PublierMissionInput(
                "Tâche spéciale", "Desc", null, null, MissionDifficulte.Facile, null, null,
                MissionCategorie.Autre, MissionNiveauEncadrement.PresentPendantMission));
        Assert.NotNull(idAutre);

        var idJardin = await missionService.PublierMissionAsync(
            particulierUserId,
            new PublierMissionInput(
                null, "Desc jardin", null, null, MissionDifficulte.Facile, null, null,
                MissionCategorie.JardinageSimple, MissionNiveauEncadrement.AutonomieApresExplication,
                PresenceEscaliers: true, RisqueParticulier: "Escalier extérieur"));
        Assert.NotNull(idJardin);

        await using var db = await fixture.Services.GetRequiredService<IDbContextFactory<MissionsDbContext>>().CreateDbContextAsync();
        var jardin = await db.Missions.AsNoTracking().FirstAsync(m => m.Id == idJardin.Value);
        Assert.Equal(MissionCategorie.JardinageSimple, jardin.Categorie);
        Assert.Equal("", jardin.Titre);
        Assert.Equal(MissionNiveauEncadrement.AutonomieApresExplication, jardin.NiveauEncadrement);
        Assert.True(jardin.PresenceEscaliers);
        Assert.Equal("Escalier extérieur", jardin.RisqueParticulier);

        await CleanupMissionsAsync([idAutre.Value, idJardin.Value], particulierUserId);
    }

    [Fact]
    public async Task Evaluation_GardesDAcces_TroisLecteurs_UnRedacteur()
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var evaluationService = fixture.Services.GetRequiredService<IMissionEvaluationParticulierService>();
        var (particulierUserId, missionId) = await PublierMissionAsync();
        var autreParticulierUserId = await CreerParticulierAsync();
        var jeune1 = await CreerJeuneAvecCoachAsync();
        var jeune2 = await CreerJeuneAvecCoachAsync();

        Assert.True(await missionService.AccepterMissionAsync(jeune1.UserId, missionId));
        var acceptationId = (await missionService.GetDemandesEnAttentePourJeuneSuiviAsync(jeune1.CoachUserId, jeune1.UserId))
            .Single().AcceptationId;

        // Pas encore Terminee → aucun accès
        Assert.Null(await evaluationService.GetOrCreateAsync(particulierUserId, acceptationId));
        Assert.False(await evaluationService.SaveAsync(
            particulierUserId, acceptationId, true, true, true, true, "ok", null, true));

        Assert.True(await missionService.ValiderAcceptationAsync(jeune1.CoachUserId, acceptationId));
        Assert.True(await missionService.MarquerTermineeAsync(jeune1.UserId, acceptationId));

        // Particulier propriétaire : écriture
        var vuePart = await evaluationService.GetOrCreateAsync(particulierUserId, acceptationId);
        Assert.NotNull(vuePart);
        Assert.Equal(MissionEvaluationParticulierAccessMode.Particulier, vuePart!.AccessMode);
        Assert.True(vuePart.PeutEcrire);
        Assert.True(await evaluationService.SaveAsync(
            particulierUserId, acceptationId,
            ponctualite: true,
            consignesComprises: true,
            tacheRealiseeCorrectement: false,
            attitudeRespectueuse: true,
            pointsPositifs: "Ponctuel",
            pointsAAmeliorer: "Relire la consigne",
            accepteraitNouvelleMission: true));

        vuePart = await evaluationService.GetOrCreateAsync(particulierUserId, acceptationId);
        Assert.Equal("Ponctuel", vuePart!.PointsPositifs);
        Assert.False(vuePart.TacheRealiseeCorrectement);

        // Autre particulier : refus
        Assert.Null(await evaluationService.GetOrCreateAsync(autreParticulierUserId, acceptationId));
        Assert.False(await evaluationService.SaveAsync(
            autreParticulierUserId, acceptationId, true, null, null, null, "x", null, null));

        // Jeune : lecture OK, écriture refusée
        var vueJeune = await evaluationService.GetOrCreateAsync(jeune1.UserId, acceptationId);
        Assert.NotNull(vueJeune);
        Assert.Equal(MissionEvaluationParticulierAccessMode.Jeune, vueJeune!.AccessMode);
        Assert.False(vueJeune.PeutEcrire);
        Assert.Equal("Ponctuel", vueJeune.PointsPositifs);
        Assert.False(await evaluationService.SaveAsync(
            jeune1.UserId, acceptationId, false, null, null, null, "hack", null, null));

        // Autre jeune : aucun accès
        Assert.Null(await evaluationService.GetOrCreateAsync(jeune2.UserId, acceptationId));
        Assert.False(await evaluationService.SaveAsync(
            jeune2.UserId, acceptationId, true, null, null, null, "x", null, null));

        // Coach suiveur : lecture OK, écriture refusée
        var vueCoach = await evaluationService.GetOrCreateAsync(jeune1.CoachUserId, acceptationId);
        Assert.NotNull(vueCoach);
        Assert.Equal(MissionEvaluationParticulierAccessMode.Coach, vueCoach!.AccessMode);
        Assert.False(vueCoach.PeutEcrire);
        Assert.Equal("Ponctuel", vueCoach.PointsPositifs);
        Assert.False(await evaluationService.SaveAsync(
            jeune1.CoachUserId, acceptationId, false, null, null, null, "hack", null, null));

        // Persistance inchangée après tentatives d'écriture refusées
        vuePart = await evaluationService.GetOrCreateAsync(particulierUserId, acceptationId);
        Assert.Equal("Ponctuel", vuePart!.PointsPositifs);
        Assert.True(vuePart.Ponctualite);

        await CleanupMissionsAsync([missionId], particulierUserId, autreParticulierUserId);
        await CleanupJeunesAsync(jeune1, jeune2);
    }

    private async Task<(string UserId, int MissionId)> PublierMissionAsync()
    {
        var particulierUserId = await CreerParticulierAsync();
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var missionId = await missionService.PublierMissionAsync(
            particulierUserId,
            new PublierMissionInput(
                "Mission eval", "Desc", null, null, MissionDifficulte.Facile, 20m, null,
                MissionCategorie.Rangement, MissionNiveauEncadrement.PresentDebutSeulement,
                PresenceAnimaux: true));
        Assert.NotNull(missionId);
        return (particulierUserId, missionId.Value);
    }

    private async Task<string> CreerParticulierAsync()
    {
        var userId = await CreerUtilisateurAsync($"part-eval-{Guid.NewGuid()}@test.local");
        var particulierService = fixture.Services.GetRequiredService<IParticulierProfileService>();
        var moduleRegistry = fixture.Services.GetRequiredService<IModuleRegistry>();
        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();

        var profileId = await particulierService.GetOrCreateProfileIdAsync(userId, "Part", "Eval");
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

    private sealed record JeuneContext(string UserId, string CoachUserId);

    private async Task<JeuneContext> CreerJeuneAvecCoachAsync()
    {
        var coachUserId = await CreerUtilisateurAsync($"coach-eval-{Guid.NewGuid()}@test.local");
        var jeuneEmail = $"jeune-eval-{Guid.NewGuid()}@test.local";
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();
        var coachingService = fixture.Services.GetRequiredService<ICoachingService>();

        var invite = await jeuneService.InviterJeuneAsync(
            coachUserId,
            jeuneEmail,
            "Eval",
            "Léa",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)),
            "http://localhost");
        Assert.True(invite.Success);

        var jeuneUserId = await CreerUtilisateurAsync(jeuneEmail);
        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();
        var invitation = await coreDb.Invitations.FirstAsync(i => i.Id == invite.Invitation!.Id);

        await jeuneService.FinaliserDepuisInvitationAsync(invitation, jeuneUserId);
        await coachingService.FinaliserJeunePrestataireDepuisInvitationAsync(invitation, jeuneUserId);
        await ActiverGestionDuTempsApresAcceptationJeuneAsync(jeuneUserId, coreDb);
        await fixture.Services.GetRequiredService<IInvitationService>().MarquerAccepteeAsync(invitation.Id, coreDb);

        return new JeuneContext(jeuneUserId, coachUserId);
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

    private async Task CleanupMissionsAsync(IEnumerable<int> missionIds, params string[] particulierUserIds)
    {
        await using var db = await fixture.Services.GetRequiredService<IDbContextFactory<MissionsDbContext>>().CreateDbContextAsync();
        foreach (var missionId in missionIds)
        {
            var mission = await db.Missions
                .Include(m => m.Acceptations)
                .FirstOrDefaultAsync(m => m.Id == missionId);
            if (mission is null)
                continue;

            var acceptationIds = mission.Acceptations.Select(a => a.Id).ToList();
            var evals = await db.MissionEvaluationsParticulier
                .Where(e => acceptationIds.Contains(e.MissionAcceptationId))
                .ToListAsync();
            db.MissionEvaluationsParticulier.RemoveRange(evals);
            var retours = await db.MissionRetours
                .Where(r => acceptationIds.Contains(r.MissionAcceptationId))
                .ToListAsync();
            db.MissionRetours.RemoveRange(retours);
            db.MissionAcceptations.RemoveRange(mission.Acceptations);
            db.Missions.Remove(mission);
        }

        await db.SaveChangesAsync();

        foreach (var particulierUserId in particulierUserIds)
        {
            var particulier = await db.ParticulierProfiles.FirstOrDefaultAsync(p => p.UserId == particulierUserId);
            if (particulier is null)
                continue;

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

    private Task CleanupJeunesAsync(params JeuneContext[] _) => Task.CompletedTask;

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
