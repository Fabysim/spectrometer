using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Invitations;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Notifications;
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
public sealed class MissionTests(ServiceFixture fixture)
{
    private static async Task RunConcurrentlyAsync(IReadOnlyList<Func<Task>> actions)
    {
        using var barrier = new Barrier(actions.Count);
        var tasks = actions.Select(action => Task.Run(async () =>
        {
            barrier.SignalAndWait();
            await action();
        })).ToArray();
        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task AccepterMission_DejaEnAttenteValidation_RetourneFalse()
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var (particulierUserId, missionId) = await PublierMissionAsync();
        var jeune1 = await CreerJeuneAvecCoachAsync();
        var jeune2 = await CreerJeuneAvecCoachAsync();
        await fixture.GarantirPublicationValideeAsync(jeune1.CoachUserId, missionId);

        Assert.True(await missionService.AccepterMissionAsync(jeune1.UserId, missionId));
        Assert.False(await missionService.AccepterMissionAsync(jeune2.UserId, missionId));

        await CleanupMissionAsync(missionId, particulierUserId, jeune1, jeune2);
    }

    [Fact]
    public async Task AccepterMission_Concurrent_DeuxJeunes_UnSeulGagne()
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var (particulierUserId, missionId) = await PublierMissionAsync();
        var jeune1 = await CreerJeuneAvecCoachAsync();
        var jeune2 = await CreerJeuneAvecCoachAsync();
        await fixture.GarantirPublicationValideeAsync(jeune1.CoachUserId, missionId);

        var results = new bool[2];
        await RunConcurrentlyAsync([
            () => AcceptAndStoreAsync(missionService, jeune1.UserId, missionId, results, 0),
            () => AcceptAndStoreAsync(missionService, jeune2.UserId, missionId, results, 1),
        ]);

        Assert.Equal(1, results.Count(r => r));
        Assert.Equal(1, results.Count(r => !r));

        await using var db = await fixture.Services.GetRequiredService<IDbContextFactory<MissionsDbContext>>().CreateDbContextAsync();
        var acceptations = await db.MissionAcceptations
            .Where(a => a.MissionId == missionId && a.Statut == MissionAcceptationStatut.EnAttenteValidationCoach)
            .ToListAsync();
        Assert.Single(acceptations);

