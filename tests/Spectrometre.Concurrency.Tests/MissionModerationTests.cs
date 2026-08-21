using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Notifications;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.JeunesPrestataires.Services;
using Spectrometre.Modules.Missions.Data;
using Spectrometre.Modules.Missions.Entities;
using Spectrometre.Modules.Missions.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

[Collection("Base de données partagée")]
public sealed class MissionModerationTests(ServiceFixture fixture)
{
    [Fact]
    public async Task Publier_NEstPasDisponible_JusquaValidation()
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var (particulierUserId, missionId, coachUserId, jeuneUserId) = await PublierAvecCoachEtJeuneAsync();

        var publiees = await missionService.GetMesMissionsPublieesAsync(particulierUserId);
        var vue = Assert.Single(publiees, m => m.MissionId == missionId);
        Assert.Equal(MissionStatut.EnAttenteModeration, vue.Statut);

        Assert.DoesNotContain(await missionService.GetMissionsDisponiblesAsync(), m => m.MissionId == missionId);
        Assert.False(await missionService.AccepterMissionAsync(jeuneUserId, missionId));

        var notifService = fixture.Services.GetRequiredService<INotificationService>();
        Assert.DoesNotContain(
            await notifService.GetRecentesAsync(particulierUserId, 20),
            n => n.TypeCode is "Missions.PublicationValidee" or "Missions.PublicationRefusee");

        var file = await missionService.GetMissionsEnAttenteModerationAsync(coachUserId);
        var detail = Assert.Single(file, m => m.MissionId == missionId);
        Assert.True(detail.PresenceEscaliers);
        Assert.True(detail.PresenceAnimaux);
        Assert.Equal("Chien nerveux", detail.RisqueParticulier);

        Assert.True(await missionService.ValiderPublicationAsync(coachUserId, missionId));
        Assert.Contains(await missionService.GetMissionsDisponiblesAsync(), m => m.MissionId == missionId);
        Assert.True(await missionService.AccepterMissionAsync(jeuneUserId, missionId));

        var notifValidee = Assert.Single(
            await notifService.GetRecentesAsync(particulierUserId, 20),
            n => n.TypeCode == "Missions.PublicationValidee");
        Assert.Equal("/particulier/mes-missions", notifValidee.Lien);
        Assert.Contains("Modération", notifValidee.Message);
        Assert.DoesNotContain(
            await notifService.GetRecentesAsync(particulierUserId, 20),
            n => n.TypeCode == "Missions.PublicationRefusee");

