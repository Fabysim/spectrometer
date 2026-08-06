using Spectrometre.Modules.GestionDuTemps.Entities;

namespace Spectrometre.Modules.GestionDuTemps.Services;

/// <summary>Calculs purs sur le minuteur d'un <see cref="KanbanStatut"/> — repris tel quel de <c>GdtKanbanTimer</c> (mvp).</summary>
public static class KanbanTimer
{
    public static long GetElapsedMs(KanbanStatut statut, DateTimeOffset? utcNow = null)
    {
        var now = utcNow ?? DateTimeOffset.UtcNow;
        var total = statut.TimerDureeReelleMs;

        if (statut.Statut == KanbanColonnes.EnCours && statut.TimerDebutUtc.HasValue)
            total += (long)(now - statut.TimerDebutUtc.Value).TotalMilliseconds;

        return total;
    }

    public static string Format(long elapsedMs)
    {
        var ts = TimeSpan.FromMilliseconds(Math.Max(0, elapsedMs));
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}h{ts.Minutes:D2}m"
            : $"{ts.Minutes:D2}m{ts.Seconds:D2}s";
    }

    public static bool IsOvertime(long elapsedMs, int dureeMinutesPlanifiee) =>
        elapsedMs > dureeMinutesPlanifiee * 60_000L;
}
