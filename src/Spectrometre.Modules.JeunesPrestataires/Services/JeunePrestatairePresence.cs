using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Modules;
using Spectrometre.Modules.JeunesPrestataires.Data;

namespace Spectrometre.Modules.JeunesPrestataires.Services;

/// <summary>
/// Implémentation réelle de <see cref="IJeunePrestatairePresence"/>. Lit le DbContext directement
/// (pas <see cref="IJeuneProfileService"/>) : JeuneProfileService dépend déjà de ICoachingService,
/// et CoachingService consomme cette présence — un relais par le service recréerait un cycle DI.
/// </summary>
public sealed class JeunePrestatairePresence(IDbContextFactory<JeunesPrestatairesDbContext> dbFactory) : IJeunePrestatairePresence
{
    public async Task<bool> EstJeunePrestataireAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.JeuneProfiles.AsNoTracking().AnyAsync(p => p.UserId == userId, cancellationToken);
    }
}
