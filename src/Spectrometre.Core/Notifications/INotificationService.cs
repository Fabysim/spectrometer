namespace Spectrometre.Core.Notifications;

public sealed record NotificationView(
    int Id,
    string Titre,
    string Message,
    string? Lien,
    string TypeCode,
    DateTimeOffset CreatedAt,
    bool EstLue);

/// <summary>Socle transverse de notifications in-app — aucun module ne doit dupliquer cette table.</summary>
public interface INotificationService
{
/// <summary>
/// Crée une notification in-app. Retourne l'Id persisté, ou <c>0</c> si la préférence
/// de catégorie est désactivée (rien n'est écrit).
/// </summary>
Task<int> CreerAsync(
    string userId,
    string titre,
    string message,
    string? lien,
    string typeCode,
    CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationView>> GetNonLuesAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Lues + non lues, plus récentes d'abord — pour le panneau déroulant.</summary>
    Task<IReadOnlyList<NotificationView>> GetRecentesAsync(string userId, int limite, CancellationToken cancellationToken = default);

    /// <summary><c>false</c> si la notification n'existe pas ou n'appartient pas à <paramref name="requestingUserId"/>.</summary>
    Task<bool> MarquerLueAsync(int notificationId, string requestingUserId, CancellationToken cancellationToken = default);

    Task MarquerToutesLuesAsync(string userId, CancellationToken cancellationToken = default);
}
