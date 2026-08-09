using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Directory;
using Spectrometre.Modules.ProfilCoach.Data;
using Spectrometre.Modules.ProfilCoach.Entities;

namespace Spectrometre.Modules.ProfilCoach.Services;

/// <summary>Implémentation réelle de <see cref="ICoachDirectoryService"/> — voir sa remarque dans Core. Enregistrée directement par <c>AddProfilCoachModule</c>.</summary>
public sealed class CoachDirectoryService(IDbContextFactory<ProfilCoachDbContext> dbFactory) : ICoachDirectoryService
{
    public async Task<IReadOnlyList<CoachDirectoryEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.CoachProfiles
            .AsNoTracking()
            .OrderBy(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Select(p => new CoachDirectoryEntry(p.Id, p.UserId, p.NomAffiche, p.VisibleDansAnnuaire, p.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(
        string? recherche = null,
        IReadOnlyCollection<string>? matchingUserIds = null,
        IReadOnlyCollection<int>? matchingProfileIds = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await ApplyFilter(db.CoachProfiles.AsNoTracking(), recherche, matchingUserIds, matchingProfileIds)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CoachDirectoryEntry>> GetPageAsync(
        int skip,
        int take,
        string? recherche = null,
        IReadOnlyCollection<string>? matchingUserIds = null,
        IReadOnlyCollection<int>? matchingProfileIds = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await ApplyFilter(db.CoachProfiles.AsNoTracking(), recherche, matchingUserIds, matchingProfileIds)
            .OrderBy(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Skip(skip)
            .Take(take)
            .Select(p => new CoachDirectoryEntry(p.Id, p.UserId, p.NomAffiche, p.VisibleDansAnnuaire, p.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<CoachProfile> ApplyFilter(
        IQueryable<CoachProfile> query,
        string? recherche,
        IReadOnlyCollection<string>? matchingUserIds,
        IReadOnlyCollection<int>? matchingProfileIds)
    {
        if (recherche is null && matchingUserIds is null && matchingProfileIds is null)
            return query;

        var userIds = matchingUserIds ?? [];
        var profileIds = matchingProfileIds ?? [];
        if (recherche is null)
            return query.Where(p => userIds.Contains(p.UserId) || profileIds.Contains(p.Id));

        var lowered = recherche.ToLowerInvariant();
        return query.Where(p =>
            p.NomAffiche.ToLower().Contains(lowered)
            || userIds.Contains(p.UserId)
            || profileIds.Contains(p.Id));
    }
}
