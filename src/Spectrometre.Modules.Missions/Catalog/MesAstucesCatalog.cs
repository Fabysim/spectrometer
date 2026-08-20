namespace Spectrometre.Modules.Missions.Catalog;

/// <summary>Fiche statique « Mes astuces » (libellés via <c>MissionsResource</c>).</summary>
public sealed record MesAstucesFicheDef(string Key);

/// <summary>
/// Catalogue fixe — document Bouchra « Fonctionnalités côté jeune » (titres) + quelques fiches utiles.
/// Textes : clés <c>MesAstuces_Fiche_{Key}_Titre</c> et <c>MesAstuces_Fiche_{Key}_Texte</c>.
/// </summary>
public static class MesAstucesCatalog
{
    public static IReadOnlyList<MesAstucesFicheDef> Fiches { get; } =
    [
        new("arriver_a_lheure"),
        new("dire_bonjour"),
        new("se_presenter"),
        new("demander_aide"),
        new("prevenir_probleme"),
        new("en_retard"),
        new("finir_mission"),
    ];
}
