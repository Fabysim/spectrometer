using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Notifications;
using Spectrometre.Modules.Coaching.Entities;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.JeunesPrestataires.Services;
using Spectrometre.Modules.Missions.Data;
using Spectrometre.Modules.Missions.Entities;

namespace Spectrometre.Modules.Missions.Services;

public sealed class MissionRetourService(
    IDbContextFactory<MissionsDbContext> dbFactory,
    IJeuneProfileService jeuneProfileService,
    ICoachingService coachingService,
    INotificationService notificationService) : IMissionRetourService
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

        var acceptation = resolved.Value.Acceptation;
        var now = DateTimeOffset.UtcNow;
        var entity = await db.MissionRetours
            .FirstOrDefaultAsync(r => r.MissionAcceptationId == missionAcceptationId, cancellationToken);

        // Première persistance uniquement : un ré-enregistrement ne spam pas le coach.
        var premiereSauvegarde = entity is null;
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

        if (premiereSauvegarde)
            await NotifierCoachRetourDisponibleAsync(acceptation, cancellationToken);

        return true;
    }

    private async Task NotifierCoachRetourDisponibleAsync(
        MissionAcceptation acceptation,
        CancellationToken cancellationToken)
    {
        var jeune = await jeuneProfileService.TryGetByIdAsync(acceptation.JeuneProfileId, cancellationToken);
        if (jeune is null)
            return;

        var liens = await coachingService.GetLiensPourSuiviAsync(jeune.UserId, cancellationToken);
        var coachId = liens.FirstOrDefault(l => l.Statut == LienCoachingStatut.Actif)?.CoachUserId;
        if (coachId is null)
            return;

        var titre = MissionDisplay.TitreAffiche(acceptation.Mission.Categorie, acceptation.Mission.Titre);
        var jeuneNom = $"{jeune.Prenoms} {jeune.Nom}".Trim();
        await notificationService.CreerAsync(
            coachId,
            "Retour de mission disponible",
            $"{jeuneNom} a enregistré son retour sur la mission « {titre} ».",
            $"/coach/suivis/{jeune.UserId}/missions/{acceptation.Id}/retour",
            "Missions.RetourJeuneDisponible",
            cancellationToken);
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
            MissionDisplay.TitreAffiche(acceptation.Mission.Categorie, acceptation.Mission.Titre),
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
