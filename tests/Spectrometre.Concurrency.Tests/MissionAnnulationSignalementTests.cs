using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Invitations;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Notifications;
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
public sealed class MissionAnnulationSignalementTests(ServiceFixture fixture)
{
    [Fact]
    public async Task AnnulerMission_UniquementDisponible_EtProprietaire()
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var (particulierUserId, missionId) = await PublierMissionAsync();
        var autreParticulier = await CreerParticulierAsync();
        var jeune = await CreerJeuneAvecCoachAsync();

        Assert.False(await missionService.AnnulerMissionAsync(autreParticulier, missionId));

        await fixture.GarantirPublicationValideeAsync(jeune.CoachUserId, missionId);
        Assert.True(await missionService.AccepterMissionAsync(jeune.UserId, missionId));
        Assert.False(await missionService.AnnulerMissionAsync(particulierUserId, missionId));

        var acceptationId = (await missionService.GetDemandesEnAttentePourJeuneSuiviAsync(jeune.CoachUserId, jeune.UserId))
            .Single().AcceptationId;
        Assert.True(await missionService.ValiderAcceptationAsync(jeune.CoachUserId, acceptationId));
        Assert.False(await missionService.AnnulerMissionAsync(particulierUserId, missionId));

        // Remet une mission Disponible pour tester l'annulation OK
        var missionDispoId = await PublierMissionPourAsync(particulierUserId);
        Assert.True(await missionService.AnnulerMissionAsync(particulierUserId, missionDispoId));
        await using (var db = await fixture.Services.GetRequiredService<IDbContextFactory<MissionsDbContext>>().CreateDbContextAsync())
        {
            var mission = await db.Missions.AsNoTracking().FirstAsync(m => m.Id == missionDispoId);
            Assert.Equal(MissionStatut.Annulee, mission.Statut);
        }

        var disponibles = await missionService.GetMissionsDisponiblesAsync();
        Assert.DoesNotContain(disponibles, m => m.MissionId == missionDispoId);
        Assert.False(await missionService.AnnulerMissionAsync(particulierUserId, missionDispoId));

