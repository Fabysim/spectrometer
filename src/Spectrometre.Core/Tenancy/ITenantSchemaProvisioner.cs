using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace Spectrometre.Core.Tenancy;

/// <summary>
/// Applique le schéma d'un module (tables, index) à un nouveau schéma tenant.
/// </summary>
/// <remarks>
/// Limite connue : les migrations EF Core d'un DbContext tenant-scopé figent le nom de schéma
/// utilisé au moment du <c>dotnet ef migrations add</c> (ici <c>"public"</c>, valeur par défaut de
/// <see cref="TenantContext"/> hors requête). <c>Database.Migrate()</c> ne peut donc pas être rejoué
/// tel quel pour un schéma différent. En attendant une solution multi-tenant EF Core plus aboutie
/// (ou des scripts DDL bruts comme en V1), ce provisioner récupère le script de création généré pour
/// le schéma « gabarit » et substitue le nom de schéma cible avant exécution — pragmatique, à revoir
/// si le nombre de tenants ou la fréquence des migrations de schéma augmente significativement.
/// </remarks>
public interface ITenantSchemaProvisioner
{
    Task ApplyModuleSchemaAsync(DbContext moduleDbContext, string templateSchemaName, string targetSchemaName, CancellationToken cancellationToken = default);
}

public sealed partial class TenantSchemaProvisioner : ITenantSchemaProvisioner
{
    public async Task ApplyModuleSchemaAsync(DbContext moduleDbContext, string templateSchemaName, string targetSchemaName, CancellationToken cancellationToken = default)
    {
        var script = moduleDbContext.Database.GenerateCreateScript();

        // Le SQL généré par Npgsql qualifie chaque objet par "schema."TableName"" — schéma NON guillemeté
        // (ex. public."CompanyProfiles"), y compris à l'intérieur des appels à pg_get_serial_sequence.
        // \b évite de toucher le mot "public" ailleurs qu'en préfixe de nom qualifié.
        var schemaPrefix = new Regex($@"\b{Regex.Escape(templateSchemaName)}\.(?="")");
        var scoped = schemaPrefix.Replace(script, $"{targetSchemaName}.");

        await moduleDbContext.Database.ExecuteSqlRawAsync(scoped, cancellationToken);
    }
}
