using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Invitations;
using Spectrometre.Core.Notifications;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.JeunesPrestataires.Catalog;
using Spectrometre.Modules.JeunesPrestataires.Data;
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
        Assert.True(AutoObservationSyntheseDocument.TryParse(synthese, out var doc));
        Assert.NotNull(doc.TroncCommun);
        Assert.Contains(doc.TroncCommun.Lignes, l => l.Theme.Contains("Forces", StringComparison.Ordinal));
        Assert.NotNull(doc.Employabilite);
        Assert.Null(doc.Orientation);
        Assert.Contains("Calme", doc.TroncCommun.Lignes.First(l => l.Theme.StartsWith("Forces", StringComparison.Ordinal)).Contenu, StringComparison.Ordinal);

        var syntheseCoach = await svc.RegenererSyntheseAsync(coachId, profileId);
        Assert.True(AutoObservationSyntheseDocument.TryParse(syntheseCoach, out _));
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

        Assert.Equal("p0.s13", AutoObservationCatalog.Part0Sections[^1].Key);
        Assert.Equal(5, AutoObservationCatalog.CategorieBSections.Count);
        Assert.Equal(5, AutoObservationCatalog.CategorieASections.Count);
        Assert.Equal("p2.s14", AutoObservationCatalog.Part2Sections[^6].Key);
        Assert.Equal("p2.s18", AutoObservationCatalog.Part2Sections[^2].Key);
        Assert.Equal("p2.s13", AutoObservationCatalog.Part2Sections[^1].Key);

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

    [Fact]
    public async Task CategorieA_SauvegardeSansToucherSectionExistante()
    {
        var (_, jeuneId, profileId) = await CreerJeuneAvecCoachAsync();
        var svc = fixture.Services.GetRequiredService<IAutoObservationService>();

        Assert.True(await svc.SaveSectionAsync(
            jeuneId,
            profileId,
            "p0.s1",
            [new AutoObservationAnswerInput("p0.s1.experiences", "de stage|de bénévolat", null)]));

        Assert.True(await svc.SaveSectionAsync(
            jeuneId,
            profileId,
            "p2.s14",
            [
                new AutoObservationAnswerInput("p2.s14.essayer", "Jardinage|Autre", null),
                new AutoObservationAnswerInput("p2.s14.essayer.autre", "Arrosage", null),
                new AutoObservationAnswerInput("p2.s14.faciles", "Rangement", null),
            ]));

        var existante = await svc.TryGetSectionAsync(jeuneId, profileId, "p0.s1");
        Assert.Equal("de stage|de bénévolat", existante!.Answers.Single(a => a.QuestionKey == "p0.s1.experiences").TextValue);

        var enrichie = await svc.TryGetSectionAsync(jeuneId, profileId, "p2.s14");
        Assert.Equal("Jardinage|Autre", enrichie!.Answers.Single(a => a.QuestionKey == "p2.s14.essayer").TextValue);
        Assert.Equal("Arrosage", enrichie.Answers.Single(a => a.QuestionKey == "p2.s14.essayer.autre").TextValue);
        Assert.Equal("Rangement", enrichie.Answers.Single(a => a.QuestionKey == "p2.s14.faciles").TextValue);
    }

    [Fact]
    public void AllSections_CompteLesSectionsEnrichiesAEtB()
    {
        const int part0Avant = 8;
        const int part1 = 5;
        const int part2Avant = 13;
        Assert.Equal(part0Avant + 5, AutoObservationCatalog.Part0Sections.Count);
        Assert.Equal(part2Avant + 5, AutoObservationCatalog.Part2Sections.Count);
        Assert.Equal(part0Avant + 5 + part1 + part2Avant + 5, AutoObservationCatalog.AllSections.Count);
        Assert.Equal(1, AutoObservationCatalog.Part0Sections.Count(s => s.Key == "p0.s1"));
        Assert.Equal(1, AutoObservationCatalog.TryGetSection("p0.s1")!.Questions.Count);
    }

    [Fact]
    public async Task RegenererSynthese_SansExperience_Tableau2A_Pas2B()
    {
        var (_, jeuneId, profileId) = await CreerJeuneAvecCoachAsync(ProfilAccompagnement.SansExperience);
        var svc = fixture.Services.GetRequiredService<IAutoObservationService>();

        Assert.True(AutoObservationSyntheseDocument.TryParse(
            await svc.RegenererSyntheseAsync(jeuneId, profileId), out var doc));
        Assert.Equal(nameof(ProfilAccompagnement.SansExperience), doc.Profil);
        Assert.Equal(5, doc.TroncCommun.Lignes.Count);
        Assert.Equal("T2A", doc.Employabilite!.Code);
        Assert.Null(doc.Orientation);
        Assert.Contains(doc.Employabilite.Lignes, l => l.Theme.StartsWith("Missions que le jeune souhaite", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RegenererSynthese_Autonome_Tableau2B_Pas2A()
    {
        var (_, jeuneId, profileId) = await CreerJeuneAvecCoachAsync(ProfilAccompagnement.Autonome);
        var svc = fixture.Services.GetRequiredService<IAutoObservationService>();

        Assert.True(AutoObservationSyntheseDocument.TryParse(
            await svc.RegenererSyntheseAsync(jeuneId, profileId), out var doc));
        Assert.Equal(nameof(ProfilAccompagnement.Autonome), doc.Profil);
        Assert.Equal(5, doc.TroncCommun.Lignes.Count);
        Assert.Null(doc.Employabilite);
        Assert.Equal("T2B", doc.Orientation!.Code);
        Assert.Contains(doc.Orientation.Lignes, l => l.Theme.StartsWith("Pistes d'études", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TryGetPage_RegenereAncienFormatMarkdown()
    {
        var (_, jeuneId, profileId) = await CreerJeuneAvecCoachAsync();
        var svc = fixture.Services.GetRequiredService<IAutoObservationService>();

        await using (var db = await fixture.Services.GetRequiredService<IDbContextFactory<JeunesPrestatairesDbContext>>().CreateDbContextAsync())
        {
            db.AutoObservationSynthesesGenerees.Add(new AutoObservationSyntheseGeneree
            {
                JeuneProfileId = profileId,
                Contenu = "## Forces perçues\n- ancien format markdown",
                GenereeLe = DateTimeOffset.UtcNow.AddDays(-2),
            });
            await db.SaveChangesAsync();
        }

        var page = await svc.TryGetPageAsync(jeuneId);
        Assert.NotNull(page!.Synthese);
        Assert.Equal("T1", page.Synthese.TroncCommun.Code);
        Assert.NotNull(page.Synthese.Employabilite);

        await using (var db = await fixture.Services.GetRequiredService<IDbContextFactory<JeunesPrestatairesDbContext>>().CreateDbContextAsync())
        {
            var stored = await db.AutoObservationSynthesesGenerees.SingleAsync(s => s.JeuneProfileId == profileId);
            Assert.True(AutoObservationSyntheseDocument.TryParse(stored.Contenu, out _));
        }
    }

    [Fact]
    public void SuggereProfil_AutonomeUniquementSiSignalUnivoque_SinonSansExperience()
    {
        Assert.Equal(
            ProfilAccompagnement.SansExperience,
            AutoObservationOrientationCatalog.SuggereProfil(Reponses(
                AutoObservationOrientationCatalog.Non,
                AutoObservationOrientationCatalog.Non,
                AutoObservationOrientationCatalog.Oui,
                AutoObservationOrientationCatalog.Non,
                AutoObservationOrientationCatalog.Oui)));

        Assert.Equal(
            ProfilAccompagnement.Autonome,
            AutoObservationOrientationCatalog.SuggereProfil(Reponses(
                AutoObservationOrientationCatalog.Oui,
                AutoObservationOrientationCatalog.Oui,
                AutoObservationOrientationCatalog.Non,
                AutoObservationOrientationCatalog.Oui,
                AutoObservationOrientationCatalog.Non)));

        Assert.Equal(
            ProfilAccompagnement.SansExperience,
            AutoObservationOrientationCatalog.SuggereProfil(Reponses(
                AutoObservationOrientationCatalog.Oui,
                AutoObservationOrientationCatalog.Non,
                AutoObservationOrientationCatalog.Oui,
                AutoObservationOrientationCatalog.Oui,
                AutoObservationOrientationCatalog.JeNeSaisPas)));

        Assert.Equal(
            ProfilAccompagnement.SansExperience,
            AutoObservationOrientationCatalog.SuggereProfil(Reponses(
                AutoObservationOrientationCatalog.UnPeu,
                AutoObservationOrientationCatalog.Oui,
                AutoObservationOrientationCatalog.Non,
                AutoObservationOrientationCatalog.Oui,
                AutoObservationOrientationCatalog.Non)));

        Assert.DoesNotContain(
            AutoObservationCatalog.AllSections,
            s => s.Key == AutoObservationOrientationCatalog.SectionKey);
    }

    [Fact]
    public async Task Orientation_SansExperienceClaire_EcraseChoixCoachAutonome()
    {
        var (coachId, jeuneId, profileId) = await CreerJeuneAvecCoachAsync(ProfilAccompagnement.Autonome);
        var svc = fixture.Services.GetRequiredService<IAutoObservationService>();
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();

        var page = await svc.TryGetPageAsync(jeuneId);
        Assert.True(page!.OrientationAFaire);
        Assert.False((await svc.TryGetPageAsync(coachId, profileId))!.OrientationAFaire);

        Assert.True(await svc.EnregistrerOrientationAsync(jeuneId, profileId, Inputs(
            AutoObservationOrientationCatalog.Non,
            AutoObservationOrientationCatalog.Non,
            AutoObservationOrientationCatalog.Oui,
            AutoObservationOrientationCatalog.Non,
            AutoObservationOrientationCatalog.Oui)));

        var apres = await jeuneService.TryGetByIdAsync(profileId);
        Assert.Equal(ProfilAccompagnement.SansExperience, apres!.ProfilAccompagnement);
        Assert.False((await svc.TryGetPageAsync(jeuneId))!.OrientationAFaire);
        Assert.Equal(
            2,
            AutoObservationCatalog.GetSectionsOrdonnees(apres.ProfilAccompagnement)[0].PartNumber);
    }

    [Fact]
    public async Task Orientation_AutonomeClaire_EcraseChoixCoachSansExperience()
    {
        var (_, jeuneId, profileId) = await CreerJeuneAvecCoachAsync(ProfilAccompagnement.SansExperience);
        var svc = fixture.Services.GetRequiredService<IAutoObservationService>();
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();

        Assert.True(await svc.EnregistrerOrientationAsync(jeuneId, profileId, Inputs(
            AutoObservationOrientationCatalog.Oui,
            AutoObservationOrientationCatalog.Oui,
            AutoObservationOrientationCatalog.Non,
            AutoObservationOrientationCatalog.Oui,
            AutoObservationOrientationCatalog.Non)));

        var apres = await jeuneService.TryGetByIdAsync(profileId);
        Assert.Equal(ProfilAccompagnement.Autonome, apres!.ProfilAccompagnement);
        Assert.Equal(0, AutoObservationCatalog.GetSectionsOrdonnees(apres.ProfilAccompagnement)[0].PartNumber);
    }

    [Fact]
    public async Task Orientation_ReponsesMixtes_RetombeSurSansExperience()
    {
        var (_, jeuneId, profileId) = await CreerJeuneAvecCoachAsync(ProfilAccompagnement.Autonome);
        var svc = fixture.Services.GetRequiredService<IAutoObservationService>();
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();

        Assert.True(await svc.EnregistrerOrientationAsync(jeuneId, profileId, Inputs(
            AutoObservationOrientationCatalog.Oui,
            AutoObservationOrientationCatalog.Oui,
            AutoObservationOrientationCatalog.Oui,
            AutoObservationOrientationCatalog.Oui,
            AutoObservationOrientationCatalog.JeNeSaisPas)));

        Assert.Equal(
            ProfilAccompagnement.SansExperience,
            (await jeuneService.TryGetByIdAsync(profileId))!.ProfilAccompagnement);
    }

    [Fact]
    public async Task Orientation_Ignoree_GardeProfilCoach_EtCoachPeutCorrigerEnsuite()
    {
        var (coachId, jeuneId, profileId) = await CreerJeuneAvecCoachAsync(ProfilAccompagnement.Autonome);
        var svc = fixture.Services.GetRequiredService<IAutoObservationService>();
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();

        Assert.True(await svc.PasserOrientationAsync(jeuneId, profileId));
        Assert.Equal(ProfilAccompagnement.Autonome, (await jeuneService.TryGetByIdAsync(profileId))!.ProfilAccompagnement);
        Assert.False((await svc.TryGetPageAsync(jeuneId))!.OrientationAFaire);

        var jeuneUserId = (await jeuneService.TryGetByIdAsync(profileId))!.UserId;
        Assert.True(await jeuneService.MettreAJourProfilAccompagnementAsync(
            coachId, jeuneUserId, ProfilAccompagnement.SansExperience));
        Assert.Equal(ProfilAccompagnement.SansExperience, (await jeuneService.TryGetByIdAsync(profileId))!.ProfilAccompagnement);

        Assert.False(await svc.EnregistrerOrientationAsync(jeuneId, profileId, Inputs(
            AutoObservationOrientationCatalog.Oui,
            AutoObservationOrientationCatalog.Oui,
            AutoObservationOrientationCatalog.Non,
            AutoObservationOrientationCatalog.Oui,
            AutoObservationOrientationCatalog.Non)));
        Assert.Equal(ProfilAccompagnement.SansExperience, (await jeuneService.TryGetByIdAsync(profileId))!.ProfilAccompagnement);
    }

    private static Dictionary<string, string?> Reponses(string q1, string q2, string q3, string q4, string q5) => new(StringComparer.Ordinal)
    {
        [AutoObservationOrientationCatalog.Q1] = q1,
        [AutoObservationOrientationCatalog.Q2] = q2,
        [AutoObservationOrientationCatalog.Q3] = q3,
        [AutoObservationOrientationCatalog.Q4] = q4,
        [AutoObservationOrientationCatalog.Q5] = q5,
    };

    private static List<AutoObservationAnswerInput> Inputs(string q1, string q2, string q3, string q4, string q5) =>
    [
        new(AutoObservationOrientationCatalog.Q1, q1, null),
        new(AutoObservationOrientationCatalog.Q2, q2, null),
        new(AutoObservationOrientationCatalog.Q3, q3, null),
        new(AutoObservationOrientationCatalog.Q4, q4, null),
        new(AutoObservationOrientationCatalog.Q5, q5, null),
    ];

    private async Task<(string CoachId, string JeuneId, int ProfileId)> CreerJeuneAvecCoachAsync(
        ProfilAccompagnement profil = ProfilAccompagnement.SansExperience)
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
            "http://localhost",
            profil);
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
