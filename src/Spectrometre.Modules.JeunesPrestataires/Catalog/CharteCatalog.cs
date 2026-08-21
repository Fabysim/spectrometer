namespace Spectrometre.Modules.JeunesPrestataires.Catalog;

/// <summary>Section fixe de la charte (libellés via <c>JeunesPrestatairesResource</c>).</summary>
public sealed record CharteSectionDef(string Key);

/// <summary>
/// Catalogue fixe — « Charte formelle des missions, des comportements et de l'accompagnement »
/// (document Bouchra, 13 sections). Textes : clés <c>Charte_S_{Key}_Titre</c> et
/// <c>Charte_S_{Key}_Corps</c>, plus le préambule <c>Charte_Intro</c> / <c>Charte_Intro2</c>.
/// Libellés repris du document source, sans paraphrase. Section 13 : confirmation par nom tapé
/// (jeune uniquement) — la ligne parent du document n'est pas un second circuit de consentement
/// (déjà couverte par <c>ConsentementParental</c>).
/// </summary>
public static class CharteCatalog
{
    public static IReadOnlyList<CharteSectionDef> Sections { get; } =
    [
        new("objet"),
        new("principes_generaux"),
        new("engagement_particulier"),
        new("ententes_hors"),
        new("responsabilite_coach"),
        new("missions_autorisees"),
        new("missions_interdites"),
        new("deroulement"),
        new("comportements"),
        new("confidentialite"),
        new("engagement_prestataire"),
        new("consequences"),
        new("formule_engagement"),
    ];
}
