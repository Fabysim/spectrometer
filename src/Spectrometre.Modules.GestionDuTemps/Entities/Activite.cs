namespace Spectrometre.Modules.GestionDuTemps.Entities;

public static class ActiviteStatuts
{
    public const string AFaire = "AFaire";
    public const string Fait = "Fait";
}

/// <summary>
/// Occurrence datée d'un <see cref="TypeDeTemps"/> — le "rappel" concret (repris de <c>GdtActivite</c> de
/// <c>mvp</c>). Volontairement sans Kanban à 3 colonnes ni minuteur (voir <c>GdtKanbanStatut</c> dans
/// mvp) : un statut binaire À faire/Fait suffit pour le noyau de ce cycle.
/// </summary>
public sealed class Activite
{
    public int Id { get; set; }

    public required string UserId { get; set; }

    public int TypeDeTempsId { get; set; }

    public required string Nom { get; set; }

    public DateOnly DateActivite { get; set; }

    public TimeOnly HeureDebut { get; set; }

    public int DureeMinutes { get; set; }

    /// <summary>Voir <see cref="TypeDeTemps.CompanyId"/> — même sémantique, vérifié indépendamment (un rappel peut être rattaché à une entreprise différente de son type de temps).</summary>
    public int? CompanyId { get; set; }

    public string Statut { get; set; } = ActiviteStatuts.AFaire;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
