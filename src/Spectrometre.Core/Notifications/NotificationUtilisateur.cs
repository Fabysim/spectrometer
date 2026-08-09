namespace Spectrometre.Core.Notifications;

/// <summary>
/// Notification in-app destinée à un utilisateur. Concept transverse (schéma <c>core</c>) —
/// n'importe quel module peut en créer via <see cref="INotificationService"/> sans connaître les autres.
/// </summary>
public sealed class NotificationUtilisateur
{
    public int Id { get; set; }

    public required string UserId { get; set; }

    public required string Titre { get; set; }

    public required string Message { get; set; }

    /// <summary>URL relative à ouvrir au clic (ex. <c>/coach/suivis</c>). Null = information seule.</summary>
    public string? Lien { get; set; }

    /// <summary>Catégorie libre (ex. <c>Coaching.DemandeRecue</c>) — pas de comportement dérivé.</summary>
    public required string TypeCode { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LueLe { get; set; }
}
