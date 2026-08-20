namespace Spectrometre.Modules.Missions.Entities;

/// <summary>
/// Évaluation constructive après mission — côté particulier uniquement.
/// Pas de note globale, pas de classement, pas de commentaire public, pas de jugement de personnalité.
/// </summary>
public sealed class MissionEvaluationParticulier
{
    public int Id { get; set; }

    public int MissionAcceptationId { get; set; }

    public MissionAcceptation MissionAcceptation { get; set; } = null!;

    public bool? Ponctualite { get; set; }

    public bool? ConsignesComprises { get; set; }

    public bool? TacheRealiseeCorrectement { get; set; }

    public bool? AttitudeRespectueuse { get; set; }

    public string? PointsPositifs { get; set; }

    public string? PointsAAmeliorer { get; set; }

    public bool? AccepteraitNouvelleMission { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
