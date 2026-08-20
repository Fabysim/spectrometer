namespace Spectrometre.Modules.JeunesPrestataires.Catalog;

/// <summary>Ligne fixe de la grille d'identification des peurs (libellés via ressources).</summary>
public sealed record GuideEntrevuePeurDef(string Key);

/// <summary>
/// Catalogue fixe du guide d'entrevue — textes affichés en lecture seule via
/// <c>JeunesPrestatairesResource</c> (clés <c>GuideEntrevue_*</c>).
/// </summary>
public static class GuideEntrevueCatalog
{
    public static IReadOnlyList<GuideEntrevuePeurDef> Peurs { get; } =
    [
        new("peur_incapacite"),
        new("peur_erreur"),
        new("peur_deranger"),
        new("peur_jugement"),
        new("peur_experience_passee"),
    ];

    /// <summary>Clés des 9 questions de réflexion (texte de référence, non éditable).</summary>
    public static IReadOnlyList<string> QuestionKeys { get; } =
    [
        "q1", "q2", "q3", "q4", "q5", "q6", "q7", "q8", "q9",
    ];

    private static readonly HashSet<string> PeurKeySet = Peurs.Select(p => p.Key).ToHashSet(StringComparer.Ordinal);

    public static bool IsValidPeurKey(string key) => PeurKeySet.Contains(key);
}
