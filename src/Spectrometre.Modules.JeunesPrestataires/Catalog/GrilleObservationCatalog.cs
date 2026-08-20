namespace Spectrometre.Modules.JeunesPrestataires.Catalog;

/// <summary>Définition d'un critère fixe de la grille d'observation socioprofessionnelle.</summary>
public sealed record GrilleObservationCritereDef(string Key);

/// <summary>
/// Catalogue fixe des critères (document Bouchra « Fonctionnalités côté jeune »).
/// Libellés et définitions pédagogiques via <c>GrilleObs_Critere_*</c> / <c>GrilleObs_Def_*</c> dans les resx.
/// </summary>
public static class GrilleObservationCatalog
{
    public static IReadOnlyList<GrilleObservationCritereDef> AllCriteres { get; } =
    [
        new("ponctualite"),
        new("comprehension_consignes"),
        new("autonomie"),
        new("communication"),
        new("soin_travail"),
        new("respect_cadre"),
        new("ordre"),
        new("proprete"),
        new("presentation_vestimentaire"),
        new("presentation_verbale"),
        new("sens_responsabilite"),
        new("sens_engagement"),
        new("initiative"),
        new("creativite"),
        new("debrouillardise"),
        new("respect"),
        new("honnetete"),
        new("integrite"),
        new("sens_justice"),
        new("rapidite_execution"),
        new("orientation_resultats"),
        new("perfectionnisme"),
        new("discretion"),
    ];

    private static readonly Dictionary<string, GrilleObservationCritereDef> ByKey =
        AllCriteres.ToDictionary(c => c.Key, StringComparer.Ordinal);

    public static GrilleObservationCritereDef? TryGet(string key) =>
        ByKey.TryGetValue(key, out var def) ? def : null;

    public static bool IsValidKey(string key) => ByKey.ContainsKey(key);
}
