using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.JeunesPrestataires;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.JeunesPrestataires.Services;
using Spectrometre.Modules.Missions.Data;
using Spectrometre.Modules.Missions.Entities;

namespace Spectrometre.Modules.Missions.Services;

/// <summary>
/// Modes d'accès à l'évaluation constructive côté particulier :
/// <list type="bullet">
/// <item><see cref="MissionEvaluationParticulierAccessMode.Particulier"/> — seul mode écriture (propriétaire de la mission).</item>
/// <item><see cref="MissionEvaluationParticulierAccessMode.Jeune"/> — lecture seule (jeune de l'acceptation).</item>
/// <item><see cref="MissionEvaluationParticulierAccessMode.Coach"/> — lecture seule (coach suiveur via GetSuiviUserIdSiAutoriseAsync).</item>
/// </list>
/// </summary>
public enum MissionEvaluationParticulierAccessMode
{
    Particulier = 0,
    Jeune = 1,
    Coach = 2,
}

public sealed record MissionEvaluationParticulierView(
    int MissionAcceptationId,
    string MissionTitre,
    bool? Ponctualite,
    bool? ConsignesComprises,
    bool? TacheRealiseeCorrectement,
    bool? AttitudeRespectueuse,
    string? PointsPositifs,
    string? PointsAAmeliorer,
    bool? AccepteraitNouvelleMission,
    MissionEvaluationParticulierAccessMode AccessMode,
    bool PeutEcrire,
    DateTimeOffset? UpdatedAt);

/// <summary>
/// Évaluation constructive après mission — écriture particulier uniquement
/// (<c>Mission.Statut == Terminee</c>) ; lecture jeune concerné + coach suiveur
/// pour une mission donnée. L'historique agrégé (<see cref="IRetoursParticuliersCoachQuery"/>)
/// est coach uniquement — jamais le jeune.
/// </summary>
public interface IMissionEvaluationParticulierService : IRetoursParticuliersCoachQuery
{
    /// <summary>
    /// Vue existante ou vide (sans créer en base). <c>null</c> si aucun accès
    /// ou mission non terminée.
    /// </summary>
    Task<MissionEvaluationParticulierView?> GetOrCreateAsync(
        string requestingUserId,
        int missionAcceptationId,
        CancellationToken cancellationToken = default);

    /// <summary>Upsert — particulier propriétaire uniquement. <c>false</c> sinon.</summary>
    Task<bool> SaveAsync(
        string particulierUserId,
        int missionAcceptationId,
        bool? ponctualite,
        bool? consignesComprises,
        bool? tacheRealiseeCorrectement,
        bool? attitudeRespectueuse,
        string? pointsPositifs,
        string? pointsAAmeliorer,
        bool? accepteraitNouvelleMission,
        CancellationToken cancellationToken = default);
}

