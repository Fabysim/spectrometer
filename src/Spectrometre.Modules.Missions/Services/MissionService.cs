using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Notifications;
using Spectrometre.Modules.Coaching.Entities;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.JeunesPrestataires.Services;
using Spectrometre.Modules.Missions.Data;
using Spectrometre.Modules.Missions.Entities;

namespace Spectrometre.Modules.Missions.Services;

/// <summary>
/// Concurrence sur l'acceptation : mise à jour conditionnelle <c>Statut = Disponible</c> via
/// <see cref="RelationalQueryableExtensions.ExecuteUpdateAsync"/> dans une transaction — un seul jeune gagne ;
/// pas d'index partiel EF (choix documenté : atomicité SQL suffisante pour ce cycle).
/// </summary>
public sealed class MissionService(
    IDbContextFactory<MissionsDbContext> dbFactory,
    IParticulierProfileService particulierProfileService,
    IJeuneProfileService jeuneProfileService,
    ICoachingService coachingService,
    INotificationService notificationService) : IMissionService
{
    public async Task<int?> PublierMissionAsync(string particulierUserId, PublierMissionInput input, CancellationToken cancellationToken = default)
    {
        var particulier = await particulierProfileService.TryGetByUserIdAsync(particulierUserId, cancellationToken);
        if (particulier is null)
            return null;

        if (string.IsNullOrWhiteSpace(input.Titre) || string.IsNullOrWhiteSpace(input.Description))
            return null;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var mission = new Mission
        {
            ParticulierProfileId = particulier.Id,
            Titre = input.Titre.Trim(),
            Description = input.Description.Trim(),
            Lieu = string.IsNullOrWhiteSpace(input.Lieu) ? null : input.Lieu.Trim(),
            DureeEstimee = string.IsNullOrWhiteSpace(input.DureeEstimee) ? null : input.DureeEstimee.Trim(),
            Difficulte = input.Difficulte,
            RemunerationMontant = input.RemunerationMontant,
            CompetencesTravaillees = string.IsNullOrWhiteSpace(input.CompetencesTravaillees) ? null : input.CompetencesTravaillees.Trim(),
            Statut = MissionStatut.Disponible,
        };
        db.Missions.Add(mission);
        await db.SaveChangesAsync(cancellationToken);
        return mission.Id;
    }

    public async Task<IReadOnlyList<MissionResumeView>> GetMissionsDisponiblesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Missions.AsNoTracking()
            .Where(m => m.Statut == MissionStatut.Disponible)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new MissionResumeView(m.Id, m.Titre, m.Lieu, m.Statut, m.Difficulte, m.RemunerationMontant, m.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MissionJeuneView>> GetMesMissionsAsync(string jeuneUserId, CancellationToken cancellationToken = default)
    {
        var jeune = await jeuneProfileService.TryGetByUserIdAsync(jeuneUserId, cancellationToken);
        if (jeune is null)
            return [];

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.MissionAcceptations.AsNoTracking()
            .Include(a => a.Mission)
            .Where(a => a.JeuneProfileId == jeune.Id)
            .OrderByDescending(a => a.AccepteeLe)
            .ToListAsync(cancellationToken);

        return rows.Select(a => new MissionJeuneView(
            a.Id,
            a.MissionId,
            a.Mission.Titre,
            a.Mission.Lieu,
            a.Mission.Statut,
            a.Statut,
            a.AccepteeLe,
            a.DecideeLe)).ToList();
    }

    public async Task<bool> AccepterMissionAsync(string jeuneUserId, int missionId, CancellationToken cancellationToken = default)
    {
        var jeune = await jeuneProfileService.TryGetByUserIdAsync(jeuneUserId, cancellationToken);
        if (jeune is null)
            return false;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var updated = await db.Missions
            .Where(m => m.Id == missionId && m.Statut == MissionStatut.Disponible)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.Statut, MissionStatut.EnAttenteValidation), cancellationToken);

        if (updated == 0)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        db.MissionAcceptations.Add(new MissionAcceptation
        {
            MissionId = missionId,
            JeuneProfileId = jeune.Id,
            AccepteeLe = DateTimeOffset.UtcNow,
            Statut = MissionAcceptationStatut.EnAttenteValidationCoach,
        });
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<MissionAcceptationView>> GetDemandesEnAttentePourCoachAsync(string coachUserId, CancellationToken cancellationToken = default)
    {
        var jeuneIds = await GetJeuneProfileIdsSuivisActifsAsync(coachUserId, cancellationToken);
        if (jeuneIds.Count == 0)
            return [];

        return await LoadAcceptationsEnAttenteAsync(jeuneIds, cancellationToken);
    }

    public async Task<IReadOnlyList<MissionAcceptationView>> GetDemandesEnAttentePourJeuneSuiviAsync(
        string coachUserId,
        string suiviUserId,
        CancellationToken cancellationToken = default)
    {
        var autorise = await coachingService.GetSuiviUserIdSiAutoriseAsync(suiviUserId, coachUserId, cancellationToken);
        if (autorise is null)
            return [];

        var jeune = await jeuneProfileService.TryGetByUserIdAsync(suiviUserId, cancellationToken);
        if (jeune is null)
            return [];

        return await LoadAcceptationsEnAttenteAsync([jeune.Id], cancellationToken);
    }

    public async Task<bool> ValiderAcceptationAsync(string coachUserId, int missionAcceptationId, CancellationToken cancellationToken = default)
    {
        return await DeciderAcceptationAsync(coachUserId, missionAcceptationId, valider: true, cancellationToken);
    }

    public async Task<bool> RefuserAcceptationAsync(string coachUserId, int missionAcceptationId, CancellationToken cancellationToken = default)
    {
        return await DeciderAcceptationAsync(coachUserId, missionAcceptationId, valider: false, cancellationToken);
    }

    public async Task<IReadOnlyList<MissionResumeView>> GetMesMissionsPublieesAsync(string particulierUserId, CancellationToken cancellationToken = default)
    {
        var particulier = await particulierProfileService.TryGetByUserIdAsync(particulierUserId, cancellationToken);
        if (particulier is null)
            return [];

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Missions.AsNoTracking()
            .Where(m => m.ParticulierProfileId == particulier.Id)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new MissionResumeView(m.Id, m.Titre, m.Lieu, m.Statut, m.Difficulte, m.RemunerationMontant, m.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private async Task<bool> DeciderAcceptationAsync(
        string coachUserId,
        int missionAcceptationId,
        bool valider,
        CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var acceptation = await db.MissionAcceptations
            .Include(a => a.Mission)
            .FirstOrDefaultAsync(a => a.Id == missionAcceptationId, cancellationToken);

        if (acceptation is null || acceptation.Statut != MissionAcceptationStatut.EnAttenteValidationCoach)
            return false;

        var jeune = await jeuneProfileService.TryGetByIdAsync(acceptation.JeuneProfileId, cancellationToken);
        if (jeune is null)
            return false;

        var autorise = await coachingService.GetSuiviUserIdSiAutoriseAsync(jeune.UserId, coachUserId, cancellationToken);
        if (autorise is null)
            return false;

        var now = DateTimeOffset.UtcNow;
        acceptation.CoachUserId = coachUserId;
        acceptation.DecideeLe = now;

        if (valider)
        {
            acceptation.Statut = MissionAcceptationStatut.ValideeParCoach;
            acceptation.Mission.Statut = MissionStatut.Attribuee;
        }
        else
        {
            acceptation.Statut = MissionAcceptationStatut.RefuseeParCoach;
            acceptation.Mission.Statut = MissionStatut.Disponible;
        }

        await db.SaveChangesAsync(cancellationToken);

        // Notification in-app uniquement après validation réussie (pas de notif sur refus).
        if (valider)
        {
            var particulier = await particulierProfileService.TryGetByIdAsync(
                acceptation.Mission.ParticulierProfileId, cancellationToken);
            if (particulier is not null)
            {
                var jeuneNom = $"{jeune.Prenoms} {jeune.Nom}".Trim();
                await notificationService.CreerAsync(
                    particulier.UserId,
                    "Mission confirmée",
                    $"{jeuneNom} a été confirmé(e) pour réaliser la mission « {acceptation.Mission.Titre} ».",
                    "/particulier/mes-missions",
                    "Missions.MissionValidee",
                    cancellationToken);
            }
        }

        return true;
    }

    private async Task<HashSet<int>> GetJeuneProfileIdsSuivisActifsAsync(string coachUserId, CancellationToken cancellationToken)
    {
        var liens = await coachingService.GetLiensPourCoachAsync(coachUserId, cancellationToken);
        var ids = new HashSet<int>();
        foreach (var lien in liens.Where(l => l.Statut == LienCoachingStatut.Actif))
        {
            var jeune = await jeuneProfileService.TryGetByUserIdAsync(lien.SuiviUserId, cancellationToken);
            if (jeune is not null)
                ids.Add(jeune.Id);
        }

        return ids;
    }

    private async Task<IReadOnlyList<MissionAcceptationView>> LoadAcceptationsEnAttenteAsync(
        IEnumerable<int> jeuneProfileIds,
        CancellationToken cancellationToken)
    {
        var idSet = jeuneProfileIds.ToHashSet();
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var acceptations = await db.MissionAcceptations.AsNoTracking()
            .Include(a => a.Mission)
            .Where(a => idSet.Contains(a.JeuneProfileId)
                && a.Statut == MissionAcceptationStatut.EnAttenteValidationCoach)
            .OrderBy(a => a.AccepteeLe)
            .ToListAsync(cancellationToken);

        var views = new List<MissionAcceptationView>(acceptations.Count);
        foreach (var a in acceptations)
        {
            var jeune = await jeuneProfileService.TryGetByIdAsync(a.JeuneProfileId, cancellationToken);
            if (jeune is null)
                continue;

            views.Add(new MissionAcceptationView(
                a.Id,
                a.MissionId,
                a.Mission.Titre,
                a.JeuneProfileId,
                jeune.Nom,
                jeune.Prenoms,
                a.Statut,
                a.Mission.Statut,
                a.AccepteeLe,
                a.DecideeLe));
        }

        return views;
    }
}
