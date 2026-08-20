namespace Spectrometre.Modules.Missions.Entities;

/// <summary>
/// Case cochée de la checklist « préparation avant mission » — une ligne par
/// (<see cref="MissionAcceptationId"/>, <see cref="ItemKey"/> catalogue fixe).
/// </summary>
public sealed class MissionPreparationCoche
{
    public int Id { get; set; }

    public int MissionAcceptationId { get; set; }

    public MissionAcceptation MissionAcceptation { get; set; } = null!;

    /// <summary>Clé catalogue (<c>tenue_adaptee</c>, etc.).</summary>
    public required string ItemKey { get; set; }

    public bool Coche { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
