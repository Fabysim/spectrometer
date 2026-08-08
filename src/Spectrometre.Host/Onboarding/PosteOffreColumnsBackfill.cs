using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Data;
using Spectrometre.Modules.ProfilEntreprise.Data;

namespace Spectrometre.Host.Onboarding;

/// <summary>
/// Rétroactif : ajoute <c>OffreTexte</c> / <c>OffreGenereeLe</c> / <c>OffreGenereeParIa</c> sur
/// <c>Postes</c> pour chaque schéma tenant déjà provisionné. <see cref="TenantSchemaSynchronizer"/>
/// ne comble que les tables absentes — pas les colonnes ajoutées à une table existante.
/// Les nouveaux tenants obtiennent ces colonnes via <c>GenerateCreateScript</c> (modèle à jour).
/// Idempotent (<c>ADD COLUMN IF NOT EXISTS</c>).
/// </summary>
internal static partial class PosteOffreColumnsBackfill
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var coreDb = services.GetRequiredService<CoreDbContext>();
        var dbFactory = services.GetRequiredService<IDbContextFactory<ProfilEntrepriseDbContext>>();

        var companies = await coreDb.Companies.AsNoTracking().ToListAsync(cancellationToken);

        foreach (var company in companies)
        {
            if (!ValidSchemaName().IsMatch(company.SchemaName))
                throw new InvalidOperationException("Schéma invalide.");

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            db.TenantSchema = company.SchemaName;

            try
            {
                var schema = company.SchemaName;
                await db.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE \"" + schema + "\".\"Postes\" ADD COLUMN IF NOT EXISTS \"OffreTexte\" text;",
                    cancellationToken);
                await db.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE \"" + schema + "\".\"Postes\" ADD COLUMN IF NOT EXISTS \"OffreGenereeLe\" timestamp with time zone;",
                    cancellationToken);
                await db.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE \"" + schema + "\".\"Postes\" ADD COLUMN IF NOT EXISTS \"OffreGenereeParIa\" boolean NOT NULL DEFAULT false;",
                    cancellationToken);
            }
            catch (Npgsql.PostgresException)
            {
                // Table Postes absente (module pas encore provisionné) — SyncAll / provisionnement futur.
            }
        }
    }

    [GeneratedRegex("^[a-z][a-z0-9_]{0,62}$")]
    private static partial Regex ValidSchemaName();
}
