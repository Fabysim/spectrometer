using System.Data;
using System.Text.RegularExpressions;
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
/// marqué actif pour elle mais pas encore (ou pas entièrement) provisionné — remplace les scripts one-off
/// réappliquant manuellement le DDL à chaque nouveau module. Idempotent : un schéma déjà complet est
/// détecté table par table et laissé intact ; les tables ajoutées au modèle EF après le premier
/// provisionnement sont créées de façon différentielle (sans retoucher les tables déjà présentes).
/// </summary>
/// <remarks>
/// Coût au démarrage : O(entreprises × modules × tables), requêtes <c>information_schema</c> — négligeable
/// avec peu de tenants en développement. En production à grande échelle, une optimisation possible serait
/// de ne lancer cette vérification que lors d'un déploiement contenant un changement de schéma.
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
                var modelTables = GetModelTableNames(db);
                if (modelTables.Count == 0)
                    continue;

                var missing = await GetMissingTablesAsync(db, company.SchemaName, modelTables, cancellationToken);
                if (missing.Count == 0)
                    continue;

                if (missing.Count == modelTables.Count)
                {
                    // Module jamais provisionné : script CREATE complet (comportement historique).
                    await schemaProvisioner.ApplyModuleSchemaAsync(db, "public", company.SchemaName, cancellationToken);
                }
                else
                {
                    // Provisionnement partiel : seules les tables absentes (ex. GenerationsCriteresIaPoste
                    // ajoutée après coup) — les tables déjà là et leurs données restent intactes.
                    await schemaProvisioner.ApplyMissingTablesAsync(
                        db, "public", company.SchemaName, missing, cancellationToken);
                }
            }
        }
    }

    public static IReadOnlyList<string> GetModelTableNames(DbContext db) =>
        db.Model.GetEntityTypes()
            .Select(t => t.GetTableName())
            .Where(t => t is not null)
            .Distinct(StringComparer.Ordinal)
            .Cast<string>()
            .ToList();

    /// <summary>
    /// Tables du modèle EF absentes du schéma cible (<c>information_schema.tables</c>).
    /// </summary>
    public static async Task<IReadOnlyList<string>> GetMissingTablesAsync(
        DbContext db,
        string targetSchema,
        IReadOnlyList<string> modelTableNames,
        CancellationToken cancellationToken)
    {
        var missing = new List<string>();
        var connection = db.Database.GetDbConnection();
        var wasClosed = connection.State != ConnectionState.Open;
        if (wasClosed)
            await connection.OpenAsync(cancellationToken);
        try
        {
            foreach (var tableName in modelTableNames)
            {
                await using var command = connection.CreateCommand();
                command.CommandText =
                    "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = @schema AND table_name = @table)";

                var schemaParam = command.CreateParameter();
                schemaParam.ParameterName = "schema";
                schemaParam.Value = targetSchema;
                command.Parameters.Add(schemaParam);

                var tableParam = command.CreateParameter();
                tableParam.ParameterName = "table";
                tableParam.Value = tableName;
                command.Parameters.Add(tableParam);

                var result = await command.ExecuteScalarAsync(cancellationToken);
                var exists = result is bool b && b;
                if (!exists)
                    missing.Add(tableName);
            }

            return missing;
        }
        finally
        {
            if (wasClosed)
                await connection.CloseAsync();
        }
    }
}
