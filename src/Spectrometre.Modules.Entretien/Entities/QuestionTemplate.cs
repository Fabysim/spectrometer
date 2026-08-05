using Spectrometre.Modules.Compatibilite.Entities;

namespace Spectrometre.Modules.Entretien.Entities;

/// <summary>Ce que cible un gabarit : un axe de compatibilité sous le seuil, ou un point de vigilance partagé (générique, pas lié à un axe précis).</summary>
public enum QuestionTemplateType
{
    Axe = 0,
    PointVigilance = 1,
}

/// <summary>Sens de la question — voir le document de cadrage : les deux sens sont à produire systématiquement.</summary>
public enum QuestionSens
{
    /// <summary>Question que l'ENTREPRISE peut poser AU CANDIDAT pour éclaircir l'écart.</summary>
    EntrepriseVersCandidat = 0,

    /// <summary>Question que le CANDIDAT peut poser À L'ENTREPRISE pour éclaircir ce même écart, de son point de vue.</summary>
    CandidatVersEntreprise = 1,
}

/// <summary>
/// Gabarit de question d'entretien — DONNÉE, pas une chaîne codée en dur dans le service ou l'UI. Le
/// partenaire psychologue du client doit pouvoir un jour affiner le phrasing sans toucher au code : pour
/// l'instant, cela se fait par modification directe des lignes en base (table <c>QuestionTemplates</c>,
/// schéma de chaque entreprise) — une future itération pourrait exposer un écran d'administration dessus,
/// mais la donnée est déjà structurée pour le permettre (pas de logique de génération encodée ailleurs
/// que dans la substitution des paramètres, voir <c>EntretienService</c>).
/// </summary>
/// <remarks>
/// <see cref="Gabarit"/> contient du texte libre avec des paramètres <c>{nom}</c> substitués à la
/// génération : <c>{axe}</c> et <c>{score}</c> (gabarits de type <see cref="QuestionTemplateType.Axe"/>),
/// <c>{rythmeCandidat}</c>/<c>{rythmeEntreprise}</c> (uniquement pour l'axe <see cref="CompatibilityAxis.Organisationnelle"/>),
/// et <c>{tag}</c> (gabarits de type <see cref="QuestionTemplateType.PointVigilance"/>).
/// </remarks>
public sealed class QuestionTemplate
{
    public int Id { get; set; }
    public QuestionTemplateType Type { get; set; }

    /// <summary>Renseigné uniquement pour <see cref="QuestionTemplateType.Axe"/> — un gabarit de vigilance est générique, appliqué à n'importe quel tag détecté.</summary>
    public CompatibilityAxis? Axis { get; set; }

    public QuestionSens Sens { get; set; }
    public required string Gabarit { get; set; }

    /// <summary>Ordre d'affichage relatif entre gabarits d'un même (Type, Axis, Sens) — permet d'avoir 1 à 2 questions par axe/sens sans ambiguïté d'ordre.</summary>
    public int DisplayOrder { get; set; }
}
