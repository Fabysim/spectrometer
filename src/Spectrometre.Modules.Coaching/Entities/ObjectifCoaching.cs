namespace Spectrometre.Modules.Coaching.Entities;

/// <summary>Objectif libre fixé par un coach pour une personne suivie, dans une <see cref="PeriodeObjectifsCoaching"/>.</summary>
public sealed class ObjectifCoaching
{
    public int Id { get; set; }

    public int PeriodeObjectifsCoachingId { get; set; }

    public PeriodeObjectifsCoaching? Periode { get; set; }

    public DateOnly Date { get; set; }

    public string Titre { get; set; } = "";

    public string? Moyens { get; set; }

    public AtteinteObjectifCoaching Atteinte { get; set; } = AtteinteObjectifCoaching.NonDefini;

    public string? Observation { get; set; }

    public int? Note { get; set; }
}
