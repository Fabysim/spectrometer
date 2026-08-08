using Spectrometre.Modules.GestionDuTemps.Entities;
using Spectrometre.Modules.GestionDuTemps.Services;

namespace Spectrometre.Modules.GestionDuTemps.Helpers;

/// <summary>
/// Agrégation moniteur (barre / radar « Réel ») — porté de <c>TableauDeBord.GetHeuresReelles</c> / <c>IsInCurrentWindow</c> mvp.
/// Les activités Kanban <see cref="KanbanColonnes.EnCours"/> et <see cref="KanbanColonnes.Termine"/>
/// alimentent les graphiques via <see cref="KanbanCarteView.TempsReelMs"/> (minuteur).
/// </summary>
public static class MoniteurChartMath
{
    public static double SumHeuresReelles(
        int typeDeTempsId,
        IEnumerable<KanbanCarteView> cartes,
        DateTimeOffset cycleStartedAt,
        string periodeReset)
    {
        var windowStart = GetResetWindowStart(cycleStartedAt, periodeReset);
        return cartes
            .Where(c => c.TypeDeTempsId == typeDeTempsId)
            .Where(c => IsInCurrentWindow(c, windowStart))
            .Sum(c => TypeDuration.GetTempsReelHeures(c.TempsReelMs, c.Statut));
    }

    public static bool IsInCurrentWindow(KanbanCarteView carte, DateTime windowStartLocal)
    {
        if (carte.Statut == KanbanColonnes.EnCours)
            return true;

        if (carte.Statut != KanbanColonnes.Termine)
            return false;

        // Identique au mvp : UpdatedAt du statut OU CreatedAt de l'activité dans la fenêtre de reset.
        return carte.UpdatedAt.LocalDateTime >= windowStartLocal ||
               carte.ActiviteCreatedAt.LocalDateTime >= windowStartLocal;
    }

    public static DateTime GetResetWindowStart(DateTimeOffset cycleStartedAt, string periodeReset)
    {
        var cycleStart = cycleStartedAt.LocalDateTime;
        var now = DateTime.Now;

        var periodStart = periodeReset switch
        {
            "Journalier" => DateTime.Today,
            "Hebdomadaire" => GetWeekStart(now),
            "Mensuel" => new DateTime(now.Year, now.Month, 1),
            "Trimestriel" => new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1),
            "Annuel" => new DateTime(now.Year, 1, 1),
            _ => GetWeekStart(now)
        };

        return periodStart > cycleStart ? periodStart : cycleStart;
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var daysSinceMonday = date.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)date.DayOfWeek - 1;
        return date.Date.AddDays(-daysSinceMonday);
    }
}