        await CleanupMissionAsync(missionId, particulierUserId, jeune1, jeune2);
    }

    [Fact]
    public async Task RefuserAcceptation_MissionRedevientDisponible_AutreJeunePeutAccepter()
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var (particulierUserId, missionId) = await PublierMissionAsync();
        var jeune1 = await CreerJeuneAvecCoachAsync();
        var jeune2 = await CreerJeuneAvecCoachAsync();
        await fixture.GarantirPublicationValideeAsync(jeune1.CoachUserId, missionId);

        Assert.True(await missionService.AccepterMissionAsync(jeune1.UserId, missionId));
        var demandes = await missionService.GetDemandesEnAttentePourJeuneSuiviAsync(jeune1.CoachUserId, jeune1.UserId);
        var acceptationId = demandes.Single().AcceptationId;

        Assert.True(await missionService.RefuserAcceptationAsync(jeune1.CoachUserId, acceptationId));

        await using var db = await fixture.Services.GetRequiredService<IDbContextFactory<MissionsDbContext>>().CreateDbContextAsync();
        var mission = await db.Missions.AsNoTracking().FirstAsync(m => m.Id == missionId);
        Assert.Equal(MissionStatut.Disponible, mission.Statut);

        Assert.True(await missionService.AccepterMissionAsync(jeune2.UserId, missionId));

        await CleanupMissionAsync(missionId, particulierUserId, jeune1, jeune2);
    }

    [Fact]
    public async Task RetirerCandidature_EnAttente_MissionRedevientDisponible_NotifieCoach()
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var notifService = fixture.Services.GetRequiredService<INotificationService>();
        var (particulierUserId, missionId) = await PublierMissionAsync();
        var jeune1 = await CreerJeuneAvecCoachAsync();
        var jeune2 = await CreerJeuneAvecCoachAsync();
        await fixture.GarantirPublicationValideeAsync(jeune1.CoachUserId, missionId);

        Assert.True(await missionService.AccepterMissionAsync(jeune1.UserId, missionId));
        var acceptationId = (await missionService.GetDemandesEnAttentePourJeuneSuiviAsync(jeune1.CoachUserId, jeune1.UserId))
            .Single().AcceptationId;

        Assert.True(await missionService.RetirerCandidatureAsync(jeune1.UserId, acceptationId));

        await using (var db = await fixture.Services.GetRequiredService<IDbContextFactory<MissionsDbContext>>().CreateDbContextAsync())
        {
            var mission = await db.Missions.AsNoTracking().FirstAsync(m => m.Id == missionId);
            Assert.Equal(MissionStatut.Disponible, mission.Statut);
            var acceptation = await db.MissionAcceptations.AsNoTracking().FirstAsync(a => a.Id == acceptationId);
            Assert.Equal(MissionAcceptationStatut.RetireeParJeune, acceptation.Statut);
        }

        Assert.Empty(await missionService.GetDemandesEnAttentePourJeuneSuiviAsync(jeune1.CoachUserId, jeune1.UserId));

        var notifCoach = Assert.Single(
            await notifService.GetRecentesAsync(jeune1.CoachUserId, 20),
            n => n.TypeCode == "Missions.CandidatureRetiree");
        Assert.Equal($"/coach/suivis/{jeune1.UserId}/missions", notifCoach.Lien);
        Assert.Contains("Léa", notifCoach.Message);

        Assert.True(await missionService.AccepterMissionAsync(jeune2.UserId, missionId));

        await CleanupMissionAsync(missionId, particulierUserId, jeune1, jeune2);
    }

    [Fact]
    public async Task RetirerCandidature_RefuseSiValideeOuAutreJeune()
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var (particulierUserId, missionId) = await PublierMissionAsync();
        var jeune1 = await CreerJeuneAvecCoachAsync();
        var jeune2 = await CreerJeuneAvecCoachAsync();
        await fixture.GarantirPublicationValideeAsync(jeune1.CoachUserId, missionId);

        Assert.True(await missionService.AccepterMissionAsync(jeune1.UserId, missionId));
        var acceptationId = (await missionService.GetDemandesEnAttentePourJeuneSuiviAsync(jeune1.CoachUserId, jeune1.UserId))
            .Single().AcceptationId;

        Assert.False(await missionService.RetirerCandidatureAsync(jeune2.UserId, acceptationId));

        await using (var db = await fixture.Services.GetRequiredService<IDbContextFactory<MissionsDbContext>>().CreateDbContextAsync())
        {
            var acceptation = await db.MissionAcceptations.AsNoTracking().FirstAsync(a => a.Id == acceptationId);
            Assert.Equal(MissionAcceptationStatut.EnAttenteValidationCoach, acceptation.Statut);
            var mission = await db.Missions.AsNoTracking().FirstAsync(m => m.Id == missionId);
            Assert.Equal(MissionStatut.EnAttenteValidation, mission.Statut);
        }

        Assert.True(await missionService.ValiderAcceptationAsync(jeune1.CoachUserId, acceptationId));
        Assert.False(await missionService.RetirerCandidatureAsync(jeune1.UserId, acceptationId));

        await using (var db = await fixture.Services.GetRequiredService<IDbContextFactory<MissionsDbContext>>().CreateDbContextAsync())
        {
            var mission = await db.Missions.AsNoTracking().FirstAsync(m => m.Id == missionId);
            Assert.Equal(MissionStatut.Attribuee, mission.Statut);
            var acceptation = await db.MissionAcceptations.AsNoTracking().FirstAsync(a => a.Id == acceptationId);
            Assert.Equal(MissionAcceptationStatut.ValideeParCoach, acceptation.Statut);
        }

        await CleanupMissionAsync(missionId, particulierUserId, jeune1, jeune2);
    }

    [Fact]
    public async Task ValiderOuRefuser_CoachNonAutorise_RetourneFalse()
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var (particulierUserId, missionId) = await PublierMissionAsync();
        var jeune = await CreerJeuneAvecCoachAsync();
        var coachNonLie = await CreerUtilisateurAsync($"coach-etranger-{Guid.NewGuid()}@test.local");
        await fixture.GarantirPublicationValideeAsync(jeune.CoachUserId, missionId);

        Assert.True(await missionService.AccepterMissionAsync(jeune.UserId, missionId));
        var demandes = await missionService.GetDemandesEnAttentePourJeuneSuiviAsync(jeune.CoachUserId, jeune.UserId);
        var acceptationId = demandes.Single().AcceptationId;

        Assert.False(await missionService.ValiderAcceptationAsync(coachNonLie, acceptationId));
        Assert.False(await missionService.RefuserAcceptationAsync(coachNonLie, acceptationId));

        await CleanupMissionAsync(missionId, particulierUserId, jeune);
    }

    [Fact]
    public async Task ValiderEtRefuser_NotifientSelonLesRegles()
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var notifService = fixture.Services.GetRequiredService<INotificationService>();
        var (particulierUserId, missionId) = await PublierMissionAsync();
        var jeune = await CreerJeuneAvecCoachAsync();

        await fixture.GarantirPublicationValideeAsync(jeune.CoachUserId, missionId);
        Assert.True(await missionService.AccepterMissionAsync(jeune.UserId, missionId));
        var demandes = await missionService.GetDemandesEnAttentePourJeuneSuiviAsync(jeune.CoachUserId, jeune.UserId);
        var acceptationId = demandes.Single().AcceptationId;

        var notifCoachDemande = Assert.Single(
            await notifService.GetRecentesAsync(jeune.CoachUserId, 20),
            n => n.TypeCode == "Missions.DemandeAcceptationEnAttente");
        Assert.Equal($"/coach/suivis/{jeune.UserId}/missions", notifCoachDemande.Lien);
        Assert.Contains("Léa", notifCoachDemande.Message);
        Assert.Contains("Test mission", notifCoachDemande.Message);

        // Refus → notif jeune MissionRefusee ; particulier sans aucune notif Missions
        Assert.True(await missionService.RefuserAcceptationAsync(jeune.CoachUserId, acceptationId));
        Assert.DoesNotContain(
            await notifService.GetRecentesAsync(particulierUserId, 20),
            n => n.TypeCode is "Missions.MissionValidee" or "Missions.MissionRefusee");

        var notifRefus = Assert.Single(
            await notifService.GetRecentesAsync(jeune.UserId, 20),
            n => n.TypeCode == "Missions.MissionRefusee");
        Assert.Equal("/jeune/missions-disponibles", notifRefus.Lien);
        Assert.Contains("Test mission", notifRefus.Message);
        Assert.DoesNotContain(
            await notifService.GetRecentesAsync(jeune.UserId, 20),
            n => n.TypeCode == "Missions.MissionValidee");

        // Nouvelle acceptation + validation → Validee pour particulier + jeune ; pas de Refusee supplémentaire
        Assert.True(await missionService.AccepterMissionAsync(jeune.UserId, missionId));
        demandes = await missionService.GetDemandesEnAttentePourJeuneSuiviAsync(jeune.CoachUserId, jeune.UserId);
        acceptationId = demandes.Single().AcceptationId;
        Assert.Equal(
            2,
            (await notifService.GetRecentesAsync(jeune.CoachUserId, 20))
                .Count(n => n.TypeCode == "Missions.DemandeAcceptationEnAttente"));
        Assert.True(await missionService.ValiderAcceptationAsync(jeune.CoachUserId, acceptationId));

        var notifParticulier = Assert.Single(
            await notifService.GetRecentesAsync(particulierUserId, 20),
            n => n.TypeCode == "Missions.MissionValidee");
        Assert.Equal("/particulier/mes-missions", notifParticulier.Lien);
        Assert.Contains("Test mission", notifParticulier.Message);
        Assert.Contains("Léa", notifParticulier.Message);
        Assert.DoesNotContain(
            await notifService.GetRecentesAsync(particulierUserId, 20),
            n => n.TypeCode == "Missions.MissionRefusee");

        var notifsJeune = await notifService.GetRecentesAsync(jeune.UserId, 20);
        var notifJeuneValidee = Assert.Single(notifsJeune, n => n.TypeCode == "Missions.MissionValidee");
        Assert.Equal("/jeune/mes-missions", notifJeuneValidee.Lien);
        Assert.Contains("Test mission", notifJeuneValidee.Message);
        // La Refusee du premier cycle reste ; la validation n'en ajoute pas une seconde
        Assert.Single(notifsJeune, n => n.TypeCode == "Missions.MissionRefusee");

        // Coach non autorisé ne crée aucune notif
        var (autreParticulier, autreMissionId) = await PublierMissionAsync();
        var autreJeune = await CreerJeuneAvecCoachAsync();
        await fixture.GarantirPublicationValideeAsync(autreJeune.CoachUserId, autreMissionId);
        Assert.True(await missionService.AccepterMissionAsync(autreJeune.UserId, autreMissionId));
        var autreDemandes = await missionService.GetDemandesEnAttentePourJeuneSuiviAsync(autreJeune.CoachUserId, autreJeune.UserId);
        var coachEtranger = await CreerUtilisateurAsync($"coach-notif-etranger-{Guid.NewGuid()}@test.local");
        Assert.False(await missionService.ValiderAcceptationAsync(coachEtranger, autreDemandes.Single().AcceptationId));
        Assert.False(await missionService.RefuserAcceptationAsync(coachEtranger, autreDemandes.Single().AcceptationId));
        Assert.DoesNotContain(
            await notifService.GetRecentesAsync(autreParticulier, 20),
            n => n.TypeCode is "Missions.MissionValidee" or "Missions.MissionRefusee");
        Assert.DoesNotContain(
            await notifService.GetRecentesAsync(autreJeune.UserId, 20),
            n => n.TypeCode is "Missions.MissionValidee" or "Missions.MissionRefusee");

        await CleanupMissionAsync(missionId, particulierUserId, jeune);
        await CleanupMissionAsync(autreMissionId, autreParticulier, autreJeune);
    }

    private static async Task AcceptAndStoreAsync(IMissionService missionService, string jeuneUserId, int missionId, bool[] results, int index)
    {
        results[index] = await missionService.AccepterMissionAsync(jeuneUserId, missionId);
    }

    private async Task<(string UserId, int MissionId)> PublierMissionAsync()
    {
        var particulierUserId = await CreerParticulierAsync("Martin", "Alice");
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var missionId = await missionService.PublierMissionAsync(
            particulierUserId,
            new PublierMissionInput("Test mission", "Description test", "Paris", "2 h", MissionDifficulte.Facile, 50m, "Organisation",
                MissionCategorie.Autre, MissionNiveauEncadrement.PresentPendantMission));
        Assert.NotNull(missionId);
        return (particulierUserId, missionId.Value);
    }

    private async Task<string> CreerParticulierAsync(string nom, string prenoms)
    {
        var userId = await CreerUtilisateurAsync($"particulier-{Guid.NewGuid()}@test.local");
        var particulierService = fixture.Services.GetRequiredService<IParticulierProfileService>();
        var moduleRegistry = fixture.Services.GetRequiredService<IModuleRegistry>();
        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();

        var particulierProfileId = await particulierService.GetOrCreateProfileIdAsync(userId, nom, prenoms);
        if (!await coreDb.ParticulierSubscriptions.AnyAsync(s => s.ParticulierProfileId == particulierProfileId))
        {
            coreDb.ParticulierSubscriptions.Add(new ParticulierSubscription
            {
                ParticulierProfileId = particulierProfileId,
                PlanCode = PlanCodes.Particulier,
                Status = SubscriptionStatus.Active,
            });
            await coreDb.SaveChangesAsync();
        }

        if (!await moduleRegistry.IsActiveForParticulierAsync(particulierProfileId, "ProfilParticulier", coreDb))
            await moduleRegistry.ActivateForParticulierAsync(particulierProfileId, "ProfilParticulier", coreDb);

        return userId;
    }

    private sealed record JeuneContext(string UserId, string CoachUserId);

    private async Task<JeuneContext> CreerJeuneAvecCoachAsync()
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
