namespace Spectrometre.Modules.Missions.Catalog;

/// <summary>Item fixe de la checklist « préparation avant mission » (libellés via <c>MissionsResource</c>).</summary>
public sealed record MissionPreparationItemDef(string Key);

/// <summary>
/// Catalogue fixe — document Bouchra « Ma préparation avant mission ».
/// Libellés : clés <c>Preparation_{Key}</c> dans les ressources du module.
/// </summary>
public static class MissionPreparationCatalog
{
    public static IReadOnlyList<MissionPreparationItemDef> Items { get; } =
    [
        new("tenue_adaptee"),
        new("horaire_verifie"),
        new("adresse_connue"),
        new("materiel_necessaire"),
        new("numero_contact"),
        new("consignes_relues"),
    ];

    private static readonly HashSet<string> KeySet =
        Items.Select(i => i.Key).ToHashSet(StringComparer.Ordinal);

    public static bool IsValidItemKey(string key) => KeySet.Contains(key);
}
