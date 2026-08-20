using Microsoft.EntityFrameworkCore;
using Spectrometre.Modules.JeunesPrestataires.Services;
using Spectrometre.Modules.Missions.Catalog;
using Spectrometre.Modules.Missions.Data;
using Spectrometre.Modules.Missions.Entities;

namespace Spectrometre.Modules.Missions.Services;

public sealed class MissionPreparationService(
    IDbContextFactory<MissionsDbContext> dbFactory,
    IJeuneProfileService jeuneProfileService) : IMissionPreparationService
{
    public async Task<MissionPreparationView?> GetPreparationAsync(
        string jeuneUserId,
        int missionAcceptationId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var acceptation = await AuthorizeAsync(db, jeuneUserId, missionAcceptationId, cancellationToken);
        if (acceptation is null)
            return null;

        var coches = await db.MissionPreparationCoches.AsNoTracking()
            .Where(c => c.MissionAcceptationId == missionAcceptationId)
            .ToDictionaryAsync(c => c.ItemKey, c => c.Coche, StringComparer.Ordinal, cancellationToken);

        var items = MissionPreparationCatalog.Items
            .Select(def => new MissionPreparationItemView(def.Key, coches.GetValueOrDefault(def.Key)))
            .ToList();

        return new MissionPreparationView(missionAcceptationId, acceptation.Mission.Titre, items);
    }

    public async Task<bool> ToggleItemPreparationAsync(
        string jeuneUserId,
        int missionAcceptationId,
        string itemKey,
        bool coche,
        CancellationToken cancellationToken = default)
    {
        if (!MissionPreparationCatalog.IsValidItemKey(itemKey))
            return false;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        if (await AuthorizeAsync(db, jeuneUserId, missionAcceptationId, cancellationToken) is null)
            return false;

        var existing = await db.MissionPreparationCoches
            .FirstOrDefaultAsync(
                c => c.MissionAcceptationId == missionAcceptationId && c.ItemKey == itemKey,
                cancellationToken);

        var now = DateTimeOffset.UtcNow;
        if (existing is null)
        {
            db.MissionPreparationCoches.Add(new MissionPreparationCoche
            {
                MissionAcceptationId = missionAcceptationId,
                ItemKey = itemKey,
                Coche = coche,
                UpdatedAt = now,
            });
        }
        else
        {
            existing.Coche = coche;
            existing.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Jeune propriétaire de l'acceptation ET statut <see cref="MissionAcceptationStatut.ValideeParCoach"/>.
    /// </summary>
    private async Task<MissionAcceptation?> AuthorizeAsync(
        MissionsDbContext db,
        string jeuneUserId,
        int missionAcceptationId,
        CancellationToken cancellationToken)
    {
        var jeune = await jeuneProfileService.TryGetByUserIdAsync(jeuneUserId, cancellationToken);
        if (jeune is null)
            return null;

        var acceptation = await db.MissionAcceptations
            .Include(a => a.Mission)
            .FirstOrDefaultAsync(a => a.Id == missionAcceptationId, cancellationToken);

        if (acceptation is null)
            return null;

        if (acceptation.JeuneProfileId != jeune.Id)
            return null;

        if (acceptation.Statut != MissionAcceptationStatut.ValideeParCoach)
            return null;

        return acceptation;
    }
}
