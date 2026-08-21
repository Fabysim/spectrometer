using Spectrometre.Modules.Missions.Entities;

namespace Spectrometre.Modules.Missions.Catalog;

/// <summary>
/// Correspondance entre les libellés de <c>p2.s12.missions_priorite</c>
/// (cases à cocher stockées dans <c>AutoObservationReponses.TextValue</c>, séparées par <c>|</c>)
/// et <see cref="MissionCategorie"/>.
/// Les libellés doivent rester alignés avec
/// <c>AutoObservationCatalogPart2.MissionsPrioriteOptions</c>.
/// </summary>
public static class MissionPreferenceCategorieMap
{
    public const string QuestionKey = "p2.s12.missions_priorite";

    /// <summary>
    /// Table retenue :
    /// <list type="bullet">
    /// <item><description>Jardinage → JardinageSimple (même type de tâche)</description></item>
    /// <item><description>Rangement → Rangement (identique)</description></item>
    /// <item><description>Nettoyage → NettoyageLeger (même type, intitulé catalogue plus précis)</description></item>
    /// <item><description>Aide aux courses → AideLogistique (courses = aide pratique de type logistique ; pas de catégorie « courses » dédiée)</description></item>
    /// <item><description>Montage simple → PetitBricolageNonDangereux (assemblage léger, plus proche du petit bricolage que d’une autre catégorie)</description></item>
    /// <item><description>Peinture simple → PetitBricolageNonDangereux (même rattachement : geste manuel simple, non dangereux)</description></item>
    /// <item><description>Déménagement léger → AideDemenagementLeger (identique)</description></item>
    /// <item><description>Lavage de voiture → LavageDeVoiture (identique)</description></item>
    /// <item><description>Autre → Autre (identique)</description></item>
    /// </list>
    /// Aucun libellé ignoré : chaque option du questionnaire a un équivalent raisonnable.
    /// Le champ libre <c>p2.s12.missions_priorite.autre</c> n’est pas utilisé (pas de catégorie déductible).
    /// Catégories Mission sans libellé auto-obs : TriClassementOrganisation, AccompagnementTachePratique, SoinsAuxAnimaux
    /// — elles ne sont suggérées que si le jeune a coché « Autre » (via Autre) ou une préférence qui mappe ailleurs.
    /// </summary>
    public static IReadOnlyDictionary<string, MissionCategorie> ParLibelle { get; } =
        new Dictionary<string, MissionCategorie>(StringComparer.Ordinal)
        {
            ["Jardinage"] = MissionCategorie.JardinageSimple,
            ["Rangement"] = MissionCategorie.Rangement,
            ["Nettoyage"] = MissionCategorie.NettoyageLeger,
            ["Aide aux courses"] = MissionCategorie.AideLogistique,
            ["Montage simple"] = MissionCategorie.PetitBricolageNonDangereux,
            ["Peinture simple"] = MissionCategorie.PetitBricolageNonDangereux,
            ["Déménagement léger"] = MissionCategorie.AideDemenagementLeger,
            ["Lavage de voiture"] = MissionCategorie.LavageDeVoiture,
            ["Autre"] = MissionCategorie.Autre,
        };

    public static IReadOnlySet<MissionCategorie> CategoriesDepuisTextValue(string? textValue)
    {
        if (string.IsNullOrWhiteSpace(textValue))
            return new HashSet<MissionCategorie>();

        var cats = new HashSet<MissionCategorie>();
        foreach (var raw in textValue.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (ParLibelle.TryGetValue(raw, out var cat))
                cats.Add(cat);
        }

        return cats;
    }
}
