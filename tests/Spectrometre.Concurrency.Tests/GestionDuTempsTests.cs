using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Modules.GestionDuTemps.Entities;
using Spectrometre.Modules.GestionDuTemps.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Vérifie le noyau du module Gestion du temps : scope strict par utilisateur (aucune notion d'entreprise
/// à vérifier — modèle d'autorisation plus simple que les autres modules, voir <see cref="IGestionDuTempsService"/>),
/// et le rattachement optionnel à une entreprise réellement gérée par l'utilisateur (<c>UserCompanyLink</c>).
/// </summary>
[Collection("Base de données partagée")]
public sealed class GestionDuTempsTests(ServiceFixture fixture)
{
    [Fact]
    public async Task GetTypesDeTempsAsync_CreeLes6CategoriesParDefaut_AuPremierAcces()
    {
        var service = fixture.Services.GetRequiredService<IGestionDuTempsService>();
        var userId = $"gdt-test-defauts-{Guid.NewGuid()}";

        var types = await service.GetTypesDeTempsAsync(userId);

        Assert.Equal(6, types.Count);
        Assert.Contains(types, t => t.Cle == "pro");
        Assert.Contains(types, t => t.Cle == "perso");

        // Un second appel ne doit pas dupliquer les catégories déjà créées.
        var typesApres = await service.GetTypesDeTempsAsync(userId);
        Assert.Equal(6, typesApres.Count);
    }

    [Fact]
    public async Task CreateActiviteAsync_UnRappelCree_ApparaitDansLaListeEtEstScopeAUtilisateur()
    {
        var service = fixture.Services.GetRequiredService<IGestionDuTempsService>();
        var userId = $"gdt-test-rappel-{Guid.NewGuid()}";
        var autreUserId = $"gdt-test-autre-{Guid.NewGuid()}";

        var types = await service.GetTypesDeTempsAsync(userId);
        var typePro = types.Single(t => t.Cle == "pro");

        var activiteId = await service.CreateActiviteAsync(
            userId, typePro.Id, "Point d'équipe", new DateOnly(2026, 8, 10), new TimeOnly(9, 30), 30, companyId: null);

        var activites = await service.GetActivitesAsync(userId, companyId: null, personnelUniquement: false);
        var activite = Assert.Single(activites);
        Assert.Equal(activiteId, activite.Id);
        Assert.Equal("Point d'équipe", activite.Nom);
        Assert.Equal(ActiviteStatuts.AFaire, activite.Statut);
        Assert.Null(activite.CompanyId);

        // Bascule de statut.
        await service.SetActiviteStatutAsync(userId, activiteId, ActiviteStatuts.Fait);
        var activitesApres = await service.GetActivitesAsync(userId, companyId: null, personnelUniquement: false);
        Assert.Equal(ActiviteStatuts.Fait, activitesApres.Single().Statut);

        // Un autre utilisateur (même base) n'a par construction aucun type de temps ni rappel en commun —
        // la création lui crée SES propres catégories par défaut, jamais un accès à celles de userId.
        var activitesAutreUser = await service.GetActivitesAsync(autreUserId, companyId: null, personnelUniquement: false);
        Assert.Empty(activitesAutreUser);

        // Suppression : n'affecte que ce rappel, scopé par utilisateur (vérifié implicitement — DeleteActiviteAsync
        // filtre par UserId, voir GestionDuTempsService).
        await service.DeleteActiviteAsync(userId, activiteId);
        Assert.Empty(await service.GetActivitesAsync(userId, companyId: null, personnelUniquement: false));
    }

    [Fact]
    public async Task UpsertTypeDeTempsAsync_AvecEntrepriseNonGeree_EstRefuse()
    {
        var service = fixture.Services.GetRequiredService<IGestionDuTempsService>();
        var userId = $"gdt-test-entreprise-refusee-{Guid.NewGuid()}";
        var suffix = Guid.NewGuid();

        // Entreprise réelle, mais gérée par un AUTRE utilisateur — userId n'a aucun UserCompanyLink dessus.
        var company = await fixture.CreateCompanyAsync($"Entreprise GDT Refus {suffix}", $"gdt-test-owner-{suffix}");

        await service.GetTypesDeTempsAsync(userId); // force la création des catégories par défaut

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpsertTypeDeTempsAsync(userId, id: null, "projet-x", "Projet X", new TimeOnly(9, 0), new TimeOnly(10, 0), "L,M", 10, company.Id));

        Assert.Contains(company.Id.ToString(), ex.Message);
    }

    [Fact]
    public async Task RappelRattacheAUneEntrepriseGeree_ApparaitDansLeFiltreEntrepriseEtPasDansPersonnel()
    {
        var service = fixture.Services.GetRequiredService<IGestionDuTempsService>();
        var suffix = Guid.NewGuid();
        var userId = $"gdt-test-owner-{suffix}";

        // CreateCompanyAsync crée l'entreprise ET lie ownerUserId via UserCompanyLink (voir ServiceFixture).
        var company = await fixture.CreateCompanyAsync($"Entreprise GDT {suffix}", userId);

        var types = await service.GetTypesDeTempsAsync(userId);
        var typePro = types.Single(t => t.Cle == "pro");

        var rappelPersoId = await service.CreateActiviteAsync(
            userId, typePro.Id, "Rappel personnel", new DateOnly(2026, 8, 11), new TimeOnly(8, 0), 15, companyId: null);
        var rappelEntrepriseId = await service.CreateActiviteAsync(
            userId, typePro.Id, "Gérer mon entreprise", new DateOnly(2026, 8, 11), new TimeOnly(10, 0), 60, company.Id);

        var tout = await service.GetActivitesAsync(userId, companyId: null, personnelUniquement: false);
        Assert.Equal(2, tout.Count);

        var personnel = await service.GetActivitesAsync(userId, companyId: null, personnelUniquement: true);
        var seulPersonnel = Assert.Single(personnel);
        Assert.Equal(rappelPersoId, seulPersonnel.Id);

        var pourEntreprise = await service.GetActivitesAsync(userId, company.Id, personnelUniquement: false);
        var seulEntreprise = Assert.Single(pourEntreprise);
        Assert.Equal(rappelEntrepriseId, seulEntreprise.Id);
        Assert.Equal(company.Id, seulEntreprise.CompanyId);
    }
}
