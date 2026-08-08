namespace Spectrometre.Modules.GestionDuTemps.Helpers;

/// <summary>Durées théoriques / réelles pour le moniteur (porté de <c>GdtTypeDuration</c> mvp).</summary>
public static class TypeDuration
{
    public static double GetDurationHours(TimeOnly debut, TimeOnly fin)
    {
        var start = debut.ToTimeSpan();
        var end = fin.ToTimeSpan();
        if (end >= start)
            return (end - start).TotalHours;

        return (TimeSpan.FromHours(24) - start + end).TotalHours;
    }

    public static double GetTempsReelHeures(long tempsReelMs, string statut)
    {
        if (statut is not (Entities.KanbanColonnes.Termine or Entities.KanbanColonnes.EnCours))
            return 0;

        return tempsReelMs / 3_600_000.0;
    }
}
