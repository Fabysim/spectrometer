using Spectrometre.Modules.ProfilEntreprise.Services;
using Spectrometre.Modules.ProfilEntreprise.Entities;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Recrutement.Entities;
using Spectrometre.Modules.Recrutement.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Socle générique <see cref="CatalogueCriteresSuggeres"/> (extrait mvp, 4 catégories) :
/// ajout via Upsert sans doublon (même clé Categorie|Libelle que GenererCriteresIaAsync).
/// </summary>
[Collection("Base de données partagée")]
public sealed class CatalogueCriteresSuggeresTests(ServiceFixture fixture)
{
    [Fact]
    public void SocleGenerique_ContientQuatreCategoriesEtVingtItems()
    {
        Assert.Equal(20, CatalogueCriteresSuggeres.Tous.Count);
        Assert.Equal(4, CatalogueCriteresSuggeres.Tous.Select(i => i.Categorie).Distinct().Count());
        Assert.Contains(CatalogueCriteresSuggeres.Tous, i => i.Libelle == "Sens de l'engagement");
        Assert.DoesNotContain(CatalogueCriteresSuggeres.Tous, i => i.Libelle.Contains("WMS", StringComparison.Ordinal));
        Assert.DoesNotContain(CatalogueCriteresSuggeres.Tous, i => i.Categorie == "Compétences techniques");
    }

    [Fact]
    public async Task AjoutSuggestions_IgnoreLesDoublonsDejaPresents()
    {
        var suffix = Guid.NewGuid();
        var company = await fixture.CreateCompanyAsync($"Entreprise Suggestions {suffix}", $"sug-{suffix}");

        using var scope = fixture.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>()
            .SetActiveCompany(company.Id, company.SchemaName);
        var service = scope.ServiceProvider.GetRequiredService<IPosteService>();

        var posteId = await service.CreatePosteAsync($"Poste sug {suffix}", null, null);
        var premier = CatalogueCriteresSuggeres.Tous[0];
        await service.UpsertCritereAsync(
            posteId, null, premier.Categorie, premier.Libelle, (int)NiveauEvaluation.Moyen, 0);

        var aAjouter = CatalogueCriteresSuggeres.Tous.Take(3).ToList();
        var existants = (await service.GetCriteresAsync(posteId)).ToList();
        var cles = existants
            .Select(c => $"{c.Categorie.Trim()}|{c.Libelle.Trim()}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var prochain = existants.Max(c => c.OrdreAffichage) + 1;
        var ajoutes = 0;
        foreach (var item in aAjouter)
        {
            var cle = $"{item.Categorie.Trim()}|{item.Libelle.Trim()}";
            if (cles.Contains(cle))
                continue;
            await service.UpsertCritereAsync(
                posteId, null, item.Categorie, item.Libelle, (int)NiveauEvaluation.Moyen, prochain++);
            cles.Add(cle);
            ajoutes++;
        }

        Assert.Equal(2, ajoutes);
        var apres = await service.GetCriteresAsync(posteId);
        Assert.Equal(3, apres.Count);
        Assert.Equal(1, apres.Count(c => c.Libelle == premier.Libelle));
    }
}
