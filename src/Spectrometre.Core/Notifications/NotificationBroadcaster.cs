using System.Collections.Concurrent;

namespace Spectrometre.Core.Notifications;

/// <inheritdoc cref="INotificationBroadcaster"/>
public sealed class NotificationBroadcaster : INotificationBroadcaster
{
    private readonly ConcurrentDictionary<string, List<Func<Task>>> _subscribers = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    public IDisposable Subscribe(string userId, Func<Task> onNotification)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(onNotification);

        lock (_gate)
        {
            if (!_subscribers.TryGetValue(userId, out var list))
            {
                list = [];
                _subscribers[userId] = list;
            }

            list.Add(onNotification);
        }

        return new Subscription(this, userId, onNotification);
    }

    public void Publish(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        List<Func<Task>> snapshot;
        lock (_gate)
        {
            if (!_subscribers.TryGetValue(userId, out var list) || list.Count == 0)
                return;
            snapshot = [.. list];
        }

        foreach (var callback in snapshot)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await callback();
                }
                catch
                {
                    /* un abonné en erreur ne doit jamais faire échouer la création de la notification
                       pour les autres, ni pour l'appelant */
                }
            });
        }
    }

    private void Unsubscribe(string userId, Func<Task> callback)
    {
        lock (_gate)
        {
            if (!_subscribers.TryGetValue(userId, out var list))
                return;

            list.Remove(callback);
            if (list.Count == 0)
                _subscribers.TryRemove(userId, out _);
        }
    }

    private sealed class Subscription(NotificationBroadcaster owner, string userId, Func<Task> callback) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;
            owner.Unsubscribe(userId, callback);
        }
    }
}
