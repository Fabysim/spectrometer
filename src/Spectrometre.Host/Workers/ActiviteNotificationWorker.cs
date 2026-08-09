using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Notifications;
using Spectrometre.Modules.GestionDuTemps.Data;
using Spectrometre.Modules.GestionDuTemps.Services;

namespace Spectrometre.Host.Workers;

/// <summary>
/// Premier <see cref="BackgroundService"/> du Host : tick ~1 min, émet les notifications
/// début/fin d'activité GDT. La sélection est déléguée à
/// <see cref="ActiviteNotificationSelector"/> (testable sans hébergement).
/// </summary>
public sealed class ActiviteNotificationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ActiviteNotificationWorker> logger) : BackgroundService
{
    private static readonly TimeSpan TickPeriod = TimeSpan.FromMinutes(1);
    private DateTime _lastTickLocal = DateTime.Now;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Décale le premier tick pour laisser le démarrage (migrations) se terminer.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        _lastTickLocal = DateTime.Now.AddMinutes(-1);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "ActiviteNotificationWorker : échec du tick.");
            }

            try
            {
                await Task.Delay(TickPeriod, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    internal async Task TickAsync(CancellationToken cancellationToken)
    {
        var windowEnd = DateTime.Now;
        var windowStart = _lastTickLocal;
        _lastTickLocal = windowEnd;

        // Fenêtre un peu élargie vers le passé pour absorber un retard de tick, sans remonter l'historique.
        if (windowEnd - windowStart > TimeSpan.FromMinutes(5))
            windowStart = windowEnd.AddMinutes(-5);

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<GestionDuTempsDbContext>>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        // Candidats : flags à false et date dans une bande large autour de la fenêtre.
        var dateMin = DateOnly.FromDateTime(windowStart.Date.AddDays(-1));
        var dateMax = DateOnly.FromDateTime(windowEnd.Date.AddDays(1));

        var rows = await db.Activites
            .Where(a => a.DateActivite >= dateMin && a.DateActivite <= dateMax
                        && (!a.NotificationDebutEnvoyee || !a.NotificationFinEnvoyee))
            .Select(a => new ActiviteScheduleSnapshot(
                a.Id,
                a.UserId,
                a.Nom,
                a.DateActivite,
                a.HeureDebut,
                a.DureeMinutes,
                a.NotificationDebutEnvoyee,
                a.NotificationFinEnvoyee))
            .ToListAsync(cancellationToken);

        var due = ActiviteNotificationSelector.SelectDue(rows, windowStart, windowEnd);
        if (due.Count == 0)
            return;

        foreach (var (snap, kind) in due)
        {
            var entity = await db.Activites.FirstOrDefaultAsync(a => a.Id == snap.Id, cancellationToken);
            if (entity is null)
                continue;

            if (kind == ActiviteNotificationKind.Debut)
            {
                if (entity.NotificationDebutEnvoyee)
                    continue;

                await notifications.CreerAsync(
                    entity.UserId,
                    "Début d'activité",
                    $"« {entity.Nom} » commence maintenant.",
                    $"/gestion-du-temps/activite/{entity.Id}/demarrer",
                    "GestionDuTemps.ActiviteDebut",
                    cancellationToken);

                entity.NotificationDebutEnvoyee = true;
            }
            else
            {
                if (entity.NotificationFinEnvoyee)
                    continue;

                await notifications.CreerAsync(
                    entity.UserId,
                    "Fin d'activité",
                    $"« {entity.Nom} » devrait se terminer maintenant.",
                    $"/gestion-du-temps/activite/{entity.Id}/terminer",
                    "GestionDuTemps.ActiviteFin",
                    cancellationToken);

                entity.NotificationFinEnvoyee = true;
            }

            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
