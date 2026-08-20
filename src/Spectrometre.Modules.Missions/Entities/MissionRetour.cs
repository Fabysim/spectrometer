namespace Spectrometre.Modules.Missions.Entities;

/// <summary>
/// Retour après mission du jeune — document vivant (un seul enregistrement par acceptation).
/// </summary>
public sealed class MissionRetour
{
    public int Id { get; set; }

    public int MissionAcceptationId { get; set; }

    public MissionAcceptation MissionAcceptation { get; set; } = null!;

    public string? CeQuiSestBienPasse { get; set; }

    public string? CeQuiAEteDifficile { get; set; }

    public string? CeQueJaiAppris { get; set; }

    public string? CeQueJeVeuxAmeliorer { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
