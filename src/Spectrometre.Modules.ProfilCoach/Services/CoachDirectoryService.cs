using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Directory;
using Spectrometre.Modules.ProfilCoach.Data;

namespace Spectrometre.Modules.ProfilCoach.Services;

/// <summary>Implémentation réelle de <see cref="ICoachDirectoryService"/> — voir sa remarque dans Core. Enregistrée directement par <c>AddProfilCoachModule</c>.</summary>
public sealed class CoachDirectoryService(IDbContextFactory<ProfilCoachDbContext> dbFactory) : ICoachDirectoryService
{
    public async Task<IReadOnlyList<CoachDirectoryEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.CoachProfiles
            .AsNoTracking()
            .Select(p => new CoachDirectoryEntry(p.Id, p.UserId, p.NomAffiche, p.VisibleDansAnnuaire, p.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
