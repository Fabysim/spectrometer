using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Invitations;
using Spectrometre.Core.Notifications;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.JeunesPrestataires.Services;
using Spectrometre.Modules.Missions.Entities;
using Spectrometre.Modules.Missions.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

[Collection("Base de données partagée")]
public sealed class CoachDashboardSyntheseTests(ServiceFixture fixture)
{
    [Fact]
    public async Task GetSyntheseAsync_CompteJeunesMissionsDossiersEtAlertes()
    {
        var coachId = await CreerUtilisateurAsync($"coach-dash-{Guid.NewGuid()}@test.local");
        var dashboard = fixture.Services.GetRequiredService<ICoachDashboardService>();

        // Jeune 1 — mineur, sans consentement → dossier incomplet
        var jeune1 = await CreerJeunePourCoachAsync(coachId, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-15)), "A");
        // Jeune 2 — majeur → suivi actif, pas de dossier incomplet
        await CreerJeunePourCoachAsync(coachId, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-20)), "B");

        var particulierId = await CreerParticulierAsync();
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var missionId = await missionService.PublierMissionAsync(
            particulierId,
            new PublierMissionInput("Mission dash", "Desc", null, null, MissionDifficulte.Facile, 20m, null,
                MissionCategorie.Autre, MissionNiveauEncadrement.PresentPendantMission));
        Assert.NotNull(missionId);
        await fixture.GarantirPublicationValideeAsync(coachId, missionId.Value);
        Assert.True(await missionService.AccepterMissionAsync(jeune1.UserId, missionId.Value));

        // Invitation expirée (alerte) — pas d'acceptation
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();
        var invite = await jeuneService.InviterJeuneAsync(
            coachId,
            $"invite-expiree-{Guid.NewGuid()}@test.local",
            "Expire",
            "Test",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)),
            "http://localhost");
        Assert.True(invite.Success);
        await using (var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync())
        {
            var invitation = await coreDb.Invitations.FirstAsync(i => i.Id == invite.Invitation!.Id);
            invitation.ExpireLe = DateTimeOffset.UtcNow.AddDays(-1);
            await coreDb.SaveChangesAsync();
        }

        var synth = await dashboard.GetSyntheseAsync(coachId);

        Assert.Equal(2, synth.JeunesSuivisActifs);
        Assert.Equal(1, synth.MissionsAValider);
        Assert.Equal(1, synth.DossiersIncomplets);
        Assert.Equal(1, synth.AlertesInvitationsExpirees);
        Assert.Equal(0, synth.SignalementsEtDemandesNonLus);
    }

    [Fact]
    public async Task GetSyntheseAsync_SyntheseNonValidee_CompteDossierIncomplet_UneSeuleFoisSiCumul()
    {
        var coachId = await CreerUtilisateurAsync($"coach-dash-syn-{Guid.NewGuid()}@test.local");
        var dashboard = fixture.Services.GetRequiredService<ICoachDashboardService>();
        var aoSvc = fixture.Services.GetRequiredService<IAutoObservationService>();

        var majeurSansSynthese = await CreerJeunePourCoachAsync(
            coachId, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-20)), "M0");
        Assert.Equal(0, (await dashboard.GetSyntheseAsync(coachId)).DossiersIncomplets);

        Assert.NotNull(await aoSvc.RegenererSyntheseAsync(coachId, majeurSansSynthese.ProfileId));
        Assert.Equal(1, (await dashboard.GetSyntheseAsync(coachId)).DossiersIncomplets);

        Assert.True(await aoSvc.ValiderSyntheseAsync(coachId, majeurSansSynthese.ProfileId));
        Assert.Equal(0, (await dashboard.GetSyntheseAsync(coachId)).DossiersIncomplets);

        var mineurSansConsentement = await CreerJeunePourCoachAsync(
            coachId, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-15)), "Min");
        Assert.Equal(1, (await dashboard.GetSyntheseAsync(coachId)).DossiersIncomplets);

        Assert.NotNull(await aoSvc.RegenererSyntheseAsync(coachId, mineurSansConsentement.ProfileId));
        Assert.Equal(1, (await dashboard.GetSyntheseAsync(coachId)).DossiersIncomplets);
    }

    [Fact]
    public async Task GetSyntheseAsync_CompteSignalementsEtDemandesNonLus()
    {
        var coachId = await CreerUtilisateurAsync($"coach-dash-sig-{Guid.NewGuid()}@test.local");
        var dashboard = fixture.Services.GetRequiredService<ICoachDashboardService>();
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var notifService = fixture.Services.GetRequiredService<INotificationService>();

        var jeune = await CreerJeunePourCoachAsync(coachId, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)), "S");
        var particulierId = await CreerParticulierAsync();
        var missionId = await missionService.PublierMissionAsync(
            particulierId,
            new PublierMissionInput("Mission sig", "Desc", null, null, MissionDifficulte.Facile, 20m, null,
                MissionCategorie.Rangement, MissionNiveauEncadrement.PresentPendantMission));
        Assert.NotNull(missionId);
        await fixture.GarantirPublicationValideeAsync(coachId, missionId.Value);
        Assert.True(await missionService.AccepterMissionAsync(jeune.UserId, missionId.Value));
        var acceptationId = (await missionService.GetDemandesEnAttentePourJeuneSuiviAsync(coachId, jeune.UserId))
            .Single().AcceptationId;
        Assert.True(await missionService.ValiderAcceptationAsync(coachId, acceptationId));

        Assert.Equal(0, (await dashboard.GetSyntheseAsync(coachId)).SignalementsEtDemandesNonLus);

        Assert.NotEqual(0, await notifService.CreerAsync(
            coachId, "Autre", "Ignore", null, "Missions.MissionValidee"));
        Assert.Equal(0, (await dashboard.GetSyntheseAsync(coachId)).SignalementsEtDemandesNonLus);

        Assert.True(await missionService.SignalerProblemeAsync(particulierId, missionId.Value, "retard"));
        Assert.Equal(1, (await dashboard.GetSyntheseAsync(coachId)).SignalementsEtDemandesNonLus);

        Assert.True(await missionService.DemanderContactCoachAsync(jeune.UserId, acceptationId, "rappel"));
        Assert.Equal(2, (await dashboard.GetSyntheseAsync(coachId)).SignalementsEtDemandesNonLus);

        await notifService.MarquerToutesLuesAsync(coachId);
        Assert.Equal(0, (await dashboard.GetSyntheseAsync(coachId)).SignalementsEtDemandesNonLus);
    }

    private async Task<(string UserId, int ProfileId)> CreerJeunePourCoachAsync(string coachId, DateOnly dateNaissance, string suffix)
    {
        var jeuneEmail = $"jeune-dash-{suffix}-{Guid.NewGuid()}@test.local";
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();
        var coachingService = fixture.Services.GetRequiredService<ICoachingService>();

        var invite = await jeuneService.InviterJeuneAsync(
            coachId,
            jeuneEmail,
            "Dash",
            suffix,
            dateNaissance,
            "http://localhost");
        Assert.True(invite.Success);

        var jeuneId = await CreerUtilisateurAsync(jeuneEmail);
        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();
        var invitation = await coreDb.Invitations.FirstAsync(i => i.Id == invite.Invitation!.Id);
        var profile = await jeuneService.FinaliserDepuisInvitationAsync(invitation, jeuneId);
        await coachingService.FinaliserJeunePrestataireDepuisInvitationAsync(invitation, jeuneId);
        await fixture.GarantirCharteAccepteeAsync(jeuneId);
        return (jeuneId, profile.Id);
    }

    private async Task<string> CreerParticulierAsync()
    {
        var userId = await CreerUtilisateurAsync($"part-dash-{Guid.NewGuid()}@test.local");
        var particulierService = fixture.Services.GetRequiredService<IParticulierProfileService>();
        var moduleRegistry = fixture.Services.GetRequiredService<Spectrometre.Core.Modules.IModuleRegistry>();
        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();

        var profileId = await particulierService.GetOrCreateProfileIdAsync(userId, "Part", "Dash");
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
