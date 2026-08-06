namespace Spectrometre.Modules.GestionDuTemps.Entities;

/// <summary>
/// Questionnaire psychosocial d'un cycle — repris de <c>GdtProfilPsychosocial</c> (mvp), champ pour champ.
/// Un par <see cref="Cycle"/> (pas global à l'utilisateur) : chaque saison a son propre instantané, cohérent
/// avec le concept de cycle déjà en place (on peut le reprendre à zéro à chaque nouveau cycle, comme les
/// catégories de temps le sont déjà via la recopie à la clôture — voir <see cref="Cycle"/>).
/// </summary>
/// <remarks>
/// Aplati par rapport à mvp : les 5 tables enfants (Tendances/Satisfactions/Desequilibres/Emotions/
/// Objectifs) deviennent des colonnes directes sur cette table — des tableaux Postgres natifs
/// (<c>List&lt;string&gt;</c>, même mécanisme que <c>CandidatureIndexEntry.TagsCles</c> dans le noyau) pour
/// les listes de tags libres, et des colonnes nommées (une par catégorie de temps fixe : sommeil, perso,
/// pro, admin, famille, social) plutôt qu'un dictionnaire pour la satisfaction/direction souhaitée — même
/// contenu que mvp, un schéma nettement plus simple.
/// </remarks>
public sealed class ProfilPsychosocial
{
    public int Id { get; set; }
    public int CycleId { get; set; }
    public required string UserId { get; set; }

    // Sommeil
    public TimeOnly? SommeilCoucher { get; set; }
    public TimeOnly? SommeilLever { get; set; }
    public string? SommeilReparateur { get; set; }
    public bool? ReveilsNocturnes { get; set; }
    public bool? EcransAvantSommeil { get; set; }

    // Temps personnel
    public bool? TempsPersoQuotidien { get; set; }
    public string? ManqueTempsPerso { get; set; }
    public string? ActivitesRessourcantes { get; set; }

    // Travail
    public string? HoraireTravail { get; set; }
    public bool? SurchargePrevisible { get; set; }
    public string? TravailHorsHeures { get; set; }

    // Contraintes
    public string? DureeTrajets { get; set; }
    public bool? DeplacementsImprevus { get; set; }
    public bool? EngagementCommunautaire { get; set; }
    public string? EngagementNature { get; set; }
    public bool? ProcheMalade { get; set; }

    // Mode de travail
    public string? ModeTravail { get; set; }
    public string? InterruptionsTravail { get; set; }

    /// <summary>Échelle 1-5.</summary>
    public int? AutonomieGestion { get; set; }

    // État psychosocial
    public string? SentimentPression { get; set; }
    public string? DifficultesConcentration { get; set; }
    public string? DecisionsPrecipitees { get; set; }
    public string? Culpabilite { get; set; }

    /// <summary>Anxieux|Adaptatif|Souple — voir <c>SyntheseCalculator</c>.</summary>
    public string? ToleranceImprevu { get; set; }

    // Organisation
    public bool? UtiliseAgenda { get; set; }
    public string? PlanificationAvance { get; set; }
    public bool? RituelsQuotidiens { get; set; }
    public string? OuiTropFacile { get; set; }
    public string? CapaciteNon { get; set; }
    public string? GestionAdmin { get; set; }
    public string? StressAdmin { get; set; }

    // Tags libres
    public List<string> Tendances { get; set; } = [];
    public List<string> Desequilibres { get; set; } = [];
    public List<string> EmotionsNegatives { get; set; } = [];
    public List<string> EmotionsPositives { get; set; } = [];
    public List<string> ObjectifsProfessionnels { get; set; } = [];
    public List<string> Obstacles { get; set; } = [];

    // Satisfaction par catégorie de temps (0 à 4)
    public int? SatisfactionSommeil { get; set; }
    public int? SatisfactionPerso { get; set; }
    public int? SatisfactionPro { get; set; }
    public int? SatisfactionAdmin { get; set; }
    public int? SatisfactionFamille { get; set; }
    public int? SatisfactionSocial { get; set; }

    // Direction souhaitée par catégorie de temps (texte libre, ex. "Plus"/"Moins"/"Stable")
    public string? DirectionSommeil { get; set; }
    public string? DirectionPerso { get; set; }
    public string? DirectionPro { get; set; }
    public string? DirectionAdmin { get; set; }
    public string? DirectionFamille { get; set; }
    public string? DirectionSocial { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
