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
public sealed class GrilleObservationTests(ServiceFixture fixture)
{
    [Fact]
    public async Task Coach_CreeEvaluation_EtJeuneLitSansCommentaireGeneral()
    {
        var (coachId, jeuneId, profileId) = await CreerJeuneAvecCoachAsync();
        var svc = fixture.Services.GetRequiredService<IGrilleObservationService>();

        var id = await svc.CreerEvaluationAsync(
            coachId,
            profileId,
            [
                new GrilleObservationCritereInput("ponctualite", 4, "Très ponctuel"),
                new GrilleObservationCritereInput("autonomie", 5, null),
            ],
            "Note confidentielle coach");

        Assert.NotNull(id);

        var detailCoach = await svc.TryGetEvaluationAsync(coachId, id!.Value);
        Assert.NotNull(detailCoach);
        Assert.Equal(GrilleObservationAccessMode.Coach, detailCoach!.AccessMode);
        Assert.Equal("Note confidentielle coach", detailCoach.CommentaireGeneral);

        var detailJeune = await svc.TryGetEvaluationAsync(jeuneId, id.Value);
        Assert.NotNull(detailJeune);
        Assert.Equal(GrilleObservationAccessMode.Jeune, detailJeune!.AccessMode);
        Assert.Null(detailJeune.CommentaireGeneral);
        Assert.Equal("Très ponctuel", detailJeune.Criteres.First(c => c.CritereKey == "ponctualite").Commentaire);
    }

    [Fact]
    public async Task CreerEvaluation_NotifieLeJeuneAChaqueFois_SansLeContenu()
    {
        var (coachId, jeuneId, profileId) = await CreerJeuneAvecCoachAsync();
        var svc = fixture.Services.GetRequiredService<IGrilleObservationService>();
        var notifSvc = fixture.Services.GetRequiredService<INotificationService>();
        var autreCoach = await CreerUtilisateurAsync($"autre-coach-go-n-{Guid.NewGuid()}@test.local");

        Assert.Null(await svc.CreerEvaluationAsync(
            autreCoach,
            profileId,
            [new GrilleObservationCritereInput("ponctualite", 3, null)],
            "Secret"));
        Assert.DoesNotContain(
            await notifSvc.GetRecentesAsync(jeuneId, 20),
            n => n.TypeCode == "JeunesPrestataires.GrilleObservationAjoutee");

        Assert.NotNull(await svc.CreerEvaluationAsync(
            coachId,
            profileId,
            [new GrilleObservationCritereInput("autonomie", 5, "Excellent")],
            "Note confidentielle"));
        Assert.NotNull(await svc.CreerEvaluationAsync(
            coachId,
            profileId,
            [new GrilleObservationCritereInput("ponctualite", 4, null)],
            "Deuxième note"));

        var notifs = (await notifSvc.GetRecentesAsync(jeuneId, 20))
            .Where(n => n.TypeCode == "JeunesPrestataires.GrilleObservationAjoutee")
            .ToList();
        Assert.Equal(2, notifs.Count);
        Assert.All(notifs, n =>
        {
            Assert.Equal("/jeune/mes-progres", n.Lien);
            Assert.DoesNotContain("Excellent", n.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("confidentielle", n.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Deuxième note", n.Message, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task AutreCoach_NAccedePasALaGrille()
    {
        var (_, _, profileId) = await CreerJeuneAvecCoachAsync();
        var autreCoach = await CreerUtilisateurAsync($"autre-coach-go-{Guid.NewGuid()}@test.local");
        var svc = fixture.Services.GetRequiredService<IGrilleObservationService>();

        var page = await svc.TryGetPageAsync(autreCoach, profileId);
        Assert.Null(page);

        var id = await svc.CreerEvaluationAsync(
            autreCoach,
            profileId,
            [new GrilleObservationCritereInput("ponctualite", 3, null)],
            "Ne doit pas être créé");
        Assert.Null(id);
    }

    [Fact]
    public async Task DeuxEvaluationsSuccessives_ConserveHistorique()
    {
        var (coachId, jeuneId, profileId) = await CreerJeuneAvecCoachAsync();
        var svc = fixture.Services.GetRequiredService<IGrilleObservationService>();

        var id1 = await svc.CreerEvaluationAsync(
            coachId,
            profileId,
            [new GrilleObservationCritereInput("ponctualite", 2, null)],
            "Première");
        var id2 = await svc.CreerEvaluationAsync(
            coachId,
            profileId,
            [new GrilleObservationCritereInput("ponctualite", 4, null)],
            "Deuxième");

        Assert.NotNull(id1);
        Assert.NotNull(id2);
        Assert.NotEqual(id1, id2);

        var historique = await svc.GetHistoriqueAsync(jeuneId, profileId);
        Assert.Equal(2, historique.Count);

        var detail1 = await svc.TryGetEvaluationAsync(coachId, id1!.Value);
        var detail2 = await svc.TryGetEvaluationAsync(coachId, id2!.Value);
        Assert.Equal(2, detail1!.Criteres.First(c => c.CritereKey == "ponctualite").Score);
        Assert.Equal(4, detail2!.Criteres.First(c => c.CritereKey == "ponctualite").Score);
    }

    private async Task<(string CoachId, string JeuneId, int ProfileId)> CreerJeuneAvecCoachAsync()
    {
        var coachId = await CreerUtilisateurAsync($"coach-go-{Guid.NewGuid()}@test.local");
        var jeuneEmail = $"jeune-go-{Guid.NewGuid()}@test.local";
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
