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
using Spectrometre.Modules.ProfilCandidat.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

public sealed class MissionPreferenceCategorieMapTests
{
    [Fact]
    public void ToutesLesOptionsQuestionnaire_OntUneCategorie()
    {
        string[] options =
        [
            "Jardinage", "Rangement", "Nettoyage", "Aide aux courses", "Montage simple",
            "Peinture simple", "Déménagement léger", "Lavage de voiture", "Autre",
        ];
        Assert.Equal(options.Length, MissionPreferenceCategorieMap.ParLibelle.Count);
        foreach (var o in options)
            Assert.True(MissionPreferenceCategorieMap.ParLibelle.ContainsKey(o), o);
    }

    [Fact]
    public void TextValue_Pipe_IgnoreInconnus()
    {
        var cats = MissionPreferenceCategorieMap.CategoriesDepuisTextValue("Jardinage|Inconnu|Rangement");
        Assert.Equal(
            new HashSet<MissionCategorie> { MissionCategorie.JardinageSimple, MissionCategorie.Rangement },
            cats);
        Assert.Empty(MissionPreferenceCategorieMap.CategoriesDepuisTextValue(null));
        Assert.Empty(MissionPreferenceCategorieMap.CategoriesDepuisTextValue("   "));
    }
}

[Collection("Base de données partagée")]
public sealed class MissionSuggestionsServiceTests(ServiceFixture fixture)
{
    [Fact]
    public async Task PreferenceDeclaree_MissionsCategorieEnTete()
    {
        var suggestions = fixture.Services.GetRequiredService<IMissionSuggestionsService>();
        var autoObs = fixture.Services.GetRequiredService<IAutoObservationService>();
        var missionService = fixture.Services.GetRequiredService<IMissionService>();

        var jeune = await CreerJeuneAvecCoachAsync(ProfilAccompagnement.SansExperience);
        var particulierUserId = await CreerParticulierAsync();

        Assert.True(await autoObs.SaveSectionAsync(
            jeune.UserId,
            jeune.ProfileId,
            "p2.s12",
            [new AutoObservationAnswerInput("p2.s12.missions_priorite", "Jardinage", null)]));

        var jardinTitre = $"jardin-{Guid.NewGuid():N}";
        var lavageTitre = $"lavage-{Guid.NewGuid():N}";
        var jardinId = await PublierDisponibleAsync(particulierUserId, jeune.CoachUserId, jardinTitre, MissionCategorie.JardinageSimple);
        var lavageId = await PublierDisponibleAsync(particulierUserId, jeune.CoachUserId, lavageTitre, MissionCategorie.LavageDeVoiture);

        var recos = await suggestions.GetRecommandeesAsync(jeune.UserId);
        Assert.NotNull(recos);
        Assert.Contains(recos, m => m.MissionId == jardinId);
        Assert.DoesNotContain(recos, m => m.MissionId == lavageId);

        var toutes = await missionService.GetMissionsDisponiblesAsync();
        Assert.Contains(toutes, m => m.MissionId == lavageId);
    }

    [Fact]
    public async Task SansReponse_AucuneSuggestion()
    {
        var suggestions = fixture.Services.GetRequiredService<IMissionSuggestionsService>();
        var jeune = await CreerJeuneAvecCoachAsync(ProfilAccompagnement.SansExperience);
        var particulierUserId = await CreerParticulierAsync();
        await PublierDisponibleAsync(
            particulierUserId, jeune.CoachUserId, $"jardin-{Guid.NewGuid():N}", MissionCategorie.JardinageSimple);

        var recos = await suggestions.GetRecommandeesAsync(jeune.UserId);
        Assert.NotNull(recos);
        Assert.Empty(recos);
    }

    [Fact]
    public async Task Autonome_AucuneSuggestionMemeAvecPreference()
    {
        var suggestions = fixture.Services.GetRequiredService<IMissionSuggestionsService>();
        var autoObs = fixture.Services.GetRequiredService<IAutoObservationService>();
        var jeune = await CreerJeuneAvecCoachAsync(ProfilAccompagnement.Autonome);
        var particulierUserId = await CreerParticulierAsync();

        Assert.True(await autoObs.SaveSectionAsync(
            jeune.UserId,
            jeune.ProfileId,
            "p2.s12",
            [new AutoObservationAnswerInput("p2.s12.missions_priorite", "Jardinage", null)]));
        var jardinId = await PublierDisponibleAsync(
            particulierUserId, jeune.CoachUserId, $"jardin-{Guid.NewGuid():N}", MissionCategorie.JardinageSimple);

        var recos = await suggestions.GetRecommandeesAsync(jeune.UserId);
        Assert.NotNull(recos);
        Assert.Empty(recos);

        var toutes = await fixture.Services.GetRequiredService<IMissionService>().GetMissionsDisponiblesAsync();
        Assert.Contains(toutes, m => m.MissionId == jardinId);
    }

    private sealed record JeuneContext(string UserId, string CoachUserId, int ProfileId);

    private async Task<int> PublierDisponibleAsync(
        string particulierUserId, string coachUserId, string titre, MissionCategorie categorie)
    {
        var missionService = fixture.Services.GetRequiredService<IMissionService>();
        var missionId = await missionService.PublierMissionAsync(
            particulierUserId,
            new PublierMissionInput(
                titre, "Desc", null, null, MissionDifficulte.Facile, 10m, null,
                categorie, MissionNiveauEncadrement.PresentDebutSeulement));
        Assert.NotNull(missionId);
        await fixture.GarantirPublicationValideeAsync(coachUserId, missionId.Value);
        return missionId.Value;
    }

    private async Task<JeuneContext> CreerJeuneAvecCoachAsync(ProfilAccompagnement profil)
    {
        var coachUserId = await CreerUtilisateurAsync($"coach-sugg-{Guid.NewGuid()}@test.local");
        var jeuneEmail = $"jeune-sugg-{Guid.NewGuid()}@test.local";
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();
        var coachingService = fixture.Services.GetRequiredService<ICoachingService>();

        var invite = await jeuneService.InviterJeuneAsync(
            coachUserId,
            jeuneEmail,
            "Sugg",
            "Léa",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-16)),
            "http://localhost",
            profil);
        Assert.True(invite.Success);

        var jeuneUserId = await CreerUtilisateurAsync(jeuneEmail);
        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();
        var invitation = await coreDb.Invitations.FirstAsync(i => i.Id == invite.Invitation!.Id);

        var profile = await jeuneService.FinaliserDepuisInvitationAsync(invitation, jeuneUserId);
        await coachingService.FinaliserJeunePrestataireDepuisInvitationAsync(invitation, jeuneUserId);
        await fixture.GarantirCharteAccepteeAsync(jeuneUserId);
        await ActiverGestionDuTempsApresAcceptationJeuneAsync(jeuneUserId, coreDb);
        await fixture.Services.GetRequiredService<IInvitationService>().MarquerAccepteeAsync(invitation.Id, coreDb);

        return new JeuneContext(jeuneUserId, coachUserId, profile.Id);
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

    private async Task<string> CreerParticulierAsync()
    {
        var userId = await CreerUtilisateurAsync($"part-sugg-{Guid.NewGuid()}@test.local");
        var particulierService = fixture.Services.GetRequiredService<IParticulierProfileService>();
        var moduleRegistry = fixture.Services.GetRequiredService<IModuleRegistry>();
        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();

        var profileId = await particulierService.GetOrCreateProfileIdAsync(userId, "Part", "Sugg");
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
