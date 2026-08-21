using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Invitations;
using Spectrometre.Core.Modules;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.JeunesPrestataires.Catalog;
using Spectrometre.Modules.JeunesPrestataires.Services;
using Spectrometre.Modules.Missions.Catalog;
using Spectrometre.Modules.Missions.Entities;
using Spectrometre.Modules.Missions.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

[Collection("Base de données partagée")]
public sealed class BadgeServiceTests(ServiceFixture fixture)
{
    [Fact]
    public void Catalog_ContientLesBadgesDemandes()
    {
        Assert.Equal(14, BadgeCatalog.All.Count);
        Assert.Equal(BadgeCatalog.All.Count, BadgeCatalog.All.Select(b => b.Key).Distinct().Count());
        Assert.Equal(4, BadgeCatalog.GrilleScoreSeuil);
        Assert.Equal([1, 5, 10], BadgeCatalog.PaliersEvaluation);
        Assert.Contains(BadgeCatalog.All, b => b.Key == "charte" && b.Criterion == BadgeCriterion.CharteAcceptee);
        Assert.Contains(GrilleObservationCatalog.AllCriteres, c => c.Key == "autonomie");
        Assert.Contains(GrilleObservationCatalog.AllCriteres, c => c.Key == "communication");
        Assert.Contains(GrilleObservationCatalog.AllCriteres, c => c.Key == "initiative");
        Assert.Contains(GrilleObservationCatalog.AllCriteres, c => c.Key == "soin_travail");
    }

    [Fact]
    public async Task GetBadges_UtilisateurNonJeune_RetourneNull()
    {
        var badges = fixture.Services.GetRequiredService<IBadgeService>();
        var userId = await CreerUtilisateurAsync($"pas-jeune-{Guid.NewGuid()}@test.local");
        Assert.Null(await badges.GetBadgesAsync(userId));
    }

    [Fact]
    public async Task Charte_PasseDeVerrouilleADebloque()
    {
        var badges = fixture.Services.GetRequiredService<IBadgeService>();
        var charte = fixture.Services.GetRequiredService<ICharteService>();
        var jeune = await CreerJeuneAvecCoachAsync(accepterCharte: false);

        var avant = (await badges.GetBadgesAsync(jeune.UserId))!.Single(b => b.Key == "charte");
        Assert.False(avant.Debloque);
        Assert.Equal(0, avant.Actuel);

        Assert.True(await charte.AccepterAsync(jeune.UserId, "Léa Dupont"));

        var apres = (await badges.GetBadgesAsync(jeune.UserId))!.Single(b => b.Key == "charte");
        Assert.True(apres.Debloque);
        Assert.NotNull(apres.DebloqueLe);
        Assert.Equal(1, apres.Actuel);
    }

    [Fact]
    public async Task MissionsEtEvaluations_Paliers_VerrouillePuisDebloque()
    {
        var badges = fixture.Services.GetRequiredService<IBadgeService>();
        var jeune = await CreerJeuneAvecCoachAsync(accepterCharte: true);
        var particulierUserId = await CreerParticulierAsync();

        BadgeView B(string key, IReadOnlyList<BadgeView> list) => list.Single(x => x.Key == key);

        var zero = (await badges.GetBadgesAsync(jeune.UserId))!;
        Assert.False(B("premiere_mission", zero).Debloque);
        Assert.False(B("ponctuel_1", zero).Debloque);
        Assert.False(B("mission_reussie_1", zero).Debloque);
        Assert.False(B("toujours_partant", zero).Debloque);
        Assert.False(B("respectueux", zero).Debloque);

        await CompleterMissionEvalueeAsync(jeune, particulierUserId);

        var un = (await badges.GetBadgesAsync(jeune.UserId))!;
        Assert.True(B("premiere_mission", un).Debloque);
        Assert.True(B("ponctuel_1", un).Debloque);
        Assert.True(B("mission_reussie_1", un).Debloque);
        Assert.False(B("ponctuel_5", un).Debloque);
        Assert.Equal(1, B("ponctuel_5", un).Actuel);
        Assert.False(B("toujours_partant", un).Debloque);
        Assert.False(B("respectueux", un).Debloque);

        await CompleterMissionEvalueeAsync(jeune, particulierUserId);
        await CompleterMissionEvalueeAsync(jeune, particulierUserId);

        var trois = (await badges.GetBadgesAsync(jeune.UserId))!;
        Assert.True(B("toujours_partant", trois).Debloque);
        Assert.True(B("respectueux", trois).Debloque);
        Assert.False(B("ponctuel_5", trois).Debloque);

        await CompleterMissionEvalueeAsync(jeune, particulierUserId);
        await CompleterMissionEvalueeAsync(jeune, particulierUserId);

        var cinq = (await badges.GetBadgesAsync(jeune.UserId))!;
        Assert.True(B("ponctuel_5", cinq).Debloque);
        Assert.True(B("mission_reussie_5", cinq).Debloque);
        Assert.False(B("ponctuel_10", cinq).Debloque);

        for (var i = 0; i < 5; i++)
            await CompleterMissionEvalueeAsync(jeune, particulierUserId);

        var dix = (await badges.GetBadgesAsync(jeune.UserId))!;
        Assert.True(B("ponctuel_10", dix).Debloque);
        Assert.True(B("mission_reussie_10", dix).Debloque);
        Assert.Equal(10, B("ponctuel_10", dix).Actuel);
    }

