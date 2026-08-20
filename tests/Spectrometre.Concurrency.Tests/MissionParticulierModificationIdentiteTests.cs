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
public sealed class MissionParticulierModificationIdentiteTests(ServiceFixture fixture)
{
    [Fact]
    public async Task GetMesMissionsPubliees_PrenomJeune_UniquementApresAttribution()
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var (particulierUserId, missionId) = await PublierMissionAsync();
        var jeune = await CreerJeuneAvecCoachAsync("Léa", "Dupont-Test");

        var avant = Assert.Single(await missionService.GetMesMissionsPublieesAsync(particulierUserId), m => m.MissionId == missionId);
        Assert.Equal(MissionStatut.Disponible, avant.Statut);
        Assert.Null(avant.JeunePrenom);

        Assert.True(await missionService.AccepterMissionAsync(jeune.UserId, missionId));
        var enAttente = Assert.Single(await missionService.GetMesMissionsPublieesAsync(particulierUserId), m => m.MissionId == missionId);
        Assert.Equal(MissionStatut.EnAttenteValidation, enAttente.Statut);
        Assert.Null(enAttente.JeunePrenom);

        var acceptationId = (await missionService.GetDemandesEnAttentePourJeuneSuiviAsync(jeune.CoachUserId, jeune.UserId))
            .Single().AcceptationId;
        Assert.True(await missionService.ValiderAcceptationAsync(jeune.CoachUserId, acceptationId));

        var attribuee = Assert.Single(await missionService.GetMesMissionsPublieesAsync(particulierUserId), m => m.MissionId == missionId);
        Assert.Equal(MissionStatut.Attribuee, attribuee.Statut);
        Assert.Equal("Léa", attribuee.JeunePrenom);
        Assert.DoesNotContain("Dupont", attribuee.JeunePrenom, StringComparison.OrdinalIgnoreCase);

        Assert.True(await missionService.MarquerTermineeAsync(jeune.UserId, acceptationId));
        var terminee = Assert.Single(await missionService.GetMesMissionsPublieesAsync(particulierUserId), m => m.MissionId == missionId);
        Assert.Equal(MissionStatut.Terminee, terminee.Statut);
        Assert.Equal("Léa", terminee.JeunePrenom);

