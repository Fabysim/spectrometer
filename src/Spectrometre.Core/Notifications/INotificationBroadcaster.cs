namespace Spectrometre.Core.Notifications;

/// <summary>
/// Diffusion en mémoire (process unique, pas de scale-out multi-instance pour l'instant) d'un événement
/// « nouvelle notification » vers les composants Blazor abonnés (typiquement NotificationBell.razor),
/// pour un badge en temps réel sans introduire de second circuit SignalR — le circuit Blazor Server
/// existant suffit déjà à pousser la mise à jour au navigateur une fois StateHasChanged appelé côté abonné.
/// </summary>
public interface INotificationBroadcaster
{
    /// <summary>S'abonne aux notifications d'un utilisateur. Retourne un IDisposable à jeter dans Dispose().</summary>
    IDisposable Subscribe(string userId, Func<Task> onNotification);

    /// <summary>Notifie tous les abonnés de cet utilisateur (appelé uniquement par NotificationService).</summary>
    void Publish(string userId);
}
