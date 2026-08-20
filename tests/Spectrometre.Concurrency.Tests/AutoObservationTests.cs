using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Invitations;
using Spectrometre.Core.Notifications;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.JeunesPrestataires.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

[Collection("Base de données partagée")]
public sealed class AutoObservationTests(ServiceFixture fixture)
{
    [Fact]
    public async Task Jeune_SauvegardeSection_EtCoachPeutLire()
    {
        var (coachId, jeuneId, profileId) = await CreerJeuneAvecCoachAsync();

        var svc = fixture.Services.GetRequiredService<IAutoObservationService>();

        var saved = await svc.SaveSectionAsync(
            jeuneId,
            profileId,
            "p1.s1",
            [new AutoObservationAnswerInput("p1.s1.q1", "J'aime le jardinage", null)]);

        Assert.True(saved);

        var sectionCoach = await svc.TryGetSectionAsync(coachId, profileId, "p1.s1");
        Assert.NotNull(sectionCoach);
        Assert.Equal(AutoObservationAccessMode.Coach, sectionCoach!.AccessMode);
        Assert.Equal("J'aime le jardinage", sectionCoach.Answers.First(a => a.QuestionKey == "p1.s1.q1").TextValue);
    }

    [Fact]
    public async Task AutreCoach_NAccedePasAuQuestionnaire()
    {
        var (_, jeuneId, profileId) = await CreerJeuneAvecCoachAsync();
        var autreCoach = await CreerUtilisateurAsync($"autre-coach-{Guid.NewGuid()}@test.local");

        var svc = fixture.Services.GetRequiredService<IAutoObservationService>();
        var page = await svc.TryGetPageAsync(autreCoach, profileId);

        Assert.Null(page);
    }

    [Fact]
    public async Task DemanderAide_NotifieLeCoachReferent()
    {
        var (coachId, jeuneId, profileId) = await CreerJeuneAvecCoachAsync();
        var svc = fixture.Services.GetRequiredService<IAutoObservationService>();

        var ok = await svc.DemanderAideAsync(jeuneId, profileId, "p1.s2");
        Assert.True(ok);

        using var scope = fixture.Services.CreateScope();
        var notifSvc = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var notifs = await notifSvc.GetRecentesAsync(coachId, 10);
        Assert.Contains(notifs, n => n.TypeCode == "JeunesPrestataires.BesoinAide");
    }

    [Fact]
    public async Task RegenererSynthese_ProduitUnTexte()
    {
        var (coachId, jeuneId, profileId) = await CreerJeuneAvecCoachAsync();
        var svc = fixture.Services.GetRequiredService<IAutoObservationService>();

        await svc.SaveSectionAsync(
            jeuneId,
            profileId,
            "p2.s3",
            [
                new AutoObservationAnswerInput("p2.s3.qualites", "Calme|Fiable", null),
                new AutoObservationAnswerInput("p2.s3.progresser", null, 5),
            ]);

        var synthese = await svc.RegenererSyntheseAsync(jeuneId, profileId);
        Assert.NotNull(synthese);
        Assert.Contains("Forces perçues", synthese, StringComparison.Ordinal);
        Assert.Contains("Calme", synthese, StringComparison.Ordinal);

        var syntheseCoach = await svc.RegenererSyntheseAsync(coachId, profileId);
        Assert.NotNull(syntheseCoach);
    }

    private async Task<(string CoachId, string JeuneId, int ProfileId)> CreerJeuneAvecCoachAsync()
    {
        var coachId = await CreerUtilisateurAsync($"coach-ao-{Guid.NewGuid()}@test.local");
        var jeuneEmail = $"jeune-ao-{Guid.NewGuid()}@test.local";
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();
        var coachingService = fixture.Services.GetRequiredService<ICoachingService>();

        var invite = await jeuneService.InviterJeuneAsync(
            coachId,
            jeuneEmail,
            "Bernard",
            "Sam",
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
