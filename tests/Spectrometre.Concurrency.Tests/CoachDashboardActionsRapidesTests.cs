using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Invitations;
using Spectrometre.Modules.Coaching.Entities;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.JeunesPrestataires.Services;
using Spectrometre.Modules.Missions.Entities;
using Spectrometre.Modules.Missions.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

[Collection("Base de données partagée")]
public sealed class CoachDashboardActionsRapidesTests(ServiceFixture fixture)
{
    [Fact]
    public async Task GetActionsRapides_ValiderMission_PointeVersLeJeuneALaPlusAncienneAcceptation()
    {
        var coachId = await CreerUtilisateurAsync($"coach-ar-miss-{Guid.NewGuid()}@test.local");
        var jeuneRecent = await CreerJeunePourCoachAsync(coachId, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18)), "recent");
        var jeuneAncien = await CreerJeunePourCoachAsync(coachId, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-19)), "ancien");

        var particulierId = await CreerParticulierAsync();
        var missionService = fixture.Services.GetRequiredService<IMissionService>();

        var missionRecentId = await missionService.PublierMissionAsync(
            particulierId,
            new PublierMissionInput("Mission récente", "D", null, null, MissionDifficulte.Facile, 10m, null,
                MissionCategorie.Autre, MissionNiveauEncadrement.PresentPendantMission));
        var missionAncienneId = await missionService.PublierMissionAsync(
            particulierId,
            new PublierMissionInput("Mission ancienne", "D", null, null, MissionDifficulte.Facile, 10m, null,
                MissionCategorie.Autre, MissionNiveauEncadrement.PresentPendantMission));
        Assert.NotNull(missionRecentId);
        Assert.NotNull(missionAncienneId);

        Assert.True(await missionService.AccepterMissionAsync(jeuneAncien.UserId, missionAncienneId.Value));
        await Task.Delay(50);
        Assert.True(await missionService.AccepterMissionAsync(jeuneRecent.UserId, missionRecentId.Value));

        // Forcer AccepteeLe pour garantir l'ordre indépendamment du timing du test.
        using (var scope = fixture.Services.CreateScope())
        {
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<Spectrometre.Modules.Missions.Data.MissionsDbContext>>();
            await using var db = await dbFactory.CreateDbContextAsync();
            var accAncienne = await db.MissionAcceptations.FirstAsync(a => a.MissionId == missionAncienneId.Value);
            var accRecente = await db.MissionAcceptations.FirstAsync(a => a.MissionId == missionRecentId.Value);
            accAncienne.AccepteeLe = DateTimeOffset.UtcNow.AddHours(-2);
            accRecente.AccepteeLe = DateTimeOffset.UtcNow.AddMinutes(-5);
            await db.SaveChangesAsync();
        }

        var actions = await fixture.Services.GetRequiredService<ICoachDashboardService>()
            .GetActionsRapidesAsync(coachId);

        Assert.Equal($"/coach/suivis/{jeuneAncien.UserId}/missions", actions.ValiderMissionHref);
        Assert.NotEqual($"/coach/suivis/{jeuneRecent.UserId}/missions", actions.ValiderMissionHref);
    }

    [Fact]
    public async Task GetActionsRapides_ValiderMission_NullApresDecisionCoach()
    {
        var coachId = await CreerUtilisateurAsync($"coach-ar-dec-{Guid.NewGuid()}@test.local");
        var jeune = await CreerJeunePourCoachAsync(coachId, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18)), "dec");
        var particulierId = await CreerParticulierAsync();
        var missionService = fixture.Services.GetRequiredService<IMissionService>();

        var missionId = await missionService.PublierMissionAsync(
            particulierId,
            new PublierMissionInput("Mission à valider", "D", null, null, MissionDifficulte.Facile, 10m, null,
                MissionCategorie.Autre, MissionNiveauEncadrement.PresentPendantMission));
        Assert.NotNull(missionId);
        Assert.True(await missionService.AccepterMissionAsync(jeune.UserId, missionId.Value));

        var avant = await fixture.Services.GetRequiredService<ICoachDashboardService>()
            .GetActionsRapidesAsync(coachId);
        Assert.Equal($"/coach/suivis/{jeune.UserId}/missions", avant.ValiderMissionHref);

        var demandes = await missionService.GetDemandesEnAttentePourCoachAsync(coachId);
        var acceptationId = Assert.Single(demandes).AcceptationId;
        Assert.True(await missionService.ValiderAcceptationAsync(coachId, acceptationId));

        var apres = await fixture.Services.GetRequiredService<ICoachDashboardService>()
            .GetActionsRapidesAsync(coachId);
        Assert.Null(apres.ValiderMissionHref);
    }

    [Fact]
    public async Task GetActionsRapides_GuideEntrevue_PrefereJeuneSansGuidePuisContinuer()
    {
        var coachId = await CreerUtilisateurAsync($"coach-ar-guide-{Guid.NewGuid()}@test.local");
        var jeuneAvecGuide = await CreerJeunePourCoachAsync(coachId, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18)), "g1");
        await Task.Delay(30);
        var jeuneSansGuide = await CreerJeunePourCoachAsync(coachId, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-19)), "g2");

        var guideSvc = fixture.Services.GetRequiredService<IGuideEntrevueService>();
        Assert.True(await guideSvc.SaveAsync(
            coachId,
            jeuneAvecGuide.ProfileId,
            "Motivations",
            null,
            null,
            null,
            []));

        var actions = await fixture.Services.GetRequiredService<ICoachDashboardService>()
            .GetActionsRapidesAsync(coachId);

        Assert.Equal($"/coach/suivis/{jeuneSansGuide.UserId}/guide-entrevue", actions.GuideEntrevueHref);
        Assert.False(actions.GuideEntrevueEstContinuer);

        Assert.True(await guideSvc.SaveAsync(
            coachId,
            jeuneSansGuide.ProfileId,
            "Aussi",
            null,
            null,
            null,
            []));

        var apres = await fixture.Services.GetRequiredService<ICoachDashboardService>()
            .GetActionsRapidesAsync(coachId);

        // Tous ont un guide → Continuer sur le premier suivi (CreatedAt le plus ancien = jeuneAvecGuide).
        Assert.Equal($"/coach/suivis/{jeuneAvecGuide.UserId}/guide-entrevue", apres.GuideEntrevueHref);
        Assert.True(apres.GuideEntrevueEstContinuer);
    }

    [Fact]
    public async Task GetActionsRapides_RelancerInvitation_AncreQuandExpireeSinonNull()
    {
        var coachId = await CreerUtilisateurAsync($"coach-ar-inv-{Guid.NewGuid()}@test.local");
        await CreerJeunePourCoachAsync(coachId, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18)), "inv");

        var sansAlerte = await fixture.Services.GetRequiredService<ICoachDashboardService>()
            .GetActionsRapidesAsync(coachId);
        Assert.Null(sansAlerte.RelancerInvitationHref);

        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();
        var invite = await jeuneService.InviterJeuneAsync(
            coachId,
            $"invite-ar-{Guid.NewGuid()}@test.local",
            "Expire",
            "Ar",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)),
            "http://localhost");
        Assert.True(invite.Success);
        await using (var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync())
        {
            var invitation = await coreDb.Invitations.FirstAsync(i => i.Id == invite.Invitation!.Id);
            invitation.ExpireLe = DateTimeOffset.UtcNow.AddDays(-1);
            await coreDb.SaveChangesAsync();
        }

        var avecAlerte = await fixture.Services.GetRequiredService<ICoachDashboardService>()
            .GetActionsRapidesAsync(coachId);
        Assert.Equal("/coach/suivis#invitations-jeunes", avecAlerte.RelancerInvitationHref);
    }

    [Fact]
    public async Task GetActionsRapides_CloturerObjectifs_PointeVersLePremierLienAvecObjectifOuvert()
    {
        var coachId = await CreerUtilisateurAsync($"coach-ar-obj-{Guid.NewGuid()}@test.local");
        var jeuneSansObj = await CreerJeunePourCoachAsync(coachId, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18)), "o1");
        await Task.Delay(30);
        var jeuneAvecObj = await CreerJeunePourCoachAsync(coachId, DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-19)), "o2");

        var coaching = fixture.Services.GetRequiredService<ICoachingService>();
        var liens = await coaching.GetLiensPourCoachAsync(coachId);
        var lienSans = Assert.Single(liens, l => l.SuiviUserId == jeuneSansObj.UserId);
        var lienAvec = Assert.Single(liens, l => l.SuiviUserId == jeuneAvecObj.UserId);

        var objectifs = fixture.Services.GetRequiredService<IObjectifsCoachingService>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Période vide (créée) sur le premier lien — ne doit PAS déclencher l'action.
        Assert.NotNull(await objectifs.GetPeriodeCouranteAsync(lienSans.Id, coachId));

        Assert.True(await objectifs.SaveObjectifsAsync(lienAvec.Id, coachId, [
            new ObjectifCoachingInput(null, today, "Objectif ouvert", null, AtteinteObjectifCoaching.NonDefini, null, null),
        ]));

        var actions = await fixture.Services.GetRequiredService<ICoachDashboardService>()
            .GetActionsRapidesAsync(coachId);

        Assert.Equal($"/coach/objectifs/{lienAvec.Id}", actions.CloturerObjectifsHref);
        Assert.NotEqual($"/coach/objectifs/{lienSans.Id}", actions.CloturerObjectifsHref);
    }

    private async Task<(string UserId, int ProfileId)> CreerJeunePourCoachAsync(string coachId, DateOnly dateNaissance, string suffix)
    {
        var jeuneEmail = $"jeune-ar-{suffix}-{Guid.NewGuid()}@test.local";
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();
        var coachingService = fixture.Services.GetRequiredService<ICoachingService>();

        var invite = await jeuneService.InviterJeuneAsync(
            coachId,
            jeuneEmail,
            "Ar",
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
        var userId = await CreerUtilisateurAsync($"part-ar-{Guid.NewGuid()}@test.local");
        var particulierService = fixture.Services.GetRequiredService<IParticulierProfileService>();
        var moduleRegistry = fixture.Services.GetRequiredService<Spectrometre.Core.Modules.IModuleRegistry>();
        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();

        var profileId = await particulierService.GetOrCreateProfileIdAsync(userId, "Part", "Ar");
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
