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

    /// <summary>
    /// Exécute uniquement le DDL (CREATE TABLE / INDEX / ALTER) concernant les
    /// <paramref name="missingTableNames"/> — pour combler un module déjà partiellement provisionné
    /// sans retomber sur « relation already exists ».
    /// </summary>
    Task ApplyMissingTablesAsync(
        DbContext moduleDbContext,
        string templateSchemaName,
        string targetSchemaName,
        IReadOnlyList<string> missingTableNames,
        CancellationToken cancellationToken = default);
}

public sealed partial class TenantSchemaProvisioner : ITenantSchemaProvisioner
{
    public async Task ApplyModuleSchemaAsync(DbContext moduleDbContext, string templateSchemaName, string targetSchemaName, CancellationToken cancellationToken = default)
    {
        var scoped = BuildScopedCreateScript(moduleDbContext, templateSchemaName, targetSchemaName);
        await ExecuteSqlBatchAsync(moduleDbContext, scoped, cancellationToken);
    }

    public async Task ApplyMissingTablesAsync(
        DbContext moduleDbContext,
        string templateSchemaName,
        string targetSchemaName,
        IReadOnlyList<string> missingTableNames,
        CancellationToken cancellationToken = default)
    {
        if (missingTableNames.Count == 0)
            return;

        var scoped = BuildScopedCreateScript(moduleDbContext, templateSchemaName, targetSchemaName);
        var missing = new HashSet<string>(missingTableNames, StringComparer.Ordinal);

        // GenerateCreateScript (Npgsql) sépare les instructions par « ;\n\n » — pas de ';' interne dans
        // les CREATE TABLE / INDEX produits pour ce projet. On filtre ensuite aux seules instructions
        // qui référencent une table manquante (identifiant entre guillemets doubles).
        var statements = SplitSqlStatements(scoped)
            .Where(s => missing.Any(t => StatementReferencesTable(s, t)))
            .ToList();

        if (statements.Count == 0)
            return;

        await ExecuteSqlBatchAsync(moduleDbContext, string.Join(";\n\n", statements) + ";\n", cancellationToken);
    }

    private static string BuildScopedCreateScript(DbContext moduleDbContext, string templateSchemaName, string targetSchemaName)
    {
        var script = moduleDbContext.Database.GenerateCreateScript();

        // Le SQL généré par Npgsql qualifie chaque objet par "schema."TableName"" — schéma NON guillemeté
        // (ex. public."CompanyProfiles"), y compris à l'intérieur des appels à pg_get_serial_sequence.
        // \b évite de toucher le mot "public" ailleurs qu'en préfixe de nom qualifié.
        var schemaPrefix = new Regex($@"\b{Regex.Escape(templateSchemaName)}\.(?="")");
        return schemaPrefix.Replace(script, $"{targetSchemaName}.");
    }

    /// <summary>
    /// Découpe le script EF/Npgsql en instructions. Les CREATE TABLE multi-lignes se terminent par « ; »
    /// suivi d'une ligne vide — aucun point-virgule interne dans le DDL généré pour nos modules.
    /// </summary>
    internal static IReadOnlyList<string> SplitSqlStatements(string script)
    {
        var parts = Regex.Split(script, @";\s*\r?\n")
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .ToList();
        return parts;
    }

    internal static bool StatementReferencesTable(string statement, string tableName) =>
        statement.Contains($"\"{tableName}\"", StringComparison.Ordinal);

    private static async Task ExecuteSqlBatchAsync(DbContext moduleDbContext, string sql, CancellationToken cancellationToken)
    {
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
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            if (wasClosed)
                await connection.CloseAsync();
        }
    }
}
