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
public sealed class MissionRetourTests(ServiceFixture fixture)
{
    [Fact]
    public async Task MarquerTerminee_EtRetour_GardesDAcces()
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var retourService = fixture.Services.GetRequiredService<IMissionRetourService>();
        var (particulierUserId, missionId) = await PublierMissionAsync();
        var jeune1 = await CreerJeuneAvecCoachAsync();
        var jeune2 = await CreerJeuneAvecCoachAsync();

        Assert.True(await missionService.AccepterMissionAsync(jeune1.UserId, missionId));
        var acceptationId = (await missionService.GetDemandesEnAttentePourJeuneSuiviAsync(jeune1.CoachUserId, jeune1.UserId))
            .Single().AcceptationId;

        // Pas encore Attribuee → MarquerTerminee refuse
        Assert.False(await missionService.MarquerTermineeAsync(jeune1.UserId, acceptationId));
        Assert.Null(await retourService.GetOrCreateAsync(jeune1.UserId, acceptationId));
        Assert.False(await retourService.SaveAsync(jeune1.UserId, acceptationId, "a", "b", "c", "d"));

        Assert.True(await missionService.ValiderAcceptationAsync(jeune1.CoachUserId, acceptationId));

        // Attribuee mais autre jeune → refuse
        Assert.False(await missionService.MarquerTermineeAsync(jeune2.UserId, acceptationId));
        // Coach ne peut pas marquer terminée
        Assert.False(await missionService.MarquerTermineeAsync(jeune1.CoachUserId, acceptationId));

        Assert.True(await missionService.MarquerTermineeAsync(jeune1.UserId, acceptationId));
        await using (var db = await fixture.Services.GetRequiredService<IDbContextFactory<MissionsDbContext>>().CreateDbContextAsync())
        {
            var mission = await db.Missions.AsNoTracking().FirstAsync(m => m.Id == missionId);
            Assert.Equal(MissionStatut.Terminee, mission.Statut);
        }

        // Déjà Terminee → second appel refuse
        Assert.False(await missionService.MarquerTermineeAsync(jeune1.UserId, acceptationId));

        // Jeune propriétaire : lecture + écriture
        var vueJeune = await retourService.GetOrCreateAsync(jeune1.UserId, acceptationId);
        Assert.NotNull(vueJeune);
        Assert.Equal(MissionRetourAccessMode.Jeune, vueJeune!.AccessMode);
        Assert.True(vueJeune.PeutEcrire);
        Assert.True(await retourService.SaveAsync(
            jeune1.UserId, acceptationId,
            "Bien passé", "Difficile", "Appris", "Améliorer"));

        vueJeune = await retourService.GetOrCreateAsync(jeune1.UserId, acceptationId);
        Assert.Equal("Bien passé", vueJeune!.CeQuiSestBienPasse);
        Assert.Equal("Difficile", vueJeune.CeQuiAEteDifficile);

        // Autre jeune : aucun accès
        Assert.Null(await retourService.GetOrCreateAsync(jeune2.UserId, acceptationId));
        Assert.False(await retourService.SaveAsync(jeune2.UserId, acceptationId, "x", null, null, null));

        // Coach suiveur : lecture OK, écriture refusée
        var vueCoach = await retourService.GetOrCreateAsync(jeune1.CoachUserId, acceptationId);
        Assert.NotNull(vueCoach);
        Assert.Equal(MissionRetourAccessMode.Coach, vueCoach!.AccessMode);
        Assert.False(vueCoach.PeutEcrire);
        Assert.Equal("Bien passé", vueCoach.CeQuiSestBienPasse);
        Assert.False(await retourService.SaveAsync(
            jeune1.CoachUserId, acceptationId, "hack", null, null, null));

        // Liste coach des terminées
        var terminees = await missionService.GetMissionsTermineesPourJeuneSuiviAsync(jeune1.CoachUserId, jeune1.UserId);
        Assert.Contains(terminees, m => m.AcceptationId == acceptationId);

        await CleanupMissionAsync(missionId, particulierUserId, jeune1, jeune2);
    }

    private async Task<(string UserId, int MissionId)> PublierMissionAsync()
    {
        var particulierUserId = await CreerParticulierAsync();
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var missionId = await missionService.PublierMissionAsync(
            particulierUserId,
            new PublierMissionInput("Mission retour", "Desc", null, null, MissionDifficulte.Facile, 20m, null,
                MissionCategorie.Autre, MissionNiveauEncadrement.PresentPendantMission));
        Assert.NotNull(missionId);
        return (particulierUserId, missionId.Value);
    }

    private async Task<string> CreerParticulierAsync()
    {
        var userId = await CreerUtilisateurAsync($"part-retour-{Guid.NewGuid()}@test.local");
        var particulierService = fixture.Services.GetRequiredService<IParticulierProfileService>();
        var moduleRegistry = fixture.Services.GetRequiredService<IModuleRegistry>();
        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();

        var profileId = await particulierService.GetOrCreateProfileIdAsync(userId, "Part", "Retour");
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
        var coachUserId = await CreerUtilisateurAsync($"coach-retour-{Guid.NewGuid()}@test.local");
        var jeuneEmail = $"jeune-retour-{Guid.NewGuid()}@test.local";
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();
        var coachingService = fixture.Services.GetRequiredService<ICoachingService>();

        var invite = await jeuneService.InviterJeuneAsync(
            coachUserId,
            jeuneEmail,
            "Retour",
            "Léa",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)),
            "http://localhost");
        Assert.True(invite.Success);

        var jeuneUserId = await CreerUtilisateurAsync(jeuneEmail);
        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();
        var invitation = await coreDb.Invitations.FirstAsync(i => i.Id == invite.Invitation!.Id);

        await jeuneService.FinaliserDepuisInvitationAsync(invitation, jeuneUserId);
        await coachingService.FinaliserJeunePrestataireDepuisInvitationAsync(invitation, jeuneUserId);
        await fixture.GarantirCharteAccepteeAsync(jeuneUserId);
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

    private async Task CleanupMissionAsync(int missionId, string particulierUserId, params JeuneContext[] jeunes)
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
