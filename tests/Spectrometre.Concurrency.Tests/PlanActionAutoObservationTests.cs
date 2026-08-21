using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.JeunesPrestataires.Data;
using Spectrometre.Modules.JeunesPrestataires.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

[Collection("Base de données partagée")]
public sealed class PlanActionAutoObservationTests(ServiceFixture fixture)
{
    [Fact]
    public async Task Coach_CreeEtModifieLePlan_JeuneLitSansPouvoirEcrire()
    {
        var (coachId, jeuneId, profileId) = await CreerJeuneAvecCoachAsync();
        var planSvc = fixture.Services.GetRequiredService<IPlanActionAutoObservationService>();
        var echeance = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(21));

        Assert.Null(await planSvc.GetLectureAsync(jeuneId, profileId));

        var vide = await planSvc.GetOrCreateAsync(coachId, profileId);
        Assert.NotNull(vide);
        Assert.False(vide!.EstRempli);

        Assert.True(await planSvc.SaveAsync(coachId, profileId, new PlanActionAutoObservationInput(
            "Tenir 3 missions simples",
            "Accepter une mission jardinage",
            "coach + particulier",
            echeance,
            "Une mission terminée sans incident")));

        Assert.True(await planSvc.SaveAsync(coachId, profileId, new PlanActionAutoObservationInput(
            "Tenir 3 missions simples (revu)",
            "Accepter une mission jardinage",
            "coach + particulier",
            echeance,
            "Une mission terminée sans incident")));

        await using var db = await fixture.Services.GetRequiredService<IDbContextFactory<JeunesPrestatairesDbContext>>()
            .CreateDbContextAsync();
        Assert.Single(await db.PlansActionAutoObservation.Where(p => p.JeuneProfileId == profileId).ToListAsync());

        var lectureJeune = await planSvc.GetLectureAsync(jeuneId, profileId);
        Assert.NotNull(lectureJeune);
        Assert.Equal("Tenir 3 missions simples (revu)", lectureJeune!.ObjectifPrincipal);

        Assert.False(await planSvc.SaveAsync(jeuneId, profileId, new PlanActionAutoObservationInput(
            "Hack", null, null, null, null)));
        Assert.Equal("Tenir 3 missions simples (revu)",
            (await planSvc.GetLectureAsync(jeuneId, profileId))!.ObjectifPrincipal);
        Assert.Null(await planSvc.GetOrCreateAsync(jeuneId, profileId));
    }

    [Fact]
    public async Task CoachNonAutorise_NePeutNiEditerNiValider()
    {
        var (coachId, jeuneId, profileId) = await CreerJeuneAvecCoachAsync();
        var autreCoach = await CreerUtilisateurAsync($"autre-pa-{Guid.NewGuid()}@test.local");
        var planSvc = fixture.Services.GetRequiredService<IPlanActionAutoObservationService>();
        var aoSvc = fixture.Services.GetRequiredService<IAutoObservationService>();

        await aoSvc.RegenererSyntheseAsync(coachId, profileId);

        Assert.Null(await planSvc.GetOrCreateAsync(autreCoach, profileId));
        Assert.False(await planSvc.SaveAsync(autreCoach, profileId, new PlanActionAutoObservationInput(
            "Hack", null, null, null, null)));
        Assert.False(await aoSvc.ValiderSyntheseAsync(autreCoach, profileId));
        Assert.False(await aoSvc.ValiderSyntheseAsync(jeuneId, profileId));
        Assert.Null((await aoSvc.TryGetPageAsync(coachId, profileId))!.SyntheseValideeLe);
    }

    [Fact]
    public async Task Coach_ValideLaSynthese_RegenerationEffaceLaValidation()
    {
        var (coachId, _, profileId) = await CreerJeuneAvecCoachAsync();
        var aoSvc = fixture.Services.GetRequiredService<IAutoObservationService>();

        Assert.False(await aoSvc.ValiderSyntheseAsync(coachId, profileId));
        Assert.NotNull(await aoSvc.RegenererSyntheseAsync(coachId, profileId));
        Assert.True(await aoSvc.ValiderSyntheseAsync(coachId, profileId));

        var page = await aoSvc.TryGetPageAsync(coachId, profileId);
        Assert.NotNull(page!.SyntheseValideeLe);

        Assert.NotNull(await aoSvc.RegenererSyntheseAsync(coachId, profileId));
        page = await aoSvc.TryGetPageAsync(coachId, profileId);
        Assert.Null(page!.SyntheseValideeLe);
    }

    private async Task<(string CoachId, string JeuneId, int ProfileId)> CreerJeuneAvecCoachAsync()
    {
        var coachId = await CreerUtilisateurAsync($"coach-pa-{Guid.NewGuid()}@test.local");
        var jeuneEmail = $"jeune-pa-{Guid.NewGuid()}@test.local";
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();
        var coachingService = fixture.Services.GetRequiredService<ICoachingService>();

        var invite = await jeuneService.InviterJeuneAsync(
            coachId,
            jeuneEmail,
            "Martin",
            "Léa",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-15)),
            "http://localhost");
        Assert.True(invite.Success);

        var jeuneId = await CreerUtilisateurAsync(jeuneEmail);
        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();
        var invitation = await coreDb.Invitations.FirstAsync(i => i.Id == invite.Invitation!.Id);

        var profile = await jeuneService.FinaliserDepuisInvitationAsync(invitation, jeuneId);
        await coachingService.FinaliserJeunePrestataireDepuisInvitationAsync(invitation, jeuneId);

        return (coachId, jeuneId, profile.Id);
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
