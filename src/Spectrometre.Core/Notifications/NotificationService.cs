using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Data;

namespace Spectrometre.Core.Notifications;

/// <summary>
/// Implémentation sur <see cref="CoreDbContext"/> via factory (jamais un contexte scopé partagé) —
/// le badge peut être rafraîchi depuis le layout pendant qu'une page mute en parallèle.
/// </summary>
public sealed class NotificationService(
    IDbContextFactory<CoreDbContext> coreDbFactory,
    IPreferenceNotificationService preferences,
    INotificationBroadcaster broadcaster) : INotificationService
{
    /// <summary>
    /// Crée une notification sauf si la préférence de catégorie est désactivée.
    /// Retourne <c>0</c> si rien n'a été persisté (opt-out).
    /// </summary>
    public async Task<int> CreerAsync(
        string userId,
        string titre,
        string message,
        string? lien,
        string typeCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(titre);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(typeCode);

        var categorie = NotificationCategoryCatalog.DeriveCategorieCode(typeCode.Trim());
        if (!await preferences.EstCategorieActiveAsync(userId, categorie, cancellationToken))
            return 0;

        await using var db = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        var entity = new NotificationUtilisateur
        {
            UserId = userId,
            Titre = titre.Trim(),
            Message = message.Trim(),
            Lien = string.IsNullOrWhiteSpace(lien) ? null : lien.Trim(),
            TypeCode = typeCode.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.NotificationsUtilisateur.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        broadcaster.Publish(userId);
        return entity.Id;
    }

    public async Task<IReadOnlyList<NotificationView>> GetNonLuesAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        return await db.NotificationsUtilisateur.AsNoTracking()
            .Where(n => n.UserId == userId && n.LueLe == null)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationView(n.Id, n.Titre, n.Message, n.Lien, n.TypeCode, n.CreatedAt, n.LueLe != null))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationView>> GetRecentesAsync(string userId, int limite, CancellationToken cancellationToken = default)
    {
        if (limite < 1)
            limite = 1;
        if (limite > 50)
            limite = 50;

        await using var db = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        return await db.NotificationsUtilisateur.AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limite)
            .Select(n => new NotificationView(n.Id, n.Titre, n.Message, n.Lien, n.TypeCode, n.CreatedAt, n.LueLe != null))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> MarquerLueAsync(int notificationId, string requestingUserId, CancellationToken cancellationToken = default)
    {
        await using var db = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        var n = await db.NotificationsUtilisateur.FirstOrDefaultAsync(
            x => x.Id == notificationId && x.UserId == requestingUserId, cancellationToken);
        if (n is null)
            return false;

        if (n.LueLe is null)
        {
            n.LueLe = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task MarquerToutesLuesAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        var nonLues = await db.NotificationsUtilisateur
            .Where(n => n.UserId == userId && n.LueLe == null)
            .ToListAsync(cancellationToken);
        if (nonLues.Count == 0)
            return;

        var now = DateTimeOffset.UtcNow;
        foreach (var n in nonLues)
            n.LueLe = now;
        await db.SaveChangesAsync(cancellationToken);
    }
}
