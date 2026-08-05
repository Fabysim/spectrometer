using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Entretien.Data;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Reproduit la dette identifiée sur trois cycles consécutifs (<c>PosteIndexEntries</c>, puis
/// <c>Entretien</c>) : une entreprise déjà existante dont un module est marqué actif mais dont le schéma
/// n'a jamais été provisionné (ex. le module a été ajouté après la création de cette entreprise). Vérifie
/// que <see cref="TenantSchemaSynchronizer"/> comble ce trou, et que rejouer la synchronisation sur une
/// entreprise déjà à jour ne casse rien (idempotence — une ré-application du DDL sur des tables déjà
/// existantes lèverait une exception si la détection était défaillante).
/// </summary>
[Collection("Base de données partagée")]
public sealed class TenantSchemaSynchronizerTests(ServiceFixture fixture)
{
    [Fact]
    public async Task EntrepriseEnRetard_EstCompleteeSansAffecterUneEntrepriseAJour()
    {
        var suffix = Guid.NewGuid();

        // "À jour" : toutes les entreprises tenant-scopées ont déjà leur schéma (comme après un onboarding normal).
        var entrepriseAJour = await fixture.CreateCompanyAsync($"Entreprise Sync AJour {suffix}", $"sync-test-manager-ajour-{suffix}");

        // "En retard" : le module Entretien est actif (comme si le registre le disait), mais son schéma
        // n'a jamais été appliqué à ce tenant — exactement le scénario reproché aux cycles précédents.
        var entrepriseEnRetard = await fixture.CreateCompanyAsync(
            $"Entreprise Sync EnRetard {suffix}", $"sync-test-manager-retard-{suffix}",
            skipSchemaForModuleCodes: [Spectrometre.Modules.Entretien.ServiceCollectionExtensions.Manifest.Code]);

        // Avant : le schéma "en retard" n'a pas les tables Entretien ; celui "à jour" les a déjà.
        Assert.False(await HasQuestionTemplatesTableAsync(entrepriseEnRetard.SchemaName));
        Assert.True(await HasQuestionTemplatesTableAsync(entrepriseAJour.SchemaName));

        // Rejoue exactement la même synchronisation que celle exécutée au démarrage du Host.
        await TenantSchemaSynchronizer.SyncAllAsync(fixture.Services, ServiceFixture.TenantModules);

        // Après : le retard est comblé...
        Assert.True(await HasQuestionTemplatesTableAsync(entrepriseEnRetard.SchemaName));
        // ... et le seed de gabarits de questions est bien présent (pas juste la table vide).
        Assert.True(await CountQuestionTemplatesAsync(entrepriseEnRetard.SchemaName) > 0);

        // L'entreprise déjà à jour n'a pas été perturbée : si la détection d'idempotence avait échoué,
        // SyncAllAsync aurait tenté de recréer des tables déjà existantes et aurait levé une exception
        // avant même d'atteindre les assertions ci-dessus (schémas traités dans l'ordre de la boucle).
        Assert.True(await HasQuestionTemplatesTableAsync(entrepriseAJour.SchemaName));
    }

    private async Task<bool> HasQuestionTemplatesTableAsync(string schema)
    {
        await using var db = await fixture.Services.GetRequiredService<IDbContextFactory<EntretienDbContext>>().CreateDbContextAsync();
        var connection = db.Database.GetDbConnection();
        var wasClosed = connection.State != System.Data.ConnectionState.Open;
        if (wasClosed) await connection.OpenAsync();
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = @schema AND table_name = 'QuestionTemplates')";
            var p = cmd.CreateParameter();
            p.ParameterName = "schema";
            p.Value = schema;
            cmd.Parameters.Add(p);
            var result = await cmd.ExecuteScalarAsync();
            return result is bool b && b;
        }
        finally
        {
            if (wasClosed) await connection.CloseAsync();
        }
    }

    private async Task<int> CountQuestionTemplatesAsync(string schema)
    {
        await using var db = await fixture.Services.GetRequiredService<IDbContextFactory<EntretienDbContext>>().CreateDbContextAsync();
        db.TenantSchema = schema;
        return await db.QuestionTemplates.CountAsync();
    }
}
