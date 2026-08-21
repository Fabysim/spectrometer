using System.Text.Json;
using Spectrometre.Modules.JeunesPrestataires.Entities;

namespace Spectrometre.Modules.JeunesPrestataires.Services;

public sealed record AutoObservationSyntheseLigne(string Theme, string Contenu);

public sealed record AutoObservationSyntheseTableau(
    string Code,
    string Titre,
    IReadOnlyList<AutoObservationSyntheseLigne> Lignes);

/// <summary>
/// Synthèse structurée (tableaux Bouchra 1 + 2A/2B). Sérialisée en JSON dans
/// <c>AutoObservationSyntheseGeneree.Contenu</c> (<see cref="VersionCourante"/>).
/// </summary>
public sealed record AutoObservationSyntheseDocument(
    int Version,
    string Profil,
    AutoObservationSyntheseTableau TroncCommun,
    AutoObservationSyntheseTableau? Employabilite,
    AutoObservationSyntheseTableau? Orientation)
{
    public const int VersionCourante = 2;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    public string Serialiser() => JsonSerializer.Serialize(this, JsonOptions);

    public static bool TryParse(string? contenu, out AutoObservationSyntheseDocument document)
    {
        document = null!;
        if (string.IsNullOrWhiteSpace(contenu))
            return false;

        var trimmed = contenu.TrimStart();
        if (trimmed.Length == 0 || trimmed[0] != '{')
            return false;

        try
        {
            var parsed = JsonSerializer.Deserialize<AutoObservationSyntheseDocument>(contenu, JsonOptions);
            if (parsed is null || parsed.Version < VersionCourante || parsed.TroncCommun is null)
                return false;
            document = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>Extrait court pour « Mes progrès » — premier contenu utile du tronc commun.</summary>
    public string Resume(int maxLength = 220)
    {
        var texte = TroncCommun.Lignes
            .Select(l => l.Contenu)
            .FirstOrDefault(c => !string.IsNullOrWhiteSpace(c) && c != AutoObservationSyntheseGenerator.DonneesInsuffisantes)
            ?? TroncCommun.Titre;
        var trimmed = texte.Trim();
        if (trimmed.Length <= maxLength)
            return trimmed;
        return trimmed[..maxLength].TrimEnd() + "…";
    }

    public bool EstSansExperience =>
        string.Equals(Profil, nameof(ProfilAccompagnement.SansExperience), StringComparison.Ordinal);

    public bool EstAutonome =>
        string.Equals(Profil, nameof(ProfilAccompagnement.Autonome), StringComparison.Ordinal);
}
