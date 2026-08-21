using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Invitations;
using Spectrometre.Core.Modules;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.JeunesPrestataires.Entities;
using Spectrometre.Modules.JeunesPrestataires.Services;
using Spectrometre.Modules.Missions.Catalog;
using Spectrometre.Modules.Missions.Entities;
using Spectrometre.Modules.Missions.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

[Collection("Base de données partagée")]
public sealed class MesAstucesRecommandationServiceTests(ServiceFixture fixture)
{
    [Fact]
    public async Task SansHistorique_StarterSansExperience()
    {
        var svc = fixture.Services.GetRequiredService<IMesAstucesRecommandationService>();
        var jeune = await CreerJeuneAvecCoachAsync(ProfilAccompagnement.SansExperience);
        var recos = await svc.GetRecommandeesAsync(jeune.UserId);
        Assert.NotNull(recos);
        Assert.Equal(
            MesAstucesRecommandationsCatalog.StarterSansExperience,
            recos.Select(f => f.Key).ToArray());
    }

    [Fact]
    public async Task EvaluationPonctualiteFalse_RecommandeFichesHoraires()
    {
        var svc = fixture.Services.GetRequiredService<IMesAstucesRecommandationService>();
        var jeune = await CreerJeuneAvecCoachAsync(ProfilAccompagnement.SansExperience);
        var particulierUserId = await CreerParticulierAsync();
        await CompleterMissionEvalueeAsync(jeune, particulierUserId, ponctualite: false);

        var recos = await svc.GetRecommandeesAsync(jeune.UserId);
        Assert.NotNull(recos);
        Assert.Contains(recos, f => f.Key == "arriver_a_lheure");
        Assert.Contains(recos, f => f.Key == "en_retard");
    }

    [Fact]
    public async Task BonsScores_ListeVide()
    {
        var svc = fixture.Services.GetRequiredService<IMesAstucesRecommandationService>();
        var jeune = await CreerJeuneAvecCoachAsync(ProfilAccompagnement.SansExperience);
        var particulierUserId = await CreerParticulierAsync();
        await CompleterMissionEvalueeAsync(jeune, particulierUserId, ponctualite: true);

        var recos = await svc.GetRecommandeesAsync(jeune.UserId);
        Assert.NotNull(recos);
        Assert.Empty(recos);
    }

    private sealed record JeuneContext(string UserId, string CoachUserId);

    private async Task CompleterMissionEvalueeAsync(JeuneContext jeune, string particulierUserId, bool ponctualite)
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var evaluationService = fixture.Services.GetRequiredService<IMissionEvaluationParticulierService>();
        var missionId = await missionService.PublierMissionAsync(
            particulierUserId,
            new PublierMissionInput(
                "Astuce", "Desc", null, null, MissionDifficulte.Facile, 10m, null,
                MissionCategorie.Rangement, MissionNiveauEncadrement.PresentDebutSeulement));
        Assert.NotNull(missionId);
        await fixture.GarantirPublicationValideeAsync(jeune.CoachUserId, missionId.Value);
        Assert.True(await missionService.AccepterMissionAsync(jeune.UserId, missionId.Value));
        var acceptationId = (await missionService.GetDemandesEnAttentePourJeuneSuiviAsync(jeune.CoachUserId, jeune.UserId))
            .Single(a => a.MissionId == missionId.Value).AcceptationId;
        Assert.True(await missionService.ValiderAcceptationAsync(jeune.CoachUserId, acceptationId));
        Assert.True(await missionService.MarquerTermineeAsync(jeune.UserId, acceptationId));
        Assert.NotNull(await evaluationService.GetOrCreateAsync(particulierUserId, acceptationId));
        Assert.True(await evaluationService.SaveAsync(
            particulierUserId, acceptationId,
            ponctualite,
            consignesComprises: true,
            tacheRealiseeCorrectement: true,
            attitudeRespectueuse: true,
            pointsPositifs: null,
            pointsAAmeliorer: null,
            accepteraitNouvelleMission: true));
    }

    private async Task<JeuneContext> CreerJeuneAvecCoachAsync(ProfilAccompagnement profil)
    {
        var coachUserId = await CreerUtilisateurAsync($"coach-astuce-{Guid.NewGuid()}@test.local");
        var jeuneEmail = $"jeune-astuce-{Guid.NewGuid()}@test.local";
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();
        var coachingService = fixture.Services.GetRequiredService<ICoachingService>();

        var invite = await jeuneService.InviterJeuneAsync(
            coachUserId,
            jeuneEmail,
            "Astuce",
            "Léa",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)),
            "http://localhost",
            profil);
        Assert.True(invite.Success);

        var jeuneUserId = await CreerUtilisateurAsync(jeuneEmail);
        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();
        var invitation = await coreDb.Invitations.FirstAsync(i => i.Id == invite.Invitation!.Id);

        await jeuneService.FinaliserDepuisInvitationAsync(invitation, jeuneUserId);
        await coachingService.FinaliserJeunePrestataireDepuisInvitationAsync(invitation, jeuneUserId);
        await fixture.GarantirCharteAccepteeAsync(jeuneUserId);
        await fixture.Services.GetRequiredService<IInvitationService>().MarquerAccepteeAsync(invitation.Id, coreDb);

        return new JeuneContext(jeuneUserId, coachUserId);
    }

    private async Task<string> CreerParticulierAsync()
    {
        var userId = await CreerUtilisateurAsync($"part-astuce-{Guid.NewGuid()}@test.local");
        var particulierService = fixture.Services.GetRequiredService<IParticulierProfileService>();
        var moduleRegistry = fixture.Services.GetRequiredService<IModuleRegistry>();
        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();

        var profileId = await particulierService.GetOrCreateProfileIdAsync(userId, "Part", "Astuce");
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
