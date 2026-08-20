namespace Spectrometre.Modules.JeunesPrestataires.Entities;

public sealed class GrilleObservationCritere
{
    public int Id { get; set; }
    public int EvaluationId { get; set; }
    public GrilleObservationEvaluation Evaluation { get; set; } = null!;

    /// <summary>Clé stable du catalogue (<see cref="Catalog.GrilleObservationCatalog"/>).</summary>
    public required string CritereKey { get; set; }

    /// <summary>Score 1–5 ; null si non renseigné pour ce passage.</summary>
    public int? Score { get; set; }

    /// <summary>Commentaire par critère — visible du jeune.</summary>
    public string? Commentaire { get; set; }
}