public sealed class MissionEvaluationParticulierService(
    IDbContextFactory<MissionsDbContext> dbFactory,
    IParticulierProfileService particulierProfileService,
    IJeuneProfileService jeuneProfileService,
    ICoachingService coachingService) : IMissionEvaluationParticulierService
{
    public async Task<MissionEvaluationParticulierView?> GetOrCreateAsync(
        string requestingUserId,
        int missionAcceptationId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var resolved = await ResolveAccessAsync(db, requestingUserId, missionAcceptationId, cancellationToken);
        if (resolved is null)
            return null;

        var (acceptation, mode) = resolved.Value;
        var entity = await db.MissionEvaluationsParticulier.AsNoTracking()
            .FirstOrDefaultAsync(e => e.MissionAcceptationId == missionAcceptationId, cancellationToken);

        return ToView(acceptation, entity, mode);
    }

    public async Task<bool> SaveAsync(
        string particulierUserId,
        int missionAcceptationId,
        bool? ponctualite,
        bool? consignesComprises,
        bool? tacheRealiseeCorrectement,
        bool? attitudeRespectueuse,
        string? pointsPositifs,
        string? pointsAAmeliorer,
        bool? accepteraitNouvelleMission,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var resolved = await ResolveAccessAsync(db, particulierUserId, missionAcceptationId, cancellationToken);
        if (resolved is null || resolved.Value.Mode != MissionEvaluationParticulierAccessMode.Particulier)
            return false;

        var now = DateTimeOffset.UtcNow;
        var entity = await db.MissionEvaluationsParticulier
            .FirstOrDefaultAsync(e => e.MissionAcceptationId == missionAcceptationId, cancellationToken);

        if (entity is null)
        {
            entity = new MissionEvaluationParticulier
            {
                MissionAcceptationId = missionAcceptationId,
                CreatedAt = now,
            };
            db.MissionEvaluationsParticulier.Add(entity);
        }

        entity.Ponctualite = ponctualite;
        entity.ConsignesComprises = consignesComprises;
        entity.TacheRealiseeCorrectement = tacheRealiseeCorrectement;
        entity.AttitudeRespectueuse = attitudeRespectueuse;
        entity.PointsPositifs = Normalize(pointsPositifs);
        entity.PointsAAmeliorer = Normalize(pointsAAmeliorer);
        entity.AccepteraitNouvelleMission = accepteraitNouvelleMission;
        entity.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<RetourParticulierCoachItem>> GetHistoriquePourCoachAsync(
        string requestingUserId,
        int jeuneProfileId,
        CancellationToken cancellationToken = default)
    {
        var jeune = await jeuneProfileService.TryGetByIdAsync(jeuneProfileId, cancellationToken);
        if (jeune is null)
            return [];

        var autorise = await coachingService.GetSuiviUserIdSiAutoriseAsync(
            jeune.UserId, requestingUserId, cancellationToken);
        if (autorise is null)
            return [];

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.MissionEvaluationsParticulier.AsNoTracking()
            .Include(e => e.MissionAcceptation)
            .ThenInclude(a => a.Mission)
            .Where(e => e.MissionAcceptation.JeuneProfileId == jeuneProfileId)
            .OrderByDescending(e => e.UpdatedAt)
            .ToListAsync(cancellationToken);

        return rows.Select(e => new RetourParticulierCoachItem(
            e.MissionAcceptationId,
            MissionDisplay.TitreAffiche(e.MissionAcceptation.Mission.Categorie, e.MissionAcceptation.Mission.Titre),
            e.UpdatedAt,
            e.Ponctualite,
            e.ConsignesComprises,
            e.TacheRealiseeCorrectement,
            e.AttitudeRespectueuse,
            e.PointsPositifs,
            e.PointsAAmeliorer,
            e.AccepteraitNouvelleMission)).ToList();
    }

    /// <summary>
    /// Trois lecteurs possibles, un seul rédacteur :
    /// particulier propriétaire (écriture), jeune de l'acceptation (lecture), coach suiveur (lecture).
    /// Garde commune : acceptation validée + mission <see cref="MissionStatut.Terminee"/>.
    /// </summary>
    private async Task<(MissionAcceptation Acceptation, MissionEvaluationParticulierAccessMode Mode)?> ResolveAccessAsync(
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

        var particulier = await particulierProfileService.TryGetByIdAsync(
            acceptation.Mission.ParticulierProfileId, cancellationToken);
        if (particulier is not null && particulier.UserId == requestingUserId)
            return (acceptation, MissionEvaluationParticulierAccessMode.Particulier);

        var jeune = await jeuneProfileService.TryGetByIdAsync(acceptation.JeuneProfileId, cancellationToken);
        if (jeune is null)
            return null;

        if (jeune.UserId == requestingUserId)
            return (acceptation, MissionEvaluationParticulierAccessMode.Jeune);

        var autorise = await coachingService.GetSuiviUserIdSiAutoriseAsync(jeune.UserId, requestingUserId, cancellationToken);
        if (autorise is not null)
            return (acceptation, MissionEvaluationParticulierAccessMode.Coach);

        return null;
    }

    private static MissionEvaluationParticulierView ToView(
        MissionAcceptation acceptation,
        MissionEvaluationParticulier? entity,
        MissionEvaluationParticulierAccessMode mode) =>
        new(
            acceptation.Id,
            MissionDisplay.TitreAffiche(acceptation.Mission.Categorie, acceptation.Mission.Titre),
            entity?.Ponctualite,
            entity?.ConsignesComprises,
            entity?.TacheRealiseeCorrectement,
            entity?.AttitudeRespectueuse,
            entity?.PointsPositifs,
            entity?.PointsAAmeliorer,
            entity?.AccepteraitNouvelleMission,
            mode,
            PeutEcrire: mode == MissionEvaluationParticulierAccessMode.Particulier,
            entity?.UpdatedAt);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
