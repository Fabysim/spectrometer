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
}
