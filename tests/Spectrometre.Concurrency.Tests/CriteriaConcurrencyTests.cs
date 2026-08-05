using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Compatibility;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Compatibilite.Entities;
using Spectrometre.Modules.Compatibilite.Services;
using Spectrometre.Modules.ProfilCandidat.Data;
using Spectrometre.Modules.ProfilCandidat.Services;
using Spectrometre.Modules.ProfilEntreprise.Data;
using Spectrometre.Modules.ProfilEntreprise.Services;
using Spectrometre.Modules.SuiviEvolutif.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Reproduit le bug de perte de mise à jour identifié sur les grilles H/K : plusieurs cases cochées
/// « en même temps » (plusieurs appels concurrents de sauvegarde sur le même profil). Avant le correctif,
/// l'ancienne <c>SaveCompatibilityCriteriaAsync</c> relisait puis réécrivait TOUTE la grille à chaque
/// case cochée — deux appels concurrents pouvaient s'écraser mutuellement selon l'ordre d'achèvement de
/// leur écriture. Ces tests échouaient de façon intermittente (mais reproductible en quelques essais)
/// sur l'ancien code ; ils doivent passer de façon fiable et répétée avec le correctif
/// (mutation ciblée par champ + jeton de concurrence optimiste xmin + relecture/réapplication).
/// </summary>
[Collection("Base de données partagée")]
public sealed class CriteriaConcurrencyTests(ServiceFixture fixture)
{
    /// <summary>
    /// Force un démarrage vraiment simultané des tâches (plutôt que <c>Task.WhenAll</c> seul, où l'ordre
    /// de démarrage réel dépend du planificateur) : chaque tâche attend la barrière avant d'appeler le
    /// service, maximisant le chevauchement réel des écritures concurrentes.
    /// </summary>
    private static async Task RunConcurrentlyAsync(IReadOnlyList<Func<Task>> actions)
    {
        using var barrier = new Barrier(actions.Count);
        var tasks = actions.Select(action => Task.Run(async () =>
        {
            barrier.SignalAndWait();
            await action();
        })).ToArray();
        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task ConcurrentTagToggles_OnCandidateGrid_LoseNoUpdate()
    {
        var candidateService = fixture.Services.GetRequiredService<ICandidateProfileService>();
        var userId = $"test-concurrency-candidat-{Guid.NewGuid()}";
        var candidateProfileId = await candidateService.GetOrCreateProfileIdAsync(userId);

        // Les 10 tags techniques du vocabulaire partagé, cochés tous en même temps sur le même profil —
        // c'est exactement le scénario qui provoquait la perte de mise à jour (ex. plusieurs cases de la
        // grille H cochées coup sur coup, avant que la sauvegarde précédente n'ait terminé).
        var allTags = CompatibilityVocabulary.TechniqueTags;
        var actions = allTags
            .Select(tag => (Func<Task>)(() => candidateService.ToggleTagAsync(candidateProfileId, CriteriaField.Technique, tag, isChecked: true)))
            .ToList();

        await RunConcurrentlyAsync(actions);

        var criteria = await candidateService.GetCompatibilityCriteriaAsync(candidateProfileId);
        Assert.NotNull(criteria);
        foreach (var tag in allTags)
            Assert.Contains(tag, criteria!.TechniqueTags);
        Assert.Equal(allTags.Count, criteria!.TechniqueTags.Count);

        await CleanupCandidateAsync(candidateProfileId);
    }

    [Fact]
    public async Task ConcurrentWritesAcrossDifferentAxes_OnCandidateGrid_LoseNoUpdate()
    {
        // Concurrence plus hétérogène : tags sur 3 axes différents + le rythme + une note, tous en même
        // temps sur le même profil — vérifie que la mutation ciblée par champ ne se marche pas dessus
        // même quand les champs modifiés sont différents (pas seulement le même axe/la même liste).
        var candidateService = fixture.Services.GetRequiredService<ICandidateProfileService>();
        var userId = $"test-concurrency-candidat-mixte-{Guid.NewGuid()}";
        var candidateProfileId = await candidateService.GetOrCreateProfileIdAsync(userId);

        var actions = new List<Func<Task>>();
        actions.AddRange(CompatibilityVocabulary.TechniqueTags.Take(3)
            .Select(tag => (Func<Task>)(() => candidateService.ToggleTagAsync(candidateProfileId, CriteriaField.Technique, tag, true))));
        actions.AddRange(CompatibilityVocabulary.ComportementaleTags.Take(3)
            .Select(tag => (Func<Task>)(() => candidateService.ToggleTagAsync(candidateProfileId, CriteriaField.Comportementale, tag, true))));
        actions.AddRange(CompatibilityVocabulary.PointsVigilanceTags.Take(3)
            .Select(tag => (Func<Task>)(() => candidateService.ToggleTagAsync(candidateProfileId, CriteriaField.PointsVigilance, tag, true))));
        actions.Add(() => candidateService.SetRythmeTravailAsync(candidateProfileId, 4));
        actions.Add(() => candidateService.SetNotesAsync(candidateProfileId, CriteriaField.Technique, "Note concurrente"));

        await RunConcurrentlyAsync(actions);

        var criteria = await candidateService.GetCompatibilityCriteriaAsync(candidateProfileId);
        Assert.NotNull(criteria);
        Assert.Equal(3, criteria!.TechniqueTags.Count);
        Assert.Equal(3, criteria.ComportementaleTags.Count);
        Assert.Equal(3, criteria.PointsVigilanceTags.Count);
        Assert.Equal(4, criteria.RythmeTravail);
        Assert.Equal("Note concurrente", criteria.TechniqueNotes);

        await CleanupCandidateAsync(candidateProfileId);
    }

    [Fact]
    public async Task ConcurrentTagToggles_OnCompanyGrid_LoseNoUpdate()
    {
        var companyService = fixture.Services.GetRequiredService<ICompanyProfileService>();

        // Schéma "public" (gabarit) : aucune vraie entreprise n'y vit jamais (voir ITenantSchemaNameGenerator),
        // c'est le bac à sable sûr déjà utilisé pour le design-time de ce module tenant-scopé.
        var tenantContext = fixture.Services.GetRequiredService<ITenantContext>();
        tenantContext.SetActiveCompany(0, "public");

        var companyProfileId = await companyService.GetOrCreateProfileIdAsync();

        var allTags = CompatibilityVocabulary.ComportementaleTags;
        var actions = allTags
            .Select(tag => (Func<Task>)(() => companyService.ToggleTagAsync(companyProfileId, CriteriaField.Comportementale, tag, isChecked: true)))
            .ToList();

        await RunConcurrentlyAsync(actions);

        var criteria = await companyService.GetCompatibilityCriteriaAsync(companyProfileId);
        Assert.NotNull(criteria);
        foreach (var tag in allTags)
            Assert.Contains(tag, criteria!.ComportementaleTags);
        Assert.Equal(allTags.Count, criteria!.ComportementaleTags.Count);

        await CleanupCompanyAsync(companyProfileId);
    }

    [Fact]
    public async Task CompatibilityScore_AfterConcurrentWrites_MatchesPersistedState()
    {
        // Exigence explicite du cycle : le score recalculé après des écritures concurrentes doit rester
        // cohérent avec l'état RÉELLEMENT persisté — pas un instantané en mémoire potentiellement obsolète.
        var candidateService = fixture.Services.GetRequiredService<ICandidateProfileService>();
        var companyService = fixture.Services.GetRequiredService<ICompanyProfileService>();
        var compatibiliteService = fixture.Services.GetRequiredService<ICompatibiliteService>();
        var tenantContext = fixture.Services.GetRequiredService<ITenantContext>();
        tenantContext.SetActiveCompany(0, "public");

        var userId = $"test-concurrency-score-{Guid.NewGuid()}";
        var candidateProfileId = await candidateService.GetOrCreateProfileIdAsync(userId);
        var companyProfileId = await companyService.GetOrCreateProfileIdAsync();

        // Écriture concurrente côté candidat sur les mêmes tags que l'entreprise attend déjà (recouvrement
        // partiel connu à l'avance pour pouvoir recalculer le score attendu à la main).
        var companyTags = CompatibilityVocabulary.TechniqueTags.Take(4).ToList();
        foreach (var tag in companyTags)
            await companyService.ToggleTagAsync(companyProfileId, CriteriaField.Technique, tag, true);

        var candidateTagsToToggle = CompatibilityVocabulary.TechniqueTags.Skip(2).Take(4).ToList(); // chevauchement partiel avec companyTags
        var actions = candidateTagsToToggle
            .Select(tag => (Func<Task>)(() => candidateService.ToggleTagAsync(candidateProfileId, CriteriaField.Technique, tag, true)))
            .ToList();
        await RunConcurrentlyAsync(actions);

        var result = await compatibiliteService.CalculerCompatibiliteAsync(candidateProfileId, companyProfileId);

        // Recalcul indépendant (même formule de Jaccard que StructuredCriteriaScorer.TagOverlapScore,
        // volontairement réimplémentée ici plutôt qu'appelée — le but est de vérifier le moteur depuis
        // l'extérieur, à partir de l'état réellement persisté, pas de réutiliser son propre code interne).
        var persistedCandidateCriteria = await candidateService.GetCompatibilityCriteriaAsync(candidateProfileId);
        Assert.NotNull(persistedCandidateCriteria);
        Assert.Equal(candidateTagsToToggle.Count, persistedCandidateCriteria!.TechniqueTags.Count);

        var expectedTechniqueScore = ExpectedJaccardScore(persistedCandidateCriteria.TechniqueTags, companyTags);
        var actualTechniqueScore = result.ScoresParAxe.Single(a => a.Axis == CompatibilityAxis.Technique).Score;

        Assert.Equal(expectedTechniqueScore, actualTechniqueScore);

        await CleanupCandidateAsync(candidateProfileId);
        await CleanupCompanyAsync(companyProfileId);
    }

    [Fact]
    public async Task ConcurrentSaveAnswerAsync_OnSameQuestion_LosesNoWrite()
    {
        // SaveAnswerAsync n'avait, avant ce correctif, aucune protection de concurrence (contrairement à
        // ToggleTagAsync/SetRythmeTravailAsync/SetNotesAsync) : même bug potentiel qu'avant le correctif de
        // la grille H, jamais couvert par un test. Contrairement à un tag (une LISTE où plusieurs éléments
        // concurrents coexistent), une réponse est un champ SCALAIRE : un seul texte peut « gagner » à la
        // fin, ce n'est pas un bug. Ce qui SERAIT un bug : une exception (le correctif ne convergerait pas
        // sous contention), un état final ne correspondant à AUCUNE des valeurs tentées (corruption), ou un
        // appel concurrent silencieusement absent de l'historique (une modification qui disparaît sans trace).
        var candidateService = fixture.Services.GetRequiredService<ICandidateProfileService>();
        var userId = $"test-concurrency-reponse-{Guid.NewGuid()}";
        var candidateProfileId = await candidateService.GetOrCreateProfileIdAsync(userId);

        var questions = await candidateService.GetQuestionnaireAsync(candidateProfileId);
        var questionId = questions[0].QuestionId;

        var valeurs = Enumerable.Range(0, 8).Select(i => $"Réponse concurrente n°{i}").ToList();
        var actions = valeurs
            .Select(valeur => (Func<Task>)(() => candidateService.SaveAnswerAsync(candidateProfileId, questionId, valeur)))
            .ToList();

        await RunConcurrentlyAsync(actions); // Ne doit lever aucune exception malgré la forte contention.

        var questionnaireApres = await candidateService.GetQuestionnaireAsync(candidateProfileId);
        var reponseFinale = questionnaireApres.Single(q => q.QuestionId == questionId).AnswerText;
        Assert.Contains(reponseFinale, valeurs); // État final valide : une des valeurs tentées, jamais une valeur corrompue/partielle.

        var historique = await fixture.Services.GetRequiredService<ISuiviEvolutifService>()
            .GetHistoriqueCandidatAsync(candidateProfileId);
        // Chaque tentative concurrente qui a réellement écrit doit laisser une trace — aucune ne doit
        // disparaître silencieusement, même si une seule "gagne" au final sur le profil vivant.
        Assert.Equal(valeurs.Count, historique.Count);
        Assert.All(historique, entree => Assert.Contains(entree.NouvelleValeur, valeurs));

        await CleanupCandidateAsync(candidateProfileId);
    }

    private static int ExpectedJaccardScore(IReadOnlyList<string> a, IReadOnlyList<string> b)
    {
        if (a.Count == 0 || b.Count == 0) return 50;
        var setA = new HashSet<string>(a, StringComparer.OrdinalIgnoreCase);
        var setB = new HashSet<string>(b, StringComparer.OrdinalIgnoreCase);
        var intersection = setA.Intersect(setB, StringComparer.OrdinalIgnoreCase).Count();
        var union = setA.Union(setB, StringComparer.OrdinalIgnoreCase).Count();
        return union == 0 ? 50 : (int)Math.Round(100.0 * intersection / union);
    }

    private async Task CleanupCandidateAsync(int candidateProfileId)
    {
        await using var db = await fixture.Services.GetRequiredService<IDbContextFactory<ProfilCandidatDbContext>>().CreateDbContextAsync();
        var criteria = await db.CandidateCompatibilityCriteria.Where(c => c.CandidateProfileId == candidateProfileId).ToListAsync();
        db.CandidateCompatibilityCriteria.RemoveRange(criteria);
        var profile = await db.CandidateProfiles.FindAsync(candidateProfileId);
        if (profile is not null) db.CandidateProfiles.Remove(profile);
        await db.SaveChangesAsync();
    }

    private async Task CleanupCompanyAsync(int companyProfileId)
    {
        var dbFactory = fixture.Services.GetRequiredService<IDbContextFactory<ProfilEntrepriseDbContext>>();
        await using var db = await dbFactory.CreateDbContextAsync();
        db.TenantSchema = "public";
        var criteria = await db.CompanyCompatibilityCriteria.Where(c => c.CompanyProfileId == companyProfileId).ToListAsync();
        db.CompanyCompatibilityCriteria.RemoveRange(criteria);
        var profile = await db.CompanyProfiles.FindAsync(companyProfileId);
        if (profile is not null) db.CompanyProfiles.Remove(profile);
        await db.SaveChangesAsync();
    }
}
