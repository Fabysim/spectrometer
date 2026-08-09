namespace Spectrometre.Modules.GestionDuTemps.Services;

/// <summary>
/// Instantané minimal pour sélectionner les activités à notifier — logique pure, testable sans horloge réelle.
/// </summary>
public sealed record ActiviteScheduleSnapshot(
    int Id,
    string UserId,
    string Nom,
    DateOnly DateActivite,
    TimeOnly HeureDebut,
    int DureeMinutes,
    bool NotificationDebutEnvoyee,
    bool NotificationFinEnvoyee);

public enum ActiviteNotificationKind
{
    Debut,
    Fin,
}

/// <summary>
/// Sélection des activités dont le début/fin planifié tombe dans une fenêtre locale.
/// Horaires stockés en <see cref="DateOnly"/>/<see cref="TimeOnly"/> « locaux serveur » (pas de TZ dédiée).
/// </summary>
public static class ActiviteNotificationSelector
{
    public static DateTime DebutLocal(ActiviteScheduleSnapshot a) =>
        a.DateActivite.ToDateTime(a.HeureDebut);

    public static DateTime FinLocal(ActiviteScheduleSnapshot a) =>
        DebutLocal(a).AddMinutes(a.DureeMinutes);

    /// <summary>
    /// Retourne les (activité, kind) dus dans <c>[windowStartInclusive, windowEndExclusive)</c>
    /// et dont le flag correspondant n'est pas encore posé.
    /// </summary>
    public static IReadOnlyList<(ActiviteScheduleSnapshot Activite, ActiviteNotificationKind Kind)> SelectDue(
        IEnumerable<ActiviteScheduleSnapshot> source,
        DateTime windowStartInclusive,
        DateTime windowEndExclusive)
    {
        var result = new List<(ActiviteScheduleSnapshot, ActiviteNotificationKind)>();
        foreach (var a in source)
        {
            if (!a.NotificationDebutEnvoyee)
            {
                var debut = DebutLocal(a);
                if (debut >= windowStartInclusive && debut < windowEndExclusive)
                    result.Add((a, ActiviteNotificationKind.Debut));
            }

            if (!a.NotificationFinEnvoyee)
            {
                var fin = FinLocal(a);
                if (fin >= windowStartInclusive && fin < windowEndExclusive)
                    result.Add((a, ActiviteNotificationKind.Fin));
            }
        }

        return result;
    }
}