        await CleanupAsync(missionId, particulierUserId);
    }

    [Fact]
    public async Task Refuser_MotifObligatoire_VisibleDuParticulier_JamaisDisponible()
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var (particulierUserId, missionId, coachUserId, jeuneUserId) = await PublierAvecCoachEtJeuneAsync();

        var notifService = fixture.Services.GetRequiredService<INotificationService>();
        Assert.False(await missionService.RefuserPublicationAsync(coachUserId, missionId, "   "));
        Assert.DoesNotContain(
            await notifService.GetRecentesAsync(particulierUserId, 20),
            n => n.TypeCode is "Missions.PublicationValidee" or "Missions.PublicationRefusee");
        Assert.True(await missionService.RefuserPublicationAsync(coachUserId, missionId, "Trop physique pour le cadre actuel"));

        Assert.DoesNotContain(await missionService.GetMissionsDisponiblesAsync(), m => m.MissionId == missionId);
        Assert.False(await missionService.AccepterMissionAsync(jeuneUserId, missionId));
        Assert.DoesNotContain(
            await missionService.GetMissionsEnAttenteModerationAsync(coachUserId),
            m => m.MissionId == missionId);

        var vue = Assert.Single(
            await missionService.GetMesMissionsPublieesAsync(particulierUserId),
            m => m.MissionId == missionId);
        Assert.Equal(MissionStatut.Annulee, vue.Statut);
        Assert.Equal("Trop physique pour le cadre actuel", vue.MotifAnnulation);

        var notifRefusee = Assert.Single(
            await notifService.GetRecentesAsync(particulierUserId, 20),
            n => n.TypeCode == "Missions.PublicationRefusee");
        Assert.Equal("/particulier/mes-missions", notifRefusee.Lien);
        Assert.Contains("Modération", notifRefusee.Message);
        Assert.Contains("Trop physique pour le cadre actuel", notifRefusee.Message);
        Assert.DoesNotContain(
            await notifService.GetRecentesAsync(particulierUserId, 20),
            n => n.TypeCode == "Missions.PublicationValidee");

        await CleanupAsync(missionId, particulierUserId);
    }

    [Fact]
    public async Task FilePartagee_AutreCoachPeutValider()
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var (particulierUserId, missionId, _, jeuneUserId) = await PublierAvecCoachEtJeuneAsync();
        var autreCoach = await CreerCoachAvecJeuneAsync();

        Assert.False(await missionService.ValiderPublicationAsync(particulierUserId, missionId));
        var notifService = fixture.Services.GetRequiredService<INotificationService>();
        Assert.DoesNotContain(
            await notifService.GetRecentesAsync(particulierUserId, 20),
            n => n.TypeCode is "Missions.PublicationValidee" or "Missions.PublicationRefusee");
        Assert.Empty(await missionService.GetMissionsEnAttenteModerationAsync("user-inconnu"));
        Assert.Contains(
            await missionService.GetMissionsEnAttenteModerationAsync(autreCoach.CoachUserId),
            m => m.MissionId == missionId);
        Assert.True(await missionService.ValiderPublicationAsync(autreCoach.CoachUserId, missionId));
        Assert.True(await missionService.AccepterMissionAsync(jeuneUserId, missionId));

        await CleanupAsync(missionId, particulierUserId);
    }

    private async Task<(string ParticulierUserId, int MissionId, string CoachUserId, string JeuneUserId)> PublierAvecCoachEtJeuneAsync()
    {
        var jeune = await CreerCoachAvecJeuneAsync();
        var particulierUserId = await CreerParticulierAsync();
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var missionId = await missionService.PublierMissionAsync(
            particulierUserId,
            new PublierMissionInput(
                "Modération",
                "Desc",
                "Lyon",
                "2 h",
                MissionDifficulte.Intermediaire,
                20m,
                null,
                MissionCategorie.AideDemenagementLeger,
                MissionNiveauEncadrement.PresentPendantMission,
                PresenceEscaliers: true,
                PresenceAnimaux: true,
                PortDeCharge: true,
                AccesDifficile: false,
                RisqueParticulier: "Chien nerveux"));
        Assert.NotNull(missionId);
        return (particulierUserId, missionId.Value, jeune.CoachUserId, jeune.JeuneUserId);
    }

    private sealed record CoachJeune(string CoachUserId, string JeuneUserId);

    private async Task<CoachJeune> CreerCoachAvecJeuneAsync()
    {
        var coachUserId = await CreerUtilisateurAsync($"coach-mod-{Guid.NewGuid()}@test.local");
        var jeuneEmail = $"jeune-mod-{Guid.NewGuid()}@test.local";
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();
        var coachingService = fixture.Services.GetRequiredService<ICoachingService>();
        var invite = await jeuneService.InviterJeuneAsync(
            coachUserId, jeuneEmail, "Mod", "Test",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18)), "http://localhost");
        Assert.True(invite.Success);
        var jeuneId = await CreerUtilisateurAsync(jeuneEmail);
        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();
        var invitation = await coreDb.Invitations.FirstAsync(i => i.Id == invite.Invitation!.Id);
        await jeuneService.FinaliserDepuisInvitationAsync(invitation, jeuneId);
        await coachingService.FinaliserJeunePrestataireDepuisInvitationAsync(invitation, jeuneId);
        await fixture.GarantirCharteAccepteeAsync(jeuneId);
        return new CoachJeune(coachUserId, jeuneId);
    }

    private async Task<string> CreerParticulierAsync()
    {
        var userId = await CreerUtilisateurAsync($"part-mod-{Guid.NewGuid()}@test.local");
        var particulierService = fixture.Services.GetRequiredService<IParticulierProfileService>();
        var moduleRegistry = fixture.Services.GetRequiredService<IModuleRegistry>();
        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();
        var particulierProfileId = await particulierService.GetOrCreateProfileIdAsync(userId, "Part", "Mod");
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

    private async Task CleanupAsync(int missionId, string particulierUserId)
    {
        await using var db = await fixture.Services.GetRequiredService<IDbContextFactory<MissionsDbContext>>().CreateDbContextAsync();
        var mission = await db.Missions.Include(m => m.Acceptations).FirstOrDefaultAsync(m => m.Id == missionId);
        if (mission is not null)
        {
            db.MissionAcceptations.RemoveRange(mission.Acceptations);
            db.Missions.Remove(mission);
            await db.SaveChangesAsync();
        }

        var particulier = fixture.Services.GetRequiredService<IParticulierProfileService>();
        var profil = await particulier.TryGetByUserIdAsync(particulierUserId);
        if (profil is not null)
        {
            await using var db2 = await fixture.Services.GetRequiredService<IDbContextFactory<MissionsDbContext>>().CreateDbContextAsync();
            var p = await db2.ParticulierProfiles.FirstOrDefaultAsync(x => x.Id == profil.Id);
            if (p is not null)
            {
                db2.ParticulierProfiles.Remove(p);
                await db2.SaveChangesAsync();
            }
        }
    }
}
