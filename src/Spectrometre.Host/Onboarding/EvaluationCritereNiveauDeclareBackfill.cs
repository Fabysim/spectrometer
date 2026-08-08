using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Data;
using Spectrometre.Modules.ProfilEntreprise.Data;

namespace Spectrometre.Host.Onboarding;

/// <summary>
/// Rétroactif : <c>NiveauDeclare</c> + nullabilité de <c>NiveauFinal</c> sur
/// <c>EvaluationsCriteresCandidature</c> pour chaque schéma tenant déjà provisionné.
/// <see cref="TenantSchemaSynchronizer"/> ne comble que les tables absentes.
/// </summary>
internal static partial class EvaluationCritereNiveauDeclareBackfill
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
                    "ALTER TABLE \"" + schema + "\".\"EvaluationsCriteresCandidature\" ADD COLUMN IF NOT EXISTS \"NiveauDeclare\" integer;",
                    cancellationToken);
                await db.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE \"" + schema + "\".\"EvaluationsCriteresCandidature\" ALTER COLUMN \"NiveauFinal\" DROP NOT NULL;",
                    cancellationToken);
            }
            catch (Npgsql.PostgresException)
            {
                // Table absente — SyncAll / provisionnement futur.
            }
        }
    }

    [GeneratedRegex("^[a-z][a-z0-9_]{0,62}$")]
    private static partial Regex ValidSchemaName();
}
