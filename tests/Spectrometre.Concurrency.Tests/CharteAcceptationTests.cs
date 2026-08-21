using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Invitations;
using Spectrometre.Core.Modules;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.JeunesPrestataires.Catalog;
using Spectrometre.Modules.JeunesPrestataires.Resources;
using Spectrometre.Modules.JeunesPrestataires.Services;
using Spectrometre.Modules.Missions.Data;
using Spectrometre.Modules.Missions.Entities;
using Spectrometre.Modules.Missions.Services;
using Spectrometre.Modules.ProfilCandidat.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

[Collection("Base de données partagée")]
public sealed class CharteAcceptationTests(ServiceFixture fixture)
{
    [Fact]
    public void Catalog_TreizeSections()
    {
        Assert.Equal(13, CharteCatalog.Sections.Count);
        Assert.Equal(
            [
                "objet",
                "principes_generaux",
                "engagement_particulier",
                "ententes_hors",
                "responsabilite_coach",
                "missions_autorisees",
                "missions_interdites",
                "deroulement",
                "comportements",
                "confidentialite",
                "engagement_prestataire",
                "consequences",
                "formule_engagement",
            ],
            CharteCatalog.Sections.Select(s => s.Key).ToArray());
    }

    [Fact]
    public async Task Accepter_UneSeuleFois_RefuseLeDeuxiemeAppel()
    {
        var charte = fixture.Services.GetRequiredService<ICharteService>();
        var jeune = await CreerJeuneSansCharteAsync();

        var avant = await charte.GetAsync(jeune.UserId);
        Assert.NotNull(avant);
        Assert.False(avant.Acceptee);

        Assert.False(await charte.AccepterAsync(jeune.UserId, "   "));
        Assert.True(await charte.AccepterAsync(jeune.UserId, "Léa Dupont"));

        var apres = await charte.GetAsync(jeune.UserId);
        Assert.NotNull(apres);
        Assert.True(apres.Acceptee);
        Assert.Equal("Léa Dupont", apres.NomConfirmation);
        Assert.NotNull(apres.AccepteeLe);

        Assert.False(await charte.AccepterAsync(jeune.UserId, "Autre Nom"));
        var encore = await charte.GetAsync(jeune.UserId);
        Assert.Equal("Léa Dupont", encore!.NomConfirmation);
    }

    [Fact]
    public async Task AccepterMission_RefuseSansCharte_PuisOkApresAcceptation()
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var charte = fixture.Services.GetRequiredService<ICharteService>();
        var jeune = await CreerJeuneSansCharteAsync();
        var particulierUserId = await CreerParticulierAsync();
        var missionId = await missionService.PublierMissionAsync(
            particulierUserId,
            new PublierMissionInput(
                "Mission charte", "Desc", null, null, MissionDifficulte.Facile, 15m, null,
                MissionCategorie.Autre, MissionNiveauEncadrement.PresentPendantMission));
        Assert.NotNull(missionId);

        Assert.False(await missionService.AccepterMissionAsync(jeune.UserId, missionId.Value));
        await fixture.GarantirPublicationValideeAsync(jeune.CoachUserId, missionId.Value);
        Assert.False(await missionService.AccepterMissionAsync(jeune.UserId, missionId.Value));
        Assert.True(await charte.AccepterAsync(jeune.UserId, "Léa Dupont"));
        Assert.True(await missionService.AccepterMissionAsync(jeune.UserId, missionId.Value));

        await using var db = await fixture.Services.GetRequiredService<IDbContextFactory<MissionsDbContext>>().CreateDbContextAsync();
        var mission = await db.Missions.AsNoTracking().FirstAsync(m => m.Id == missionId.Value);
        Assert.Equal(MissionStatut.EnAttenteValidation, mission.Statut);
    }

    [Fact]
    public void GeneratePdf_ProduitUnPdfNonVide()
    {
        var pdf = fixture.Services.GetRequiredService<IChartePdfService>();
        var localizer = fixture.Services.GetRequiredService<IStringLocalizer<JeunesPrestatairesResource>>();
        var bytes = pdf.GeneratePdf(localizer);
        Assert.NotEmpty(bytes);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }

    private sealed record JeuneContext(string UserId, string CoachUserId);

    private async Task<JeuneContext> CreerJeuneSansCharteAsync()
    {
        var coachUserId = await CreerUtilisateurAsync($"coach-charte-{Guid.NewGuid()}@test.local");
        var jeuneEmail = $"jeune-charte-{Guid.NewGuid()}@test.local";
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
        await fixture.Services.GetRequiredService<IInvitationService>().MarquerAccepteeAsync(invitation.Id, coreDb);
        return new JeuneContext(jeuneUserId, coachUserId);
    }

    private async Task<string> CreerParticulierAsync()
    {
        var userId = await CreerUtilisateurAsync($"part-charte-{Guid.NewGuid()}@test.local");
        var particulierService = fixture.Services.GetRequiredService<IParticulierProfileService>();
        var moduleRegistry = fixture.Services.GetRequiredService<IModuleRegistry>();
        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();

        var profileId = await particulierService.GetOrCreateProfileIdAsync(userId, "Part", "Charte");
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
