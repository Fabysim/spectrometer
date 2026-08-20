using Microsoft.EntityFrameworkCore;
using Spectrometre.Modules.Missions.Data;
using Spectrometre.Modules.Missions.Entities;

namespace Spectrometre.Modules.Missions.Services;

public sealed class ParticulierProfileService(IDbContextFactory<MissionsDbContext> dbFactory) : IParticulierProfileService
{
    public async Task<ParticulierProfileView?> TryGetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.ParticulierProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        return entity is null ? null : ToView(entity);
    }

    public async Task<int> GetOrCreateProfileIdAsync(string userId, string nom, string prenoms, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.ParticulierProfiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (existing is not null)
            return existing.Id;

        var profile = new ParticulierProfile
        {
            UserId = userId,
            Nom = nom.Trim(),
            Prenoms = prenoms.Trim(),
        };
        db.ParticulierProfiles.Add(profile);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            await using var freshDb = await dbFactory.CreateDbContextAsync(cancellationToken);
            return (await freshDb.ParticulierProfiles.FirstAsync(p => p.UserId == userId, cancellationToken)).Id;
        }

        return profile.Id;
    }

    private static ParticulierProfileView ToView(ParticulierProfile entity) =>
        new(entity.Id, entity.UserId, entity.Nom, entity.Prenoms, entity.CreatedAt);
}
