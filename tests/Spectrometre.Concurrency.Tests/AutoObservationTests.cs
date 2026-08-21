using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Invitations;
using Spectrometre.Core.Notifications;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.JeunesPrestataires.Catalog;
using Spectrometre.Modules.JeunesPrestataires.Entities;
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

    [Fact]
    public void AllSections_InclutPart0EnTete_AvecClesP0()
    {
        var sections = AutoObservationCatalog.AllSections;
        Assert.True(sections.Count >= AutoObservationCatalog.Part0Sections.Count
            + AutoObservationCatalog.Part1Sections.Count
            + AutoObservationCatalog.Part2Sections.Count);

        Assert.Equal("p0.s1", sections[0].Key);
        Assert.Equal(0, sections[0].PartNumber);
        Assert.All(AutoObservationCatalog.Part0Sections, s => Assert.Equal(0, s.PartNumber));

        Assert.Contains(sections, s => s.Key == "p0.s7");
        var grille = Assert.Single(sections, s => s.Key == "p0.s7");
        Assert.Equal(24, grille.Questions.Count);
        Assert.Contains(grille.Questions, q => q.Key == "p0.s7.piste1.motivation");
        Assert.Contains(grille.Questions, q => q.Key == "p0.s7.piste4.utilite");
        Assert.All(grille.Questions, q => Assert.Equal(AutoObservationFieldType.Scale1To5, q.FieldType));

        Assert.Contains(sections, s => s.Key == "p0.s8");
        var conclusion = Assert.Single(sections, s => s.Key == "p0.s8");
        Assert.Equal(7, conclusion.Questions.Count);

        // Part1/Part2 conservées après Part0, clés inchangées
        var idxP1 = sections.ToList().FindIndex(s => s.Key == "p1.s1");
        var idxP2 = sections.ToList().FindIndex(s => s.Key == "p2.s1");
        Assert.True(idxP1 > 0);
        Assert.True(idxP2 > idxP1);
    }

    [Fact]
    public void GetSectionsOrdonnees_DependDuProfilAccompagnement()
    {
        var sansExp = AutoObservationCatalog.GetSectionsOrdonnees(ProfilAccompagnement.SansExperience);
        Assert.Equal(2, sansExp[0].PartNumber);
        Assert.Equal("p2.s1", sansExp[0].Key);
        Assert.Equal(
            AutoObservationCatalog.Part2Sections.Count
            + AutoObservationCatalog.Part1Sections.Count
            + AutoObservationCatalog.Part0Sections.Count,
            sansExp.Count);
        Assert.Equal("p0.s1", sansExp[^AutoObservationCatalog.Part0Sections.Count].Key);

        var autonome = AutoObservationCatalog.GetSectionsOrdonnees(ProfilAccompagnement.Autonome);
        Assert.Equal(0, autonome[0].PartNumber);
        Assert.Equal("p0.s1", autonome[0].Key);
        Assert.Equal("p2.s1", autonome[AutoObservationCatalog.Part0Sections.Count].Key);

        Assert.Equal("p0.s1", AutoObservationCatalog.AllSections[0].Key);
    }

    [Fact]
    public async Task Jeune_PeutSauvegarderSectionPart0()
    {
        var (_, jeuneId, profileId) = await CreerJeuneAvecCoachAsync();
        var svc = fixture.Services.GetRequiredService<IAutoObservationService>();

        Assert.True(await svc.SaveSectionAsync(
            jeuneId,
            profileId,
            "p0.s7",
            [
                new AutoObservationAnswerInput("p0.s7.piste1.motivation", null, 4),
                new AutoObservationAnswerInput("p0.s7.piste2.valeurs", null, 3),
            ]));

        var section = await svc.TryGetSectionAsync(jeuneId, profileId, "p0.s7");
        Assert.NotNull(section);
        Assert.Equal(4, section!.Answers.First(a => a.QuestionKey == "p0.s7.piste1.motivation").NumericValue);
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
