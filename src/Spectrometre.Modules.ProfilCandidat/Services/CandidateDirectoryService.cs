using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Directory;
using Spectrometre.Modules.ProfilCandidat.Data;

namespace Spectrometre.Modules.ProfilCandidat.Services;

/// <summary>Implémentation réelle de <see cref="ICandidateDirectoryService"/> — voir sa remarque dans Core. Enregistrée directement par <c>AddProfilCandidatModule</c>.</summary>
public sealed class CandidateDirectoryService(IDbContextFactory<ProfilCandidatDbContext> dbFactory) : ICandidateDirectoryService
{
    public async Task<IReadOnlyList<CandidateDirectoryEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.CandidateProfiles
            .AsNoTracking()
            .Select(p => new CandidateDirectoryEntry(p.Id, p.UserId, p.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
