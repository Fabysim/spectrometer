using Spectrometre.Modules.ProfilEntreprise.Services;
using Spectrometre.Modules.ProfilEntreprise.Entities;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Recrutement.Entities;
using Spectrometre.Modules.Recrutement.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Génération IA des critères de poste : agrégation sans écrasement, idempotence via hash de contexte,
/// régénération forcée ou après changement titre/description. UI complète = navigateur (pas de
/// framework de rendu Blazor dans cette suite).
/// </summary>
[Collection("Base de données partagée")]
public sealed class GenererCriteresIaPosteTests(ServiceFixture fixture)
{
    private IServiceScope NewScope() => fixture.Services.CreateScope();

    [Fact]
    public async Task GenerationIaAjouteSansSupprimerLesCriteresManuels_IdempotencePuisForceEtChangementContexte()
    {
        var suffix = Guid.NewGuid();
        var ownerUserId = $"criteres-ia-owner-{suffix}";
        var company = await fixture.CreateCompanyAsync($"Entreprise Criteres IA {suffix}", ownerUserId);

        int posteId;
        using (var setupScope = NewScope())
        {
            setupScope.ServiceProvider.GetRequiredService<ITenantContext>()
                .SetActiveCompany(company.Id, company.SchemaName);
            var posteService = setupScope.ServiceProvider.GetRequiredService<IPosteService>();
            posteId = await posteService.CreatePosteAsync(
                $"Dev .NET {suffix}",
                "Backend C#",
                null,
                tachesDescription: "APIs, reviews");
            await posteService.UpsertCritereAsync(
                posteId, null, "Manuel", "Communication", (int)NiveauEvaluation.Moyen, 0);
        }

        using (var scope = NewScope())
        {
            scope.ServiceProvider.GetRequiredService<ITenantContext>()
                .SetActiveCompany(company.Id, company.SchemaName);
            var fake = (FakePosteCritereIaService)scope.ServiceProvider
                .GetRequiredService<IPosteCritereIaService>();
            fake.Suggestions.Clear();
            fake.Suggestions.Add(("Technique", "C#", 3));
            fake.Suggestions.Add(("Technique", "SQL", 2));
            fake.ResetAppels();

            var service = scope.ServiceProvider.GetRequiredService<IPosteService>();

            var avant = await service.GetCriteresAsync(posteId);
            Assert.Single(avant);
            Assert.Equal("Communication", avant[0].Libelle);

            var ajoutes = await service.GenererCriteresIaAsync(posteId);
            Assert.Equal(2, ajoutes);

            var apres = await service.GetCriteresAsync(posteId);
            Assert.Equal(3, apres.Count);
            Assert.Contains(apres, c => c.Categorie == "Manuel" && c.Libelle == "Communication");
            Assert.Contains(apres, c => c.Libelle == "C#");
            Assert.Contains(apres, c => c.Libelle == "SQL");

            var appelsApresPremier = fake.Appels;
            Assert.True(appelsApresPremier >= 1);

            var second = await service.GenererCriteresIaAsync(posteId);
            Assert.Equal(0, second);
            Assert.Equal(appelsApresPremier, fake.Appels); // pas de nouvel appel IA (hash inchangé)

            Assert.Equal(3, (await service.GetCriteresAsync(posteId)).Count);

            fake.Suggestions.Clear();
            fake.Suggestions.Add(("Soft", "Autonomie", 3));

            var force = await service.GenererCriteresIaAsync(posteId, forcerRegeneration: true);
            Assert.Equal(1, force);
            var apresForce = await service.GetCriteresAsync(posteId);
            Assert.Equal(4, apresForce.Count);
            Assert.Contains(apresForce, c => c.Libelle == "Autonomie");
            Assert.Contains(apresForce, c => c.Libelle == "Communication");

            await service.UpdatePosteAsync(
                posteId,
                $"Dev .NET senior {suffix}",
                "Backend C# et architecture",
                null,
                tachesDescription: "APIs, reviews");

            fake.Suggestions.Clear();
            fake.Suggestions.Add(("Architecture", "DDD", 4));

            var apresChangement = await service.GenererCriteresIaAsync(posteId, forcerRegeneration: false);
            Assert.Equal(1, apresChangement);
            var final = await service.GetCriteresAsync(posteId);
            Assert.Equal(5, final.Count);
            Assert.Contains(final, c => c.Libelle == "DDD");
            Assert.Contains(final, c => c.Libelle == "Communication");
        }
    }

    [Fact]
    public async Task EchecIaRetourneMoinsUn_SansSupprimerLesCriteresExistants()
    {
        var suffix = Guid.NewGuid();
        var ownerUserId = $"criteres-ia-fail-{suffix}";
        var company = await fixture.CreateCompanyAsync($"Entreprise Criteres IA Fail {suffix}", ownerUserId);

        using var scope = NewScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>()
            .SetActiveCompany(company.Id, company.SchemaName);
        var service = scope.ServiceProvider.GetRequiredService<IPosteService>();
        var fake = (FakePosteCritereIaService)scope.ServiceProvider
            .GetRequiredService<IPosteCritereIaService>();
        fake.Suggestions.Clear();

        var posteId = await service.CreatePosteAsync($"Poste fail {suffix}", "Desc", null);
        await service.UpsertCritereAsync(posteId, null, "Manuel", "Existant", (int)NiveauEvaluation.Fort, 1);

        var result = await service.GenererCriteresIaAsync(posteId);
        Assert.Equal(-1, result);
        Assert.Single(await service.GetCriteresAsync(posteId));
    }
}
