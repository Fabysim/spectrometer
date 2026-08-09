namespace Spectrometre.Modules.SuiviEmployes.Entities;

/// <summary>Période d'évaluation d'objectifs (équivalent mvp <c>ObjectivesEvaluation</c>).</summary>
public sealed class EvaluationObjectifs
{
    public int Id { get; set; }

    public int UserCompanyLinkId { get; set; }

    public DateOnly DateDebut { get; set; }

    public DateOnly DateFin { get; set; }

    public bool Archivee { get; set; }

    public string? EvaluateurUserId { get; set; }

    /// <summary>
    /// Indicateur simplifié (pas d'infra Notification dans le modulaire) :
    /// 3 notes consécutives sous le seuil critique.
    /// </summary>
    public bool SeuilCritiqueAtteint { get; set; }

    public ICollection<Objectif> Objectifs { get; set; } = [];
}