        await CleanupMissionsAsync([missionId, missionDispoId], particulierUserId, autreParticulier);
    }

    [Fact]
    public async Task AnnulerMissionAttribuee_NotifieJeuneEtCoach_RefuseSiTermineeOuAutreProprietaire()
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var notifService = fixture.Services.GetRequiredService<INotificationService>();
        var (particulierUserId, missionId) = await PublierMissionAsync();
        var autreParticulier = await CreerParticulierAsync();
        var jeune = await CreerJeuneAvecCoachAsync();

        await fixture.GarantirPublicationValideeAsync(jeune.CoachUserId, missionId);
        Assert.True(await missionService.AccepterMissionAsync(jeune.UserId, missionId));
        var acceptationId = (await missionService.GetDemandesEnAttentePourJeuneSuiviAsync(jeune.CoachUserId, jeune.UserId))
            .Single().AcceptationId;
        Assert.True(await missionService.ValiderAcceptationAsync(jeune.CoachUserId, acceptationId));

        Assert.False(await missionService.AnnulerMissionAttribueeAsync(particulierUserId, missionId, "   "));
        Assert.False(await missionService.AnnulerMissionAttribueeAsync(autreParticulier, missionId, "Pas chez moi"));

        const string motif = "Empêchement familial imprévu";
        Assert.True(await missionService.AnnulerMissionAttribueeAsync(particulierUserId, missionId, motif));

        await using (var db = await fixture.Services.GetRequiredService<IDbContextFactory<MissionsDbContext>>().CreateDbContextAsync())
        {
            var mission = await db.Missions.AsNoTracking().FirstAsync(m => m.Id == missionId);
            Assert.Equal(MissionStatut.Annulee, mission.Statut);
            Assert.Equal(motif, mission.MotifAnnulation);
            var acceptation = await db.MissionAcceptations.AsNoTracking().FirstAsync(a => a.Id == acceptationId);
            Assert.Equal(MissionAcceptationStatut.AnnuleeParParticulier, acceptation.Statut);
        }

        var notifJeune = Assert.Single(
            await notifService.GetRecentesAsync(jeune.UserId, 20),
            n => n.TypeCode == "Missions.MissionAnnuleeParParticulier");
        Assert.Equal("/jeune/mes-missions", notifJeune.Lien);
        Assert.Contains(motif, notifJeune.Message, StringComparison.Ordinal);

        var notifCoach = Assert.Single(
            await notifService.GetRecentesAsync(jeune.CoachUserId, 20),
            n => n.TypeCode == "Missions.MissionAnnuleeParParticulier");
        Assert.Equal($"/coach/suivis/{jeune.UserId}/missions", notifCoach.Lien);
        Assert.Contains(motif, notifCoach.Message, StringComparison.Ordinal);

        Assert.False(await missionService.AnnulerMissionAttribueeAsync(particulierUserId, missionId, motif));

        var missionTermineeId = await PublierMissionPourAsync(particulierUserId);
        await fixture.GarantirPublicationValideeAsync(jeune.CoachUserId, missionTermineeId);
        Assert.True(await missionService.AccepterMissionAsync(jeune.UserId, missionTermineeId));
        var accTermineeId = (await missionService.GetDemandesEnAttentePourJeuneSuiviAsync(jeune.CoachUserId, jeune.UserId))
            .Single().AcceptationId;
        Assert.True(await missionService.ValiderAcceptationAsync(jeune.CoachUserId, accTermineeId));
        Assert.True(await missionService.MarquerTermineeAsync(jeune.UserId, accTermineeId));
        Assert.False(await missionService.AnnulerMissionAttribueeAsync(
            particulierUserId, missionTermineeId, "Trop tard"));

        await CleanupMissionsAsync([missionId, missionTermineeId], particulierUserId, autreParticulier);
    }

    [Fact]
    public async Task SignalerProbleme_NotifieCoach_EchoueSiNonAttribueeOuAutreProprietaire()
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var notifService = fixture.Services.GetRequiredService<INotificationService>();
        var (particulierUserId, missionId) = await PublierMissionAsync();
        var jeune = await CreerJeuneAvecCoachAsync();

        Assert.False(await missionService.SignalerProblemeAsync(particulierUserId, missionId, "trop tôt"));

        await fixture.GarantirPublicationValideeAsync(jeune.CoachUserId, missionId);
        Assert.True(await missionService.AccepterMissionAsync(jeune.UserId, missionId));
        var acceptationId = (await missionService.GetDemandesEnAttentePourJeuneSuiviAsync(jeune.CoachUserId, jeune.UserId))
            .Single().AcceptationId;
        Assert.True(await missionService.ValiderAcceptationAsync(jeune.CoachUserId, acceptationId));

        Assert.True(await missionService.SignalerProblemeAsync(
            particulierUserId, missionId, "Le jeune est en retard"));

        var notifsCoach = await notifService.GetRecentesAsync(jeune.CoachUserId, 20);
        var notif = Assert.Single(notifsCoach, n => n.TypeCode == "Missions.ProblemeSignale");
        Assert.Contains("retard", notif.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal($"/coach/suivis/{jeune.UserId}/missions", notif.Lien);

        var autre = await CreerParticulierAsync();
        Assert.False(await missionService.SignalerProblemeAsync(autre, missionId, "x"));

        await CleanupMissionsAsync([missionId], particulierUserId, autre);
    }

    [Fact]
    public async Task SignalerProbleme_SansCoachActif_EchoueProprement()
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var coachingService = fixture.Services.GetRequiredService<ICoachingService>();
        var notifService = fixture.Services.GetRequiredService<INotificationService>();
        var (particulierUserId, missionId) = await PublierMissionAsync();
        var jeune = await CreerJeuneAvecCoachAsync();

        await fixture.GarantirPublicationValideeAsync(jeune.CoachUserId, missionId);
        Assert.True(await missionService.AccepterMissionAsync(jeune.UserId, missionId));
        var acceptationId = (await missionService.GetDemandesEnAttentePourJeuneSuiviAsync(jeune.CoachUserId, jeune.UserId))
            .Single().AcceptationId;
        Assert.True(await missionService.ValiderAcceptationAsync(jeune.CoachUserId, acceptationId));

        var liens = await coachingService.GetLiensPourSuiviAsync(jeune.UserId);
        var lienActif = Assert.Single(liens, l => l.Statut == LienCoachingStatut.Actif);
        Assert.True(await coachingService.RevoquerAsync(lienActif.Id, jeune.UserId));

        Assert.False(await missionService.SignalerProblemeAsync(particulierUserId, missionId, "plus de coach"));

        var notifsCoach = await notifService.GetRecentesAsync(jeune.CoachUserId, 20);
        Assert.DoesNotContain(notifsCoach, n => n.TypeCode == "Missions.ProblemeSignale");

        await CleanupMissionsAsync([missionId], particulierUserId);
    }

    [Fact]
    public async Task DemanderContactCoach_TitulaireAttribuee_Notifie_SinonRefuse()
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var notifService = fixture.Services.GetRequiredService<INotificationService>();
        var (particulierUserId, missionId) = await PublierMissionAsync();
        var jeune = await CreerJeuneAvecCoachAsync();
        var autreJeune = await CreerJeuneAvecCoachAsync();

        await fixture.GarantirPublicationValideeAsync(jeune.CoachUserId, missionId);
        Assert.True(await missionService.AccepterMissionAsync(jeune.UserId, missionId));
        var acceptationId = (await missionService.GetDemandesEnAttentePourJeuneSuiviAsync(jeune.CoachUserId, jeune.UserId))
            .Single().AcceptationId;

        Assert.False(await missionService.DemanderContactCoachAsync(jeune.UserId, acceptationId, "trop tôt"));
        Assert.False(await missionService.SignalerProblemeJeuneAsync(jeune.UserId, acceptationId, "trop tôt"));

        Assert.True(await missionService.ValiderAcceptationAsync(jeune.CoachUserId, acceptationId));

        Assert.False(await missionService.DemanderContactCoachAsync(autreJeune.UserId, acceptationId, "pas moi"));
        Assert.True(await missionService.DemanderContactCoachAsync(jeune.UserId, acceptationId, "Merci de me rappeler"));

        var notifsCoach = await notifService.GetRecentesAsync(jeune.CoachUserId, 20);
        var contact = Assert.Single(notifsCoach, n => n.TypeCode == "Missions.DemandeContact");
        Assert.Contains("rappeler", contact.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Le jeune", contact.Message);
        Assert.Equal($"/coach/suivis/{jeune.UserId}/missions", contact.Lien);

        Assert.True(await missionService.SignalerProblemeJeuneAsync(jeune.UserId, acceptationId, "Ça bloque"));
        var probleme = Assert.Single(
            await notifService.GetRecentesAsync(jeune.CoachUserId, 20),
            n => n.TypeCode == "Missions.ProblemeSignale");
        Assert.Contains("bloque", probleme.Message, StringComparison.OrdinalIgnoreCase);

        Assert.True(await missionService.MarquerTermineeAsync(jeune.UserId, acceptationId));
        Assert.True(await missionService.DemanderContactCoachAsync(jeune.UserId, acceptationId, "question après coup"));

        await CleanupMissionsAsync([missionId], particulierUserId);
    }

    [Fact]
    public async Task DemanderContactParticulier_TypeCodeDistinctDuProbleme()
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var notifService = fixture.Services.GetRequiredService<INotificationService>();
        var (particulierUserId, missionId) = await PublierMissionAsync();
        var jeune = await CreerJeuneAvecCoachAsync();

        Assert.False(await missionService.DemanderContactParticulierAsync(particulierUserId, missionId, "trop tôt"));

        await fixture.GarantirPublicationValideeAsync(jeune.CoachUserId, missionId);
        Assert.True(await missionService.AccepterMissionAsync(jeune.UserId, missionId));
        var acceptationId = (await missionService.GetDemandesEnAttentePourJeuneSuiviAsync(jeune.CoachUserId, jeune.UserId))
            .Single().AcceptationId;
        Assert.True(await missionService.ValiderAcceptationAsync(jeune.CoachUserId, acceptationId));

        Assert.True(await missionService.DemanderContactParticulierAsync(
            particulierUserId, missionId, "J'ai une question"));
        var notifs = await notifService.GetRecentesAsync(jeune.CoachUserId, 20);
        var contact = Assert.Single(notifs, n => n.TypeCode == "Missions.DemandeContact");
        Assert.Contains("Le particulier", contact.Message);
        Assert.DoesNotContain(notifs, n => n.TypeCode == "Missions.ProblemeSignale");

        await CleanupMissionsAsync([missionId], particulierUserId);
    }

    private async Task<(string UserId, int MissionId)> PublierMissionAsync()
    {
        var particulierUserId = await CreerParticulierAsync();
        var missionId = await PublierMissionPourAsync(particulierUserId);
        return (particulierUserId, missionId);
    }

    private async Task<int> PublierMissionPourAsync(string particulierUserId)
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var missionId = await missionService.PublierMissionAsync(
            particulierUserId,
            new PublierMissionInput(
                "Mission signal", "Desc", null, null, MissionDifficulte.Facile, 20m, null,
                MissionCategorie.Autre, MissionNiveauEncadrement.PresentPendantMission));
        Assert.NotNull(missionId);
        return missionId.Value;
    }

    private async Task<string> CreerParticulierAsync()
    {
        var userId = await CreerUtilisateurAsync($"part-sig-{Guid.NewGuid()}@test.local");
        var particulierService = fixture.Services.GetRequiredService<IParticulierProfileService>();
        var moduleRegistry = fixture.Services.GetRequiredService<IModuleRegistry>();
        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();

        var profileId = await particulierService.GetOrCreateProfileIdAsync(userId, "Part", "Sig");
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
        var coachUserId = await CreerUtilisateurAsync($"coach-sig-{Guid.NewGuid()}@test.local");
        var jeuneEmail = $"jeune-sig-{Guid.NewGuid()}@test.local";
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();
        var coachingService = fixture.Services.GetRequiredService<ICoachingService>();

        var invite = await jeuneService.InviterJeuneAsync(
            coachUserId,
            jeuneEmail,
            "Sig",
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
