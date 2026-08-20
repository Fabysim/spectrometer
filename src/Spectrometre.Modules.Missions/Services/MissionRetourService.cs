using Microsoft.EntityFrameworkCore;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.JeunesPrestataires.Services;
using Spectrometre.Modules.Missions.Data;
using Spectrometre.Modules.Missions.Entities;

namespace Spectrometre.Modules.Missions.Services;

public sealed class MissionRetourService(
    IDbContextFactory<MissionsDbContext> dbFactory,
    IJeuneProfileService jeuneProfileService,
    ICoachingService coachingService) : IMissionRetourService
{
    public async Task<MissionRetourView?> GetOrCreateAsync(
        string requestingUserId,
        int missionAcceptationId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var resolved = await ResolveAccessAsync(db, requestingUserId, missionAcceptationId, cancellationToken);
        if (resolved is null)
            return null;

        var (acceptation, mode) = resolved.Value;
        var entity = await db.MissionRetours.AsNoTracking()
            .FirstOrDefaultAsync(r => r.MissionAcceptationId == missionAcceptationId, cancellationToken);

        return ToView(acceptation, entity, mode);
    }

    public async Task<bool> SaveAsync(
        string jeuneUserId,
        int missionAcceptationId,
        string? ceQuiSestBienPasse,
        string? ceQuiAEteDifficile,
        string? ceQueJaiAppris,
        string? ceQueJeVeuxAmeliorer,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var resolved = await ResolveAccessAsync(db, jeuneUserId, missionAcceptationId, cancellationToken);
        if (resolved is null || resolved.Value.Mode != MissionRetourAccessMode.Jeune)
            return false;

        var now = DateTimeOffset.UtcNow;
        var entity = await db.MissionRetours
            .FirstOrDefaultAsync(r => r.MissionAcceptationId == missionAcceptationId, cancellationToken);

        if (entity is null)
        {
            entity = new MissionRetour
            {
                MissionAcceptationId = missionAcceptationId,
                CreatedAt = now,
            };
            db.MissionRetours.Add(entity);
        }

        entity.CeQuiSestBienPasse = Normalize(ceQuiSestBienPasse);
        entity.CeQuiAEteDifficile = Normalize(ceQuiAEteDifficile);
        entity.CeQueJaiAppris = Normalize(ceQueJaiAppris);
        entity.CeQueJeVeuxAmeliorer = Normalize(ceQueJeVeuxAmeliorer);
        entity.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Propriétaire jeune ou coach suiveur — uniquement si mission <see cref="MissionStatut.Terminee"/>
    /// et acceptation <see cref="MissionAcceptationStatut.ValideeParCoach"/>.
    /// </summary>
    private async Task<(MissionAcceptation Acceptation, MissionRetourAccessMode Mode)?> ResolveAccessAsync(
        MissionsDbContext db,
        string requestingUserId,
        int missionAcceptationId,
        CancellationToken cancellationToken)
    {
        var acceptation = await db.MissionAcceptations
            .Include(a => a.Mission)
            .FirstOrDefaultAsync(a => a.Id == missionAcceptationId, cancellationToken);

        if (acceptation is null)
            return null;

        if (acceptation.Statut != MissionAcceptationStatut.ValideeParCoach)
            return null;

        if (acceptation.Mission.Statut != MissionStatut.Terminee)
            return null;

        var jeune = await jeuneProfileService.TryGetByIdAsync(acceptation.JeuneProfileId, cancellationToken);
        if (jeune is null)
            return null;

        if (jeune.UserId == requestingUserId)
            return (acceptation, MissionRetourAccessMode.Jeune);

        var autorise = await coachingService.GetSuiviUserIdSiAutoriseAsync(jeune.UserId, requestingUserId, cancellationToken);
        if (autorise is not null)
            return (acceptation, MissionRetourAccessMode.Coach);

        return null;
    }

    private static MissionRetourView ToView(
        MissionAcceptation acceptation,
        MissionRetour? entity,
        MissionRetourAccessMode mode) =>
        new(
            acceptation.Id,
            acceptation.Mission.Titre,
            entity?.CeQuiSestBienPasse,
            entity?.CeQuiAEteDifficile,
            entity?.CeQueJaiAppris,
            entity?.CeQueJeVeuxAmeliorer,
            mode,
            PeutEcrire: mode == MissionRetourAccessMode.Jeune,
            entity?.UpdatedAt);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
