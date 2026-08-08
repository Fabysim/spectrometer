using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Directory;
using Spectrometre.Modules.Coaching.Data;

namespace Spectrometre.Modules.Coaching.Services;

/// <summary>Implémentation réelle de <see cref="ICoachingLinkOverviewService"/> — voir sa remarque dans Core. Enregistrée directement par <c>AddCoachingModule</c>.</summary>
public sealed class CoachingLinkOverviewService(IDbContextFactory<CoachingDbContext> dbFactory) : ICoachingLinkOverviewService
{
    public async Task<IReadOnlyList<CoachingLinkSummary>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.LiensCoaching
            .AsNoTracking()
            .Select(l => new CoachingLinkSummary(l.Id, l.SuiviUserId, l.CoachUserId, l.Statut.ToString(), l.CreatedAt, l.AccepteLe))
            .ToListAsync(cancellationToken);
    }
}
