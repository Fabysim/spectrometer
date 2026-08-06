namespace Spectrometre.Modules.GestionDuTemps.Entities;

/// <summary>
/// Traduction d'affichage des 6 catégories de temps par défaut (bilinguisme, cycle contenu métier) —
/// résolue par <see cref="TypeDeTemps.Cle"/>, jamais un champ persisté séparé sur <see cref="TypeDeTemps"/> :
/// <see cref="TypeDeTemps.Libelle"/> reste librement éditable par l'utilisateur (voir son commentaire), donc
/// la seule façon de rester honnête envers une catégorie renommée ou ajoutée par l'utilisateur est de ne
/// traduire que les clés connues des 6 gabarits de départ et de retomber sur le libellé tel quel sinon —
/// même principe que <c>CouleurCategorie</c> (clé, jamais le libellé). Traduction automatique pour l'instant,
/// à affiner plus tard.
/// </summary>
public static class DefaultCategoryLabels
{
    private static readonly IReadOnlyDictionary<string, string> LabelsEn = new Dictionary<string, string>
    {
        ["sommeil"] = "Sleep · Rest",
        ["perso"] = "Personal",
        ["pro"] = "Professional",
        ["admin"] = "Administrative",
        ["famille"] = "Family · Relationships",
        ["social"] = "Social",
    };

    /// <summary>Libellé d'affichage selon la culture — retombe sur <paramref name="libelle"/> tel quel si <paramref name="cle"/> n'est pas l'une des 6 clés par défaut (catégorie personnalisée) ou si la culture est française.</summary>
    public static string Display(string cle, string libelle, bool english) =>
        english && LabelsEn.TryGetValue(cle, out var translated) ? translated : libelle;
}
