using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Directory;
using Spectrometre.Modules.Coaching.Data;
using Spectrometre.Modules.Coaching.Entities;

namespace Spectrometre.Modules.Coaching.Services;

/// <summary>Implémentation réelle de <see cref="ICoachingLinkOverviewService"/> — voir sa remarque dans Core. Enregistrée directement par <c>AddCoachingModule</c>.</summary>
public sealed class CoachingLinkOverviewService(IDbContextFactory<CoachingDbContext> dbFactory) : ICoachingLinkOverviewService
{
    public async Task<IReadOnlyList<CoachingLinkSummary>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.LiensCoaching
            .AsNoTracking()
            .OrderByDescending(l => l.CreatedAt)
            .ThenBy(l => l.Id)
            .Select(l => new CoachingLinkSummary(l.Id, l.SuiviUserId, l.CoachUserId, l.Statut.ToString(), l.CreatedAt, l.AccepteLe))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(
        string? recherche = null,
        IReadOnlyCollection<string>? matchingUserIds = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await ApplyFilter(db.LiensCoaching.AsNoTracking(), recherche, matchingUserIds)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CoachingLinkSummary>> GetPageAsync(
        int skip,
        int take,
        string? recherche = null,
        IReadOnlyCollection<string>? matchingUserIds = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await ApplyFilter(db.LiensCoaching.AsNoTracking(), recherche, matchingUserIds)
            .OrderByDescending(l => l.CreatedAt)
            .ThenBy(l => l.Id)
            .Skip(skip)
            .Take(take)
            .Select(l => new CoachingLinkSummary(l.Id, l.SuiviUserId, l.CoachUserId, l.Statut.ToString(), l.CreatedAt, l.AccepteLe))
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<LienCoaching> ApplyFilter(
        IQueryable<LienCoaching> query,
        string? recherche,
        IReadOnlyCollection<string>? matchingUserIds)
    {
        if (recherche is null)
            return query;

        var userIds = matchingUserIds ?? [];
        var matchingStatuts = Enum.GetValues<LienCoachingStatut>()
            .Where(s => s.ToString().Contains(recherche, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return query.Where(l =>
            userIds.Contains(l.SuiviUserId)
            || userIds.Contains(l.CoachUserId)
            || matchingStatuts.Contains(l.Statut));
    }
}