    [Fact]
    public async Task Grille_Score3Verrouille_Score4Debloque()
    {
        var badges = fixture.Services.GetRequiredService<IBadgeService>();
        var grille = fixture.Services.GetRequiredService<IGrilleObservationService>();
        var jeune = await CreerJeuneAvecCoachAsync(accepterCharte: true);
        var profile = await fixture.Services.GetRequiredService<IJeuneProfileService>()
            .TryGetByUserIdAsync(jeune.UserId);
        Assert.NotNull(profile);

        var idFaible = await grille.CreerEvaluationAsync(
            jeune.CoachUserId,
            profile!.Id,
            [
                new GrilleObservationCritereInput("autonomie", 3, null),
                new GrilleObservationCritereInput("communication", 3, null),
                new GrilleObservationCritereInput("initiative", 3, null),
                new GrilleObservationCritereInput("soin_travail", 3, null),
            ],
            null);
        Assert.NotNull(idFaible);

        var avant = (await badges.GetBadgesAsync(jeune.UserId))!;
        Assert.False(avant.Single(b => b.Key == "autonome").Debloque);
        Assert.False(avant.Single(b => b.Key == "bon_communicant").Debloque);
        Assert.False(avant.Single(b => b.Key == "esprit_initiative").Debloque);
        Assert.False(avant.Single(b => b.Key == "soin_travail").Debloque);
        Assert.Equal(3, avant.Single(b => b.Key == "autonome").Actuel);

        var idFort = await grille.CreerEvaluationAsync(
            jeune.CoachUserId,
            profile.Id,
            [
                new GrilleObservationCritereInput("autonomie", 4, null),
                new GrilleObservationCritereInput("communication", 5, null),
                new GrilleObservationCritereInput("initiative", 4, null),
                new GrilleObservationCritereInput("soin_travail", 4, null),
            ],
            null);
        Assert.NotNull(idFort);

        var apres = (await badges.GetBadgesAsync(jeune.UserId))!;
        Assert.True(apres.Single(b => b.Key == "autonome").Debloque);
        Assert.True(apres.Single(b => b.Key == "bon_communicant").Debloque);
        Assert.True(apres.Single(b => b.Key == "esprit_initiative").Debloque);
        Assert.True(apres.Single(b => b.Key == "soin_travail").Debloque);
        Assert.Equal(5, apres.Single(b => b.Key == "bon_communicant").Actuel);
        Assert.NotNull(apres.Single(b => b.Key == "autonome").DebloqueLe);
    }

    private sealed record JeuneContext(string UserId, string CoachUserId);

    private async Task CompleterMissionEvalueeAsync(JeuneContext jeune, string particulierUserId)
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var evaluationService = fixture.Services.GetRequiredService<IMissionEvaluationParticulierService>();
        var missionId = await missionService.PublierMissionAsync(
            particulierUserId,
            new PublierMissionInput(
                "Badge", "Desc", null, null, MissionDifficulte.Facile, 10m, null,
                MissionCategorie.Rangement, MissionNiveauEncadrement.PresentDebutSeulement));
        Assert.NotNull(missionId);
        Assert.True(await missionService.AccepterMissionAsync(jeune.UserId, missionId.Value));
        var acceptationId = (await missionService.GetDemandesEnAttentePourJeuneSuiviAsync(jeune.CoachUserId, jeune.UserId))
            .Single(a => a.MissionId == missionId.Value).AcceptationId;
        Assert.True(await missionService.ValiderAcceptationAsync(jeune.CoachUserId, acceptationId));
        Assert.True(await missionService.MarquerTermineeAsync(jeune.UserId, acceptationId));
        Assert.NotNull(await evaluationService.GetOrCreateAsync(particulierUserId, acceptationId));
        Assert.True(await evaluationService.SaveAsync(
            particulierUserId, acceptationId,
            ponctualite: true,
            consignesComprises: true,
            tacheRealiseeCorrectement: true,
            attitudeRespectueuse: true,
            pointsPositifs: null,
            pointsAAmeliorer: null,
            accepteraitNouvelleMission: true));
    }

    private async Task<JeuneContext> CreerJeuneAvecCoachAsync(bool accepterCharte)
    {
        var coachUserId = await CreerUtilisateurAsync($"coach-badge-{Guid.NewGuid()}@test.local");
        var jeuneEmail = $"jeune-badge-{Guid.NewGuid()}@test.local";
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();
        var coachingService = fixture.Services.GetRequiredService<ICoachingService>();

        var invite = await jeuneService.InviterJeuneAsync(
            coachUserId,
            jeuneEmail,
            "Badge",
            "Léa",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)),
            "http://localhost");
        Assert.True(invite.Success);

        var jeuneUserId = await CreerUtilisateurAsync(jeuneEmail);
        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();
        var invitation = await coreDb.Invitations.FirstAsync(i => i.Id == invite.Invitation!.Id);

        await jeuneService.FinaliserDepuisInvitationAsync(invitation, jeuneUserId);
        await coachingService.FinaliserJeunePrestataireDepuisInvitationAsync(invitation, jeuneUserId);
        if (accepterCharte)
            await fixture.GarantirCharteAccepteeAsync(jeuneUserId);
        await fixture.Services.GetRequiredService<IInvitationService>().MarquerAccepteeAsync(invitation.Id, coreDb);

        return new JeuneContext(jeuneUserId, coachUserId);
    }

    private async Task<string> CreerParticulierAsync()
    {
        var userId = await CreerUtilisateurAsync($"part-badge-{Guid.NewGuid()}@test.local");
        var particulierService = fixture.Services.GetRequiredService<IParticulierProfileService>();
        var moduleRegistry = fixture.Services.GetRequiredService<IModuleRegistry>();
        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();

        var profileId = await particulierService.GetOrCreateProfileIdAsync(userId, "Part", "Badge");
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
