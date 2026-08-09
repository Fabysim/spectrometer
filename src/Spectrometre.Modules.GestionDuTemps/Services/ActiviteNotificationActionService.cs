using Microsoft.EntityFrameworkCore;
using Spectrometre.Modules.GestionDuTemps.Data;

namespace Spectrometre.Modules.GestionDuTemps.Services;

/// <summary>
/// Actions directes depuis les liens de notification (démarrage / fin) —
/// refuse silencieusement (sans révéler l'existence) si l'utilisateur n'est pas propriétaire.
/// </summary>
public interface IActiviteNotificationActionService
{
    /// <summary><c>true</c> si l'action a été appliquée ; <c>false</c> si non-propriétaire / introuvable.</summary>
    Task<bool> DemarrerSiProprietaireAsync(string userId, int activiteId, CancellationToken cancellationToken = default);

    Task<bool> TerminerSiProprietaireAsync(string userId, int activiteId, CancellationToken cancellationToken = default);
}

public sealed class ActiviteNotificationActionService(
    IDbContextFactory<GestionDuTempsDbContext> dbFactory,
    IGestionDuTempsService gestionDuTemps) : IActiviteNotificationActionService
{
    public async Task<bool> DemarrerSiProprietaireAsync(string userId, int activiteId, CancellationToken cancellationToken = default)
    {
        if (!await EstProprietaireAsync(userId, activiteId, cancellationToken))
            return false;

        await gestionDuTemps.MarquerDebutAsync(userId, activiteId, cancellationToken);
        return true;
    }

    public async Task<bool> TerminerSiProprietaireAsync(string userId, int activiteId, CancellationToken cancellationToken = default)
    {
        if (!await EstProprietaireAsync(userId, activiteId, cancellationToken))
            return false;

        await gestionDuTemps.MarquerTermineAsync(userId, activiteId, cancellationToken);
        return true;
    }

    private async Task<bool> EstProprietaireAsync(string userId, int activiteId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Activites.AsNoTracking()
            .AnyAsync(a => a.Id == activiteId && a.UserId == userId, cancellationToken);
    }
}
