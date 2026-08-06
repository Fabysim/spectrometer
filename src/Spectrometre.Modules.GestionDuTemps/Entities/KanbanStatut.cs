namespace Spectrometre.Modules.GestionDuTemps.Entities;

public static class KanbanColonnes
{
    public const string AFaire = "AFaire";
    public const string EnCours = "EnCours";
    public const string Termine = "Termine";
}

/// <summary>
/// Statut d'avancement Kanban d'une <see cref="Activite"/> (relation un-à-un) + minuteur — repris de
/// <c>GdtKanbanStatut</c> de mvp. <see cref="TimerDureeReelleMs"/> accumule le temps réel passé en
/// <see cref="KanbanColonnes.EnCours"/> à travers plusieurs marche/pause (jamais remis à zéro) ;
/// <see cref="TimerDebutUtc"/> n'est renseigné QUE pendant que le minuteur tourne — voir
/// <c>GestionDuTempsService.MarquerDebutAsync/MarquerPauseAsync/MarquerTermineAsync</c> pour la logique
/// exacte de démarrage/pause/finalisation (identique à <c>GdtKanbanTimer</c> de mvp).
/// </summary>
public sealed class KanbanStatut
{
    public int Id { get; set; }

    public int ActiviteId { get; set; }

    public string Statut { get; set; } = KanbanColonnes.AFaire;

    public DateTimeOffset? TimerDebutUtc { get; set; }

    public long TimerDureeReelleMs { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
