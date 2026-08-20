using Spectrometre.Modules.Missions.Entities;

namespace Spectrometre.Modules.Missions.Catalog;

/// <summary>Exemples concrets affichés à côté de chaque catégorie (aide UI, pas un champ stocké).</summary>
public static class MissionCategorieCatalog
{
    public static string ExempleCle(MissionCategorie categorie) => $"CategorieExemple_{categorie}";
}
