using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Directory;
using Spectrometre.Modules.ProfilCandidat.Data;
using Spectrometre.Modules.ProfilCandidat.Entities;

namespace Spectrometre.Modules.ProfilCandidat.Services;

/// <summary>Implémentation réelle de <see cref="ICandidateDirectoryService"/> — voir sa remarque dans Core. Enregistrée directement par <c>AddProfilCandidatModule</c>.</summary>
public sealed class CandidateDirectoryService(IDbContextFactory<ProfilCandidatDbContext> dbFactory) : ICandidateDirectoryService
{
    public async Task<IReadOnlyList<CandidateDirectoryEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.CandidateProfiles
            .AsNoTracking()
            .OrderBy(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Select(p => new CandidateDirectoryEntry(p.Id, p.UserId, p.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(
        IReadOnlyCollection<string>? matchingUserIds = null,
        IReadOnlyCollection<int>? matchingProfileIds = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await ApplyFilter(db.CandidateProfiles.AsNoTracking(), matchingUserIds, matchingProfileIds)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CandidateDirectoryEntry>> GetPageAsync(
        int skip,
        int take,
        IReadOnlyCollection<string>? matchingUserIds = null,
        IReadOnlyCollection<int>? matchingProfileIds = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await ApplyFilter(db.CandidateProfiles.AsNoTracking(), matchingUserIds, matchingProfileIds)
            .OrderBy(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Skip(skip)
            .Take(take)
            .Select(p => new CandidateDirectoryEntry(p.Id, p.UserId, p.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<CandidateProfile> ApplyFilter(
        IQueryable<CandidateProfile> query,
        IReadOnlyCollection<string>? matchingUserIds,
        IReadOnlyCollection<int>? matchingProfileIds)
    {
        if (matchingUserIds is null && matchingProfileIds is null)
            return query;

        var userIds = matchingUserIds ?? [];
        var profileIds = matchingProfileIds ?? [];
        return query.Where(p => userIds.Contains(p.UserId) || profileIds.Contains(p.Id));
    }
}
