namespace Spectrometre.Modules.JeunesPrestataires.Entities;

/// <summary>
/// Une évaluation datée de la grille d'observation socioprofessionnelle — historique conservé
/// (contrairement à l'auto-observation qui écrase l'état courant).
/// </summary>
public sealed class GrilleObservationEvaluation
{
    public int Id { get; set; }
    public int JeuneProfileId { get; set; }
    public required string CoachUserId { get; set; }
    public DateTimeOffset EvalueeLe { get; set; }
    /// <summary>Notes d'accompagnement confidentielles — jamais exposées au jeune (filtrage service).</summary>
    public string? CommentaireGeneral { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<GrilleObservationCritere> Criteres { get; set; } = [];
}
