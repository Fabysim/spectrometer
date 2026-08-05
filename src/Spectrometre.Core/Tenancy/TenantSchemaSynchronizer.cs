using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Data;
using Spectrometre.Core.Modules;

namespace Spectrometre.Core.Tenancy;

/// <summary>
/// Un module tenant-scopé : son code de manifeste, et comment obtenir un <see cref="DbContext"/> frais
/// pour lui (typiquement via <c>IDbContextFactory&lt;TContext&gt;.CreateDbContextAsync</c>). Vit dans le
/// noyau car <see cref="TenantSchemaSynchronizer"/> (qui l'utilise) doit être appelable aussi bien depuis
/// <c>Spectrometre.Host</c> (au démarrage réel) que depuis les tests (sans dupliquer l'algorithme) — mais
/// c'est toujours <c>Spectrometre.Host</c> qui construit la LISTE de ces modules (lui seul connaît les
/// types de DbContext concrets de chaque module).
/// </summary>
public sealed record TenantSchemaModule(string ModuleCode, Func<IServiceProvider, CancellationToken, Task<DbContext>> CreateDbContextAsync);

/// <summary>
/// Comble rétroactivement, pour chaque entreprise existante, le schéma de chaque module tenant-scopé
/// marqué actif pour elle mais pas encore provisionné — remplace les scripts one-off réappliquant
/// manuellement le DDL à chaque nouveau module (dette identifiée sur <c>PosteIndexEntries</c> puis
/// <c>Entretien</c>). Idempotent : un schéma déjà provisionné est détecté et laissé intact.
/// </summary>
/// <remarks>
/// Coût au démarrage : O(entreprises × modules tenant-scopés), une requête <c>information_schema</c> par
/// paire — négligeable avec le nombre d'entreprises en jeu pendant les tests/le développement. Avec
/// beaucoup de tenants en production, ce coût grossirait linéairement à CHAQUE démarrage du Host (pas
/// seulement quand un module est réellement ajouté) : une optimisation possible serait de ne lancer cette
/// vérification que lors d'un déploiement contenant un changement de schéma (ex. un indicateur de version
/// de schéma comparé), ou de la déplacer dans un outil d'administration exécuté à la demande plutôt qu'à
/// chaque démarrage. Hors scope pour ce cycle (peu de tenants en test) — signalé ici pour la suite.
/// </remarks>
public static class TenantSchemaSynchronizer
{
    public static async Task SyncAllAsync(IServiceProvider services, IReadOnlyList<TenantSchemaModule> modules, CancellationToken cancellationToken = default)
    {
        var coreDb = services.GetRequiredService<CoreDbContext>();
        var moduleRegistry = services.GetRequiredService<IModuleRegistry>();
        var schemaProvisioner = services.GetRequiredService<ITenantSchemaProvisioner>();

        var companies = await coreDb.Companies.AsNoTracking().ToListAsync(cancellationToken);

        foreach (var company in companies)
        {
            var activeCodes = await moduleRegistry.GetActiveModuleCodesAsync(company.Id, coreDb, cancellationToken);

            foreach (var module in modules)
            {
                if (!activeCodes.Contains(module.ModuleCode))
                    continue;

                await using var db = await module.CreateDbContextAsync(services, cancellationToken);
                if (await HasSchemaAsync(db, company.SchemaName, cancellationToken))
                    continue;

                await schemaProvisioner.ApplyModuleSchemaAsync(db, "public", company.SchemaName, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Vérifie l'existence d'UNE table attendue (la première du modèle EF de ce DbContext) dans le schéma
    /// cible, via <c>information_schema.tables</c> — générique, sans connaître les noms de table de chaque
    /// module à l'avance. Suffisant en pratique : <see cref="ITenantSchemaProvisioner.ApplyModuleSchemaAsync"/>
    /// provisionne toujours TOUTES les tables d'un module en une seule opération, donc la présence de la
    /// première implique celle des autres.
    /// </summary>
    private static async Task<bool> HasSchemaAsync(DbContext db, string targetSchema, CancellationToken cancellationToken)
    {
        var firstTable = db.Model.GetEntityTypes().Select(t => t.GetTableName()).FirstOrDefault(t => t is not null);
        if (firstTable is null)
            return true; // Aucune table dans ce modèle : rien à provisionner, considéré à jour.

        var connection = db.Database.GetDbConnection();
        var wasClosed = connection.State != ConnectionState.Open;
        if (wasClosed)
            await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = @schema AND table_name = @table)";

            var schemaParam = command.CreateParameter();
            schemaParam.ParameterName = "schema";
            schemaParam.Value = targetSchema;
            command.Parameters.Add(schemaParam);

            var tableParam = command.CreateParameter();
            tableParam.ParameterName = "table";
            tableParam.Value = firstTable;
            command.Parameters.Add(tableParam);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is bool exists && exists;
        }
        finally
        {
            if (wasClosed)
                await connection.CloseAsync();
        }
    }
}
