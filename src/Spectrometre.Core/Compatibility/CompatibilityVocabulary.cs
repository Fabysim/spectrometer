namespace Spectrometre.Core.Compatibility;

/// <summary>
/// Vocabulaire partagé entre Profil Candidat et Profil Entreprise pour les critères de compatibilité
/// (grilles H et K du document source). Vit dans le noyau — pas dans un des deux modules — car les deux
/// doivent proposer EXACTEMENT les mêmes choix pour qu'une comparaison structurée ait un sens : si chacun
/// choisissait sa propre liste de tags, on retomberait dans le problème du texte libre non comparable
/// (voir l'audit qui a motivé ce changement).
/// </summary>
/// <remarks>
/// Listes volontairement modestes pour ce cycle, reprises du vocabulaire déjà présent dans le document
/// source (valeurs, motivations, signaux d'alerte). Un vrai catalogue de compétences techniques dépend
/// fortement du secteur d'activité : à terme, ce sera un référentiel administrable (sur le modèle du
/// catalogue ProfileItem/ProfileCategory de V1), pas une liste figée en code.
/// </remarks>
public static class CompatibilityVocabulary
{
    public static readonly IReadOnlyList<string> TechniqueTags =
    [
        "Informatique bureautique",
        "Gestion de projet",
        "Comptabilité",
        "Vente / négociation",
        "Mécanique / électricité",
        "Langues étrangères",
        "Service client",
        "Rédaction / communication",
        "Outils numériques avancés",
        "Gestion d'équipe",
    ];

    public static readonly IReadOnlyList<string> ComportementaleTags =
    [
        "Ponctualité",
        "Sens des responsabilités",
        "Rigueur",
        "Coopération",
        "Initiative",
        "Gestion des conflits",
        "Adaptabilité",
        "Communication",
    ];

    public static readonly IReadOnlyList<string> CulturelleTags =
    [
        "Respect",
        "Collaboration",
        "Excellence",
        "Autonomie",
        "Innovation",
        "Transparence",
        "Stabilité",
        "Esprit d'équipe",
    ];

    public static readonly IReadOnlyList<string> MotivationnelleTags =
    [
        "Reconnaissance",
        "Salaire",
        "Stabilité",
        "Progression",
        "Responsabilités",
        "Utilité sociale",
        "Apprentissage",
        "Esprit d'équipe",
        "Autonomie",
    ];

    public static readonly IReadOnlyList<string> PointsVigilanceTags =
    [
        "Bruit",
        "Conflits fréquents",
        "Horaires variables",
        "Pression commerciale",
        "Isolement",
        "Manque de reconnaissance",
        "Manque de moyens",
        "Consignes floues",
        "Mobilité fréquente",
        "Rythme intense",
    ];

    /// <summary>Échelle 1-5 pour l'axe organisationnel — rythme/pression du poste (candidat : toléré ; entreprise : exigé).</summary>
    public static readonly IReadOnlyDictionary<int, string> RythmeTravailLabels = new Dictionary<int, string>
    {
        [1] = "Calme et régulier",
        [2] = "Modéré",
        [3] = "Soutenu",
        [4] = "Intense",
        [5] = "Très intense / urgences fréquentes",
    };

    // Traductions anglaises (bilinguisme, cycle contenu métier) — affichage uniquement. Les tags sélectionnés
    // sont TOUJOURS stockés sous leur forme française (valeur canonique dans CompanyCompatibilityCriteria /
    // CandidateProfile), quelle que soit la culture d'affichage : traduire l'affichage n'a jamais d'incidence
    // sur la valeur persistée ni sur les comparaisons de compatibilité (qui comparent des chaînes françaises
    // des deux côtés). Traduction automatique pour l'instant, à affiner par une relecture humaine plus tard.
    private static readonly IReadOnlyList<string> TechniqueTagsEn =
    [
        "Office computing",
        "Project management",
        "Accounting",
        "Sales / negotiation",
        "Mechanics / electrical",
        "Foreign languages",
        "Customer service",
        "Writing / communication",
        "Advanced digital tools",
        "Team management",
    ];

    private static readonly IReadOnlyList<string> ComportementaleTagsEn =
    [
        "Punctuality",
        "Sense of responsibility",
        "Rigor",
        "Cooperation",
        "Initiative",
        "Conflict management",
        "Adaptability",
        "Communication",
    ];

    private static readonly IReadOnlyList<string> CulturelleTagsEn =
    [
        "Respect",
        "Collaboration",
        "Excellence",
        "Autonomy",
        "Innovation",
        "Transparency",
        "Stability",
        "Team spirit",
    ];

    private static readonly IReadOnlyList<string> MotivationnelleTagsEn =
    [
        "Recognition",
        "Salary",
        "Stability",
        "Career progression",
        "Responsibilities",
        "Social usefulness",
        "Learning",
        "Team spirit",
        "Autonomy",
    ];

    private static readonly IReadOnlyList<string> PointsVigilanceTagsEn =
    [
        "Noise",
        "Frequent conflicts",
        "Variable hours",
        "Sales pressure",
        "Isolation",
        "Lack of recognition",
        "Lack of resources",
        "Unclear instructions",
        "Frequent relocation",
        "Intense pace",
    ];

    private static readonly IReadOnlyDictionary<int, string> RythmeTravailLabelsEn = new Dictionary<int, string>
    {
        [1] = "Calm and steady",
        [2] = "Moderate",
        [3] = "Sustained",
        [4] = "Intense",
        [5] = "Very intense / frequent emergencies",
    };

    private static readonly IReadOnlyDictionary<string, string> EnglishByFrenchTag = BuildTranslationMap();

    private static IReadOnlyDictionary<string, string> BuildTranslationMap()
    {
        var map = new Dictionary<string, string>();
        AddPairs(map, TechniqueTags, TechniqueTagsEn);
        AddPairs(map, ComportementaleTags, ComportementaleTagsEn);
        AddPairs(map, CulturelleTags, CulturelleTagsEn);
        AddPairs(map, MotivationnelleTags, MotivationnelleTagsEn);
        AddPairs(map, PointsVigilanceTags, PointsVigilanceTagsEn);
        return map;

        static void AddPairs(Dictionary<string, string> map, IReadOnlyList<string> fr, IReadOnlyList<string> en)
        {
            for (var i = 0; i < fr.Count; i++)
                map[fr[i]] = en[i];
        }
    }

    /// <summary>
    /// Libellé d'affichage d'un tag selon la culture — jamais la valeur stockée (voir remarque ci-dessus).
    /// Retombe sur le tag français lui-même si <paramref name="tag"/> n'appartient à aucune liste connue
    /// (ex. donnée historique) ou si aucune traduction n'existe pour la culture demandée.
    /// </summary>
    public static string DisplayTag(string tag, bool english) =>
        english && EnglishByFrenchTag.TryGetValue(tag, out var translated) ? translated : tag;

    /// <summary>Libellé du niveau de rythme de travail (1-5) selon la culture — jamais la valeur numérique stockée.</summary>
    public static string DisplayRythmeTravail(int niveau, bool english) =>
        english && RythmeTravailLabelsEn.TryGetValue(niveau, out var translated)
            ? translated
            : RythmeTravailLabels.GetValueOrDefault(niveau, niveau.ToString());
}
