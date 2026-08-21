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
using Spectrometre.Modules.Missions.Catalog;
using Spectrometre.Modules.Missions.Data;
using Spectrometre.Modules.Missions.Entities;
using Spectrometre.Modules.Missions.Services;
using Spectrometre.Modules.ProfilCandidat.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

[Collection("Base de données partagée")]
public sealed class MissionPreparationTests(ServiceFixture fixture)
{
    [Fact]
    public async Task GetEtToggle_RefuseSiPasProprietaireOuPasValidee()
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var prep = fixture.Services.GetRequiredService<IMissionPreparationService>();
        var (particulierUserId, missionId) = await PublierMissionAsync();
        var jeune1 = await CreerJeuneAvecCoachAsync();
        var jeune2 = await CreerJeuneAvecCoachAsync();

        await fixture.GarantirPublicationValideeAsync(jeune1.CoachUserId, missionId);
        Assert.True(await missionService.AccepterMissionAsync(jeune1.UserId, missionId));
        var acceptationId = (await missionService.GetDemandesEnAttentePourJeuneSuiviAsync(jeune1.CoachUserId, jeune1.UserId))
            .Single().AcceptationId;

        // En attente coach → inaccessible
        Assert.Null(await prep.GetPreparationAsync(jeune1.UserId, acceptationId));
        Assert.False(await prep.ToggleItemPreparationAsync(jeune1.UserId, acceptationId, "tenue_adaptee", true));

        Assert.True(await missionService.RefuserAcceptationAsync(jeune1.CoachUserId, acceptationId));
        // Refusée → inaccessible
        Assert.Null(await prep.GetPreparationAsync(jeune1.UserId, acceptationId));
        Assert.False(await prep.ToggleItemPreparationAsync(jeune1.UserId, acceptationId, "tenue_adaptee", true));

        Assert.True(await missionService.AccepterMissionAsync(jeune1.UserId, missionId));
        acceptationId = (await missionService.GetDemandesEnAttentePourJeuneSuiviAsync(jeune1.CoachUserId, jeune1.UserId))
            .Single().AcceptationId;
        Assert.True(await missionService.ValiderAcceptationAsync(jeune1.CoachUserId, acceptationId));

        // Autre jeune → inaccessible
        Assert.Null(await prep.GetPreparationAsync(jeune2.UserId, acceptationId));
        Assert.False(await prep.ToggleItemPreparationAsync(jeune2.UserId, acceptationId, "tenue_adaptee", true));

        // Propriétaire + validée → OK
        var view = await prep.GetPreparationAsync(jeune1.UserId, acceptationId);
        Assert.NotNull(view);
        Assert.Equal(MissionPreparationCatalog.Items.Count, view!.Items.Count);
        Assert.All(view.Items, i => Assert.False(i.Coche));

        Assert.True(await prep.ToggleItemPreparationAsync(jeune1.UserId, acceptationId, "tenue_adaptee", true));
        Assert.True(await prep.ToggleItemPreparationAsync(jeune1.UserId, acceptationId, "horaire_verifie", true));
        Assert.False(await prep.ToggleItemPreparationAsync(jeune1.UserId, acceptationId, "cle_inconnue", true));

        view = await prep.GetPreparationAsync(jeune1.UserId, acceptationId);
        Assert.NotNull(view);
        Assert.Equal(2, view!.Items.Count(i => i.Coche));
        Assert.Contains(view.Items, i => i.ItemKey == "tenue_adaptee" && i.Coche);
        Assert.Contains(view.Items, i => i.ItemKey == "horaire_verifie" && i.Coche);

        await CleanupMissionAsync(missionId, particulierUserId, jeune1, jeune2);
    }

    private async Task<(string UserId, int MissionId)> PublierMissionAsync()
    {
        var particulierUserId = await CreerParticulierAsync();
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var missionId = await missionService.PublierMissionAsync(
            particulierUserId,
            new PublierMissionInput("Mission prep", "Desc", null, null, MissionDifficulte.Facile, 20m, null,
                MissionCategorie.Autre, MissionNiveauEncadrement.PresentPendantMission));
        Assert.NotNull(missionId);
        return (particulierUserId, missionId.Value);
    }

    private async Task<string> CreerParticulierAsync()
    {
        var userId = await CreerUtilisateurAsync($"part-prep-{Guid.NewGuid()}@test.local");
        var particulierService = fixture.Services.GetRequiredService<IParticulierProfileService>();
        var moduleRegistry = fixture.Services.GetRequiredService<IModuleRegistry>();
        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();

        var profileId = await particulierService.GetOrCreateProfileIdAsync(userId, "Part", "Prep");
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
        var coachUserId = await CreerUtilisateurAsync($"coach-prep-{Guid.NewGuid()}@test.local");
        var jeuneEmail = $"jeune-prep-{Guid.NewGuid()}@test.local";
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();
        var coachingService = fixture.Services.GetRequiredService<ICoachingService>();

        var invite = await jeuneService.InviterJeuneAsync(
            coachUserId,
            jeuneEmail,
            "Prep",
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
