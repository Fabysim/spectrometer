namespace Spectrometre.Modules.GestionDuTemps.Entities;

/// <summary>
/// Occurrence datée d'un <see cref="TypeDeTemps"/> — le "rappel" concret (repris de <c>GdtActivite</c> de
/// <c>mvp</c>). Appartient à un <see cref="Cycle"/> — le même que son <see cref="TypeDeTemps"/> (vérifié à
/// l'écriture par <c>GestionDuTempsService</c>, comme dans mvp). Le statut d'avancement vit désormais
/// entièrement dans <see cref="KanbanStatut"/> (3 colonnes + minuteur, introduits dans ce cycle de travail) :
/// l'ancien champ <c>Statut</c> binaire (À faire/Fait) du premier cycle de travail a été retiré, remplacé
/// par un concept strictement plus riche — pas question de faire coexister les deux.
/// </summary>
public sealed class Activite
{
    public int Id { get; set; }

    public required string UserId { get; set; }

    public int CycleId { get; set; }

    public int TypeDeTempsId { get; set; }

    public required string Nom { get; set; }

    public DateOnly DateActivite { get; set; }

    public TimeOnly HeureDebut { get; set; }

    /// <summary>Durée planifiée, en minutes — c'est à cette valeur (pas à la plage horaire du <see cref="TypeDeTemps"/>) que le minuteur compare le temps réel, comme dans mvp (<c>GdtKanbanTimer.IsOvertime</c>).</summary>
    public int DureeMinutes { get; set; }

    /// <summary>Voir <see cref="TypeDeTemps.CompanyId"/> — même sémantique, vérifié indépendamment (un rappel peut être rattaché à une entreprise différente de son type de temps).</summary>
    public int? CompanyId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Évite les doublons du worker de notifications de début (Prompt E).</summary>
    public bool NotificationDebutEnvoyee { get; set; }

    /// <summary>Évite les doublons du worker de notifications de fin planifiée (Prompt E).</summary>
    public bool NotificationFinEnvoyee { get; set; }
}
