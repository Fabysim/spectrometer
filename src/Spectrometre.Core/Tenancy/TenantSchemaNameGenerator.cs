using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Spectrometre.Core.Tenancy;

public interface ITenantSchemaNameGenerator
{
    /// <summary>Construit un nom de schéma Postgres valide (préfixe <c>co_</c>) à partir du nom d'une entreprise.</summary>
    string GenerateSchemaName(string companyName);
}

/// <summary>Réécriture du générateur de slug de schéma de V1 (<c>TenantService.GenerateSchemaName</c>), sans dépendance à un DbContext précis.</summary>
public sealed partial class TenantSchemaNameGenerator : ITenantSchemaNameGenerator
{
    public string GenerateSchemaName(string companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            throw new ArgumentException("Nom d'entreprise requis.", nameof(companyName));

        var normalized = companyName.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var slug = NonAlphaNumeric().Replace(sb.ToString().ToLowerInvariant(), "_");
        slug = ConsecutiveUnderscores().Replace(slug, "_").Trim('_');
        slug = $"co_{slug}";

        if (slug.Length > 63)
            slug = slug[..63].TrimEnd('_');

        if (!ValidSchemaName().IsMatch(slug))
            throw new InvalidOperationException($"Nom de schéma invalide généré : {slug}");

        return slug;
    }

    [GeneratedRegex(@"[^a-z0-9]")]
    private static partial Regex NonAlphaNumeric();

    [GeneratedRegex(@"_+")]
    private static partial Regex ConsecutiveUnderscores();

    [GeneratedRegex(@"^[a-z][a-z0-9_]{0,62}$")]
    private static partial Regex ValidSchemaName();
}
