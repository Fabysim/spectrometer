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

        // Exécuté via la connexion ADO.NET brute plutôt que Database.ExecuteSqlRawAsync : cette dernière
        // traite le texte SQL comme une chaîne de format composite (elle interprète tout "{...}" comme un
        // espace réservé de paramètre, même sans paramètre fourni) — or des données seed peuvent tout à
        // fait contenir des accolades littérales dans du texte (ex. les gabarits de questions du module
        // Entretien, qui utilisent la syntaxe "{tag}" pour leurs propres paramètres). GenerateCreateScript()
        // ne produit aucune valeur destinée à être formatée : on l'exécute donc tel quel, sans passer par
        // ce mécanisme de formatage inadapté ici.
        var connection = moduleDbContext.Database.GetDbConnection();
        var wasClosed = connection.State != System.Data.ConnectionState.Open;
        if (wasClosed)
            await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = scoped;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            if (wasClosed)
                await connection.CloseAsync();
        }
    }
}
