namespace Spectrometre.Core.Notifications;

/// <summary>
/// Catalogue central des catégories de notifications in-app.
/// Pour en ajouter une : (1) constante ici, (2) entrée dans
/// <see cref="NotificationCategoryCatalog.All"/> avec sa règle de pertinence,
/// (3) émetteurs qui utilisent un <c>TypeCode</c> préfixé par ce code.
/// </summary>
public static class NotificationCategoryCodes
{
    /// <summary>Invitations coaching — TypeCode ex. <c>Coaching.DemandeRecue</c>.</summary>
    public const string Coaching = "Coaching";

    /// <summary>Alertes Suivi employés — TypeCode ex. <c>SuiviEmployes.SeuilCritique</c>.</summary>
    public const string SuiviEmployes = "SuiviEmployes";

    /// <summary>
    /// Activités Gestion du temps (libellé UI « Activités ») — TypeCode ex.
    /// <c>GestionDuTemps.ActiviteDebut</c>. Code = préfixe TypeCode, pas « Activites ».
    /// </summary>
    public const string GestionDuTemps = "GestionDuTemps";

    /// <summary>Jeunes prestataires — TypeCode ex. <c>JeunesPrestataires.BesoinAide</c>.</summary>
    public const string JeunesPrestataires = "JeunesPrestataires";

    /// <summary>Missions (particulier / jeune / coach) — TypeCode ex. <c>Missions.MissionValidee</c>, <c>Missions.PublicationValidee</c>, <c>Missions.PublicationRefusee</c>, <c>Missions.MissionTerminee</c>, <c>Missions.DemandeAcceptationEnAttente</c>, <c>Missions.ProblemeSignale</c>, <c>Missions.DemandeContact</c>.</summary>
    public const string Missions = "Missions";
}

public sealed record NotificationCategoryDefinition(
    string CategorieCode,
    string LibelleFr,
    string LibelleEn);

public sealed record PreferenceNotificationView(
    string CategorieCode,
    string Libelle,
    bool Active);

/// <summary>
/// Correspondance catégorie → règle de pertinence. Une seule table à étendre —
/// jamais de <c>if (categorie == ...)</c> dispersés dans les pages.
/// </summary>
public static class NotificationCategoryCatalog
{
    public static IReadOnlyList<NotificationCategoryDefinition> All { get; } =
    [
        new(NotificationCategoryCodes.Coaching, "Invitations", "Invitations"),
        new(NotificationCategoryCodes.SuiviEmployes, "Alertes", "Alerts"),
        new(NotificationCategoryCodes.GestionDuTemps, "Activités", "Activities"),
        new(NotificationCategoryCodes.JeunesPrestataires, "Jeunes prestataires", "Young providers"),
        new(NotificationCategoryCodes.Missions, "Missions", "Missions"),
    ];

    public static string DeriveCategorieCode(string typeCode)
    {
        var dot = typeCode.IndexOf('.');
        return dot > 0 ? typeCode[..dot] : typeCode;
    }
}
