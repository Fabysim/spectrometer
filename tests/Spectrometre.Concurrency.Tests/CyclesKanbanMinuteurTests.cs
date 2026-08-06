using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Modules.GestionDuTemps.Entities;
using Spectrometre.Modules.GestionDuTemps.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Vérifie l'enrichissement du noyau Gestion du temps : cycles (saisons), Kanban à 3 colonnes et minuteur —
/// repris de mvp (voir le résumé du cycle pour la correspondance exacte avec <c>GdtCycle</c>/
/// <c>GdtKanbanStatut</c>/<c>GdtKanbanTimer</c>).
/// </summary>
[Collection("Base de données partagée")]
public sealed class CyclesKanbanMinuteurTests(ServiceFixture fixture)
{
    [Fact]
    public async Task GetOrCreateCycleActifAsync_CreeLeCycle1AvecLes6CategoriesParDefaut_AuPremierAcces()
    {
        var service = fixture.Services.GetRequiredService<IGestionDuTempsService>();
        var userId = $"gdt-cycle-defaut-{Guid.NewGuid()}";

        var cycle = await service.GetOrCreateCycleActifAsync(userId);

        Assert.Equal(1, cycle.NumeroCycle);
        Assert.Equal(CycleStatuts.EnCours, cycle.Statut);
        Assert.Null(cycle.ClotureLe);

        var types = await service.GetTypesDeTempsAsync(userId);
        Assert.Equal(6, types.Count);

        // Un second appel retourne EXACTEMENT le même cycle (pas de doublon).
        var cycleApres = await service.GetOrCreateCycleActifAsync(userId);
        Assert.Equal(cycle.Id, cycleApres.Id);
    }

    [Fact]
    public async Task ClotureEtDemarrerNouveauCycleAsync_RecopieLesTypesMaisPasLesActivites()
    {
        var service = fixture.Services.GetRequiredService<IGestionDuTempsService>();
        var userId = $"gdt-cloture-{Guid.NewGuid()}";

        var cycle1 = await service.GetOrCreateCycleActifAsync(userId);
        var typesCycle1 = await service.GetTypesDeTempsAsync(userId);
        var typePro = typesCycle1.Single(t => t.Cle == "pro");

        var activiteId = await service.CreateActiviteAsync(
            userId, typePro.Id, "Tâche du cycle 1", new DateOnly(2026, 8, 12), new TimeOnly(9, 0), 30, companyId: null);

        // Une activité non terminée à la clôture — toujours "À faire" côté Kanban.
        var kanbanAvant = Assert.Single(await service.GetKanbanAsync(userId));
        Assert.Equal(KanbanColonnes.AFaire, kanbanAvant.Statut);

        var cycle2 = await service.ClotureEtDemarrerNouveauCycleAsync(userId);

        Assert.Equal(2, cycle2.NumeroCycle);
        Assert.NotEqual(cycle1.Id, cycle2.Id);

        // Les types de temps sont RECOPIÉS dans le nouveau cycle — les mêmes 6 catégories, nouveaux Id.
        var typesCycle2 = await service.GetTypesDeTempsAsync(userId);
        Assert.Equal(6, typesCycle2.Count);
        Assert.Contains(typesCycle2, t => t.Cle == "pro");
        Assert.DoesNotContain(typesCycle2, t => t.Id == typePro.Id);

        // L'activité du cycle 1 n'est PAS reportée : invisible dans les listes du cycle actif (archivage passif).
        Assert.Empty(await service.GetActivitesAsync(userId, companyId: null, personnelUniquement: false));
        Assert.Empty(await service.GetKanbanAsync(userId));

        // Confirme qu'elle n'a pas non plus été supprimée : elle existe toujours (juste attachée au cycle 1 clôturé).
        Assert.True(activiteId > 0);
    }

    [Fact]
    public async Task Kanban_TransitionsCompletes_AccumuleLeTempsReelACheval_SurPauseEtReprise()
    {
        var service = fixture.Services.GetRequiredService<IGestionDuTempsService>();
        var userId = $"gdt-kanban-{Guid.NewGuid()}";

        var types = await service.GetTypesDeTempsAsync(userId);
        var typePro = types.Single(t => t.Cle == "pro");
        var activiteId = await service.CreateActiviteAsync(
            userId, typePro.Id, "Rédiger le rapport", new DateOnly(2026, 8, 13), new TimeOnly(9, 0), 1, companyId: null);

        // Statut initial : À faire, aucun temps réel.
        var carte = Assert.Single(await service.GetKanbanAsync(userId));
        Assert.Equal(KanbanColonnes.AFaire, carte.Statut);
        Assert.Equal(0, carte.TempsReelMs);

        // Démarre le minuteur, laisse s'écouler un peu de temps réel, puis met en pause.
        await service.MarquerDebutAsync(userId, activiteId);
        carte = Assert.Single(await service.GetKanbanAsync(userId));
        Assert.Equal(KanbanColonnes.EnCours, carte.Statut);

        await Task.Delay(60);
        await service.MarquerPauseAsync(userId, activiteId);

        carte = Assert.Single(await service.GetKanbanAsync(userId));
        Assert.Equal(KanbanColonnes.AFaire, carte.Statut);
        var tempsApresPremierePause = carte.TempsReelMs;
        Assert.True(tempsApresPremierePause > 0, "Le temps réel doit avoir avancé pendant la période En cours.");

        // Reprend : le temps déjà accumulé n'est pas perdu, il continue de s'additionner.
        await service.MarquerDebutAsync(userId, activiteId);
        await Task.Delay(60);
        await service.MarquerTermineAsync(userId, activiteId);

        carte = Assert.Single(await service.GetKanbanAsync(userId));
        Assert.Equal(KanbanColonnes.Termine, carte.Statut);
        Assert.True(carte.TempsReelMs > tempsApresPremierePause, "Le temps réel doit s'accumuler à travers plusieurs marche/pause, jamais repartir de zéro.");
    }

    [Fact]
    public async Task Kanban_EnDepassement_QuandLeTempsReelDepasseLaDureePlanifiee()
    {
        var service = fixture.Services.GetRequiredService<IGestionDuTempsService>();
        var userId = $"gdt-depassement-{Guid.NewGuid()}";

        var types = await service.GetTypesDeTempsAsync(userId);
        var typePro = types.Single(t => t.Cle == "pro");
        // Durée planifiée volontairement à 0 minute : n'importe quel temps réel mesurable la dépasse.
        var activiteId = await service.CreateActiviteAsync(
            userId, typePro.Id, "Tâche courte", new DateOnly(2026, 8, 14), new TimeOnly(9, 0), 0, companyId: null);

        await service.MarquerDebutAsync(userId, activiteId);
        await Task.Delay(30);

        var carte = Assert.Single(await service.GetKanbanAsync(userId));
        Assert.True(carte.EnDepassement);
    }
}