        await CleanupMissionsAsync([missionId], particulierUserId);
    }

    [Fact]
    public async Task ModifierMission_UniquementDisponibleEtProprietaire_MemeValidationPublication()
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var (particulierUserId, missionId) = await PublierMissionAsync();
        var autreParticulier = await CreerParticulierAsync();
        var jeune = await CreerJeuneAvecCoachAsync("Nora", "Cachee");

        DateTimeOffset createdAt;
        await using (var db = await fixture.Services.GetRequiredService<IDbContextFactory<MissionsDbContext>>().CreateDbContextAsync())
        {
            createdAt = (await db.Missions.AsNoTracking().FirstAsync(m => m.Id == missionId)).CreatedAt;
        }

        Assert.False(await missionService.ModifierMissionAsync(
            autreParticulier,
            missionId,
            Input(MissionCategorie.JardinageSimple, titre: null, description: "Hijack")));
        Assert.Null(await missionService.TryGetMissionPourModificationAsync(autreParticulier, missionId));

        Assert.False(await missionService.ModifierMissionAsync(
            particulierUserId,
            missionId,
            Input(MissionCategorie.Autre, titre: null, description: "Sans titre")));
        Assert.False(await missionService.ModifierMissionAsync(
            particulierUserId,
            missionId,
            Input(MissionCategorie.Autre, titre: "   ", description: "Sans titre")));

        Assert.True(await missionService.ModifierMissionAsync(
            particulierUserId,
            missionId,
            new PublierMissionInput(
                "Tâche spéciale",
                "Nouvelle desc",
                "Rue test",
                "2 h",
                MissionDifficulte.Intermediaire,
                35m,
                "Organisation",
                MissionCategorie.Autre,
                MissionNiveauEncadrement.AutonomieApresExplication,
                PresenceEscaliers: true,
                RisqueParticulier: "Marches")));

        await using (var db = await fixture.Services.GetRequiredService<IDbContextFactory<MissionsDbContext>>().CreateDbContextAsync())
        {
            var mission = await db.Missions.AsNoTracking().FirstAsync(m => m.Id == missionId);
            Assert.Equal(MissionStatut.Disponible, mission.Statut);
            Assert.Equal(createdAt, mission.CreatedAt);
            Assert.Equal(MissionCategorie.Autre, mission.Categorie);
            Assert.Equal("Tâche spéciale", mission.Titre);
            Assert.Equal("Nouvelle desc", mission.Description);
            Assert.Equal("Rue test", mission.Lieu);
            Assert.Equal("2 h", mission.DureeEstimee);
            Assert.Equal(MissionDifficulte.Intermediaire, mission.Difficulte);
            Assert.Equal(35m, mission.RemunerationMontant);
            Assert.Equal("Organisation", mission.CompetencesTravaillees);
            Assert.Equal(MissionNiveauEncadrement.AutonomieApresExplication, mission.NiveauEncadrement);
            Assert.True(mission.PresenceEscaliers);
            Assert.Equal("Marches", mission.RisqueParticulier);
        }

        Assert.True(await missionService.AccepterMissionAsync(jeune.UserId, missionId));
        Assert.False(await missionService.ModifierMissionAsync(
            particulierUserId,
            missionId,
            Input(MissionCategorie.JardinageSimple, titre: null, description: "Trop tard")));
        Assert.Null(await missionService.TryGetMissionPourModificationAsync(particulierUserId, missionId));

        var acceptationId = (await missionService.GetDemandesEnAttentePourJeuneSuiviAsync(jeune.CoachUserId, jeune.UserId))
            .Single().AcceptationId;
        Assert.True(await missionService.ValiderAcceptationAsync(jeune.CoachUserId, acceptationId));
        Assert.False(await missionService.ModifierMissionAsync(
            particulierUserId,
            missionId,
            Input(MissionCategorie.JardinageSimple, titre: null, description: "Encore trop tard")));

        await CleanupMissionsAsync([missionId], particulierUserId, autreParticulier);
    }

    private static PublierMissionInput Input(MissionCategorie categorie, string? titre, string description) =>
        new(
            titre,
            description,
            null,
            null,
            MissionDifficulte.Facile,
            20m,
            null,
            categorie,
            MissionNiveauEncadrement.PresentPendantMission);

    private async Task<(string UserId, int MissionId)> PublierMissionAsync()
    {
        var particulierUserId = await CreerParticulierAsync();
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var missionId = await missionService.PublierMissionAsync(
            particulierUserId,
            Input(MissionCategorie.Autre, "Mission modif", "Desc initiale"));
        Assert.NotNull(missionId);
        return (particulierUserId, missionId.Value);
    }

    private async Task<string> CreerParticulierAsync()
    {
        var userId = await CreerUtilisateurAsync($"part-mod-{Guid.NewGuid()}@test.local");
        var particulierService = fixture.Services.GetRequiredService<IParticulierProfileService>();
        var moduleRegistry = fixture.Services.GetRequiredService<IModuleRegistry>();
        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();

        var profileId = await particulierService.GetOrCreateProfileIdAsync(userId, "Part", "Mod");
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

    private async Task<JeuneContext> CreerJeuneAvecCoachAsync(string prenoms, string nom)
    {
        var coachUserId = await CreerUtilisateurAsync($"coach-mod-{Guid.NewGuid()}@test.local");
        var jeuneEmail = $"jeune-mod-{Guid.NewGuid()}@test.local";
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();
        var coachingService = fixture.Services.GetRequiredService<ICoachingService>();

        var invite = await jeuneService.InviterJeuneAsync(
            coachUserId,
            jeuneEmail,
            nom,
            prenoms,
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
            var mission = await db.Missions.Include(m => m.Acceptations).FirstOrDefaultAsync(m => m.Id == missionId);
            if (mission is null)
                continue;
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
