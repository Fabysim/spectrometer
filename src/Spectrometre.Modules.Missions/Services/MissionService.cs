using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Modules;
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
    ICoachSubjectResolver coachSubjectResolver,
    INotificationService notificationService,
    ICharteService charteService,
    UserManager<ApplicationUser> userManager) : IMissionService
{
    public async Task<int?> PublierMissionAsync(string particulierUserId, PublierMissionInput input, CancellationToken cancellationToken = default)
    {
        var particulier = await particulierProfileService.TryGetByUserIdAsync(particulierUserId, cancellationToken);
        if (particulier is null)
            return null;

        if (!TryNormalizePublication(input, out var titre, out var description))
            return null;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var mission = new Mission
        {
            ParticulierProfileId = particulier.Id,
            Description = description,
            Statut = MissionStatut.EnAttenteModeration,
        };
        ApplyPublicationFields(mission, input, titre, description);
        db.Missions.Add(mission);
        await db.SaveChangesAsync(cancellationToken);
        return mission.Id;
    }

    public async Task<IReadOnlyList<MissionDetailView>> GetMissionsEnAttenteModerationAsync(
        string coachUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await EstCoachModerateurAsync(coachUserId, cancellationToken))
            return [];

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var enAttente = await (
            from m in db.Missions.AsNoTracking()
            where m.Statut == MissionStatut.EnAttenteModeration
            join p in db.ParticulierProfiles.AsNoTracking() on m.ParticulierProfileId equals p.Id into gj
            from p in gj.DefaultIfEmpty()
            orderby m.CreatedAt
            select new { Mission = m, Particulier = p }).ToListAsync(cancellationToken);

        if (enAttente.Count == 0)
            return [];

        var particulierIds = enAttente.Select(x => x.Mission.ParticulierProfileId).Distinct().ToList();
        var historiques = await db.Missions.AsNoTracking()
            .Where(m => particulierIds.Contains(m.ParticulierProfileId))
            .GroupBy(m => m.ParticulierProfileId)
            .Select(g => new
            {
                ParticulierProfileId = g.Key,
                Total = g.Count(),
                Annulees = g.Count(m => m.Statut == MissionStatut.Annulee),
            })
            .ToListAsync(cancellationToken);
        var histoParParticulier = historiques.ToDictionary(h => h.ParticulierProfileId);

        var userIds = enAttente
            .Where(x => x.Particulier is not null)
            .Select(x => x.Particulier!.UserId)
            .Distinct()
            .ToList();
        var contacts = userIds.Count == 0
            ? new Dictionary<string, (string? Email, string? PhoneNumber)>()
            : await userManager.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(
                    u => u.Id,
                    u => (u.Email, u.PhoneNumber),
                    cancellationToken);

        return enAttente.Select(x =>
        {
            histoParParticulier.TryGetValue(x.Mission.ParticulierProfileId, out var histo);
            var total = histo?.Total ?? 0;
            var dejaPubliees = Math.Max(0, total - 1);
            contacts.TryGetValue(x.Particulier?.UserId ?? "", out var contact);
            return ToDetailView(
                x.Mission,
                x.Particulier?.Prenoms ?? "",
                x.Particulier?.Nom ?? "",
                dejaPubliees,
                histo?.Annulees ?? 0,
                contact.Email,
                contact.PhoneNumber);
        }).ToList();
    }

    public async Task<bool> ValiderPublicationAsync(
        string coachUserId,
        int missionId,
        CancellationToken cancellationToken = default)
    {
        if (!await EstCoachModerateurAsync(coachUserId, cancellationToken))
            return false;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var mission = await db.Missions.FirstOrDefaultAsync(m => m.Id == missionId, cancellationToken);
        if (mission is null || mission.Statut != MissionStatut.EnAttenteModeration)
            return false;

        mission.Statut = MissionStatut.Disponible;
        mission.MotifAnnulation = null;
        await db.SaveChangesAsync(cancellationToken);
        await NotifierParticulierPublicationAsync(mission, validee: true, cancellationToken);
        return true;
    }

    public async Task<bool> RefuserPublicationAsync(
        string coachUserId,
        int missionId,
        string motif,
        CancellationToken cancellationToken = default)
    {
        if (!await EstCoachModerateurAsync(coachUserId, cancellationToken))
            return false;

        if (string.IsNullOrWhiteSpace(motif))
            return false;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var mission = await db.Missions.FirstOrDefaultAsync(m => m.Id == missionId, cancellationToken);
        if (mission is null || mission.Statut != MissionStatut.EnAttenteModeration)
            return false;

        mission.Statut = MissionStatut.Annulee;
        mission.MotifAnnulation = motif.Trim();
        if (mission.MotifAnnulation.Length > 2000)
            mission.MotifAnnulation = mission.MotifAnnulation[..2000];
        await db.SaveChangesAsync(cancellationToken);
        await NotifierParticulierPublicationAsync(mission, validee: false, cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<MissionResumeView>> GetMissionsDisponiblesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Missions.AsNoTracking()
            .Where(m => m.Statut == MissionStatut.Disponible)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new MissionResumeView(
                m.Id,
                m.Titre,
                m.Categorie,
                m.Lieu,
                m.Statut,
                m.Difficulte,
                m.NiveauEncadrement,
                m.RemunerationMontant,
                m.PresenceEscaliers,
                m.PresenceAnimaux,
                m.PortDeCharge,
                m.AccesDifficile,
                m.RisqueParticulier,
                m.CreatedAt,
                null,
                null,
                null))
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

        return await MapToJeuneViewsAsync(db, rows, cancellationToken);
    }

    public async Task<bool> AccepterMissionAsync(string jeuneUserId, int missionId, CancellationToken cancellationToken = default)
    {
        var jeune = await jeuneProfileService.TryGetByUserIdAsync(jeuneUserId, cancellationToken);
        if (jeune is null)
            return false;

        // Garde charte : en rejoignant la plateforme, le prestataire s'engage à respecter la charte.
        // Les jeunes déjà présents en base de développement sans CharteAcceptation ne pourront plus
        // accepter de mission tant qu'un rattrapage n'aura pas été décidé (hors de ce cycle).
        if (!await charteService.EstAccepteeAsync(jeuneUserId, cancellationToken))
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

        var mission = await db.Missions.AsNoTracking()
            .FirstAsync(m => m.Id == missionId, cancellationToken);
        await NotifierCoachsDemandeAcceptationAsync(jeune, mission, cancellationToken);
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

    public async Task<bool> RetirerCandidatureAsync(
        string jeuneUserId,
        int missionAcceptationId,
        CancellationToken cancellationToken = default)
    {
        var jeune = await jeuneProfileService.TryGetByUserIdAsync(jeuneUserId, cancellationToken);
        if (jeune is null)
            return false;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var acceptation = await db.MissionAcceptations
            .Include(a => a.Mission)
            .FirstOrDefaultAsync(a => a.Id == missionAcceptationId, cancellationToken);

        if (acceptation is null
            || acceptation.JeuneProfileId != jeune.Id
            || acceptation.Statut != MissionAcceptationStatut.EnAttenteValidationCoach
            || acceptation.Mission.Statut != MissionStatut.EnAttenteValidation)
        {
            await tx.RollbackAsync(cancellationToken);
            return false;
        }

        acceptation.Statut = MissionAcceptationStatut.RetireeParJeune;
        acceptation.DecideeLe = DateTimeOffset.UtcNow;
        acceptation.Mission.Statut = MissionStatut.Disponible;
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        await NotifierCoachCandidatureRetireeAsync(jeune, acceptation.Mission, cancellationToken);
        return true;
    }

    public async Task<bool> MarquerTermineeAsync(string jeuneUserId, int missionAcceptationId, CancellationToken cancellationToken = default)
    {
        var jeune = await jeuneProfileService.TryGetByUserIdAsync(jeuneUserId, cancellationToken);
        if (jeune is null)
            return false;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var acceptation = await db.MissionAcceptations
            .Include(a => a.Mission)
            .FirstOrDefaultAsync(a => a.Id == missionAcceptationId, cancellationToken);

        if (acceptation is null)
            return false;

        if (acceptation.JeuneProfileId != jeune.Id)
            return false;

        if (acceptation.Statut != MissionAcceptationStatut.ValideeParCoach)
            return false;

        if (acceptation.Mission.Statut != MissionStatut.Attribuee)
            return false;

        acceptation.Mission.Statut = MissionStatut.Terminee;
        await db.SaveChangesAsync(cancellationToken);

        var particulier = await particulierProfileService.TryGetByIdAsync(
            acceptation.Mission.ParticulierProfileId, cancellationToken);
        if (particulier is not null)
        {
            var titreMission = MissionDisplay.TitreAffiche(
                acceptation.Mission.Categorie, acceptation.Mission.Titre);
            await notificationService.CreerAsync(
                particulier.UserId,
                "Mission terminée",
                $"La mission « {titreMission} » est terminée. Vous pouvez laisser un court retour.",
                "/particulier/mes-missions",
                "Missions.MissionTerminee",
                cancellationToken);
        }

        return true;
    }

    public Task<IReadOnlyList<MissionJeuneView>> GetMissionsTermineesPourJeuneSuiviAsync(
        string coachUserId,
        string suiviUserId,
        CancellationToken cancellationToken = default) =>
        LoadMissionsPourJeuneSuiviAsync(coachUserId, suiviUserId, MissionStatut.Terminee, cancellationToken);

    public Task<IReadOnlyList<MissionJeuneView>> GetMissionsEnCoursPourJeuneSuiviAsync(
        string coachUserId,
        string suiviUserId,
        CancellationToken cancellationToken = default) =>
        LoadMissionsPourJeuneSuiviAsync(coachUserId, suiviUserId, MissionStatut.Attribuee, cancellationToken);

    private async Task<IReadOnlyList<MissionJeuneView>> LoadMissionsPourJeuneSuiviAsync(
        string coachUserId,
        string suiviUserId,
        MissionStatut missionStatut,
        CancellationToken cancellationToken)
    {
        var autorise = await coachingService.GetSuiviUserIdSiAutoriseAsync(suiviUserId, coachUserId, cancellationToken);
        if (autorise is null)
            return [];

        var jeune = await jeuneProfileService.TryGetByUserIdAsync(suiviUserId, cancellationToken);
        if (jeune is null)
            return [];

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.MissionAcceptations.AsNoTracking()
            .Include(a => a.Mission)
            .Where(a => a.JeuneProfileId == jeune.Id
                && a.Statut == MissionAcceptationStatut.ValideeParCoach
                && a.Mission.Statut == missionStatut)
            .OrderByDescending(a => a.DecideeLe ?? a.AccepteeLe)
            .ToListAsync(cancellationToken);

        return await MapToJeuneViewsAsync(db, rows, cancellationToken);
    }

    public async Task<IReadOnlyList<MissionResumeView>> GetMesMissionsPublieesAsync(string particulierUserId, CancellationToken cancellationToken = default)
    {
        var particulier = await particulierProfileService.TryGetByUserIdAsync(particulierUserId, cancellationToken);
        if (particulier is null)
            return [];

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var missions = await db.Missions.AsNoTracking()
            .Include(m => m.Acceptations)
            .Where(m => m.ParticulierProfileId == particulier.Id)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        var result = new List<MissionResumeView>(missions.Count);
        foreach (var m in missions)
        {
            var accValidee = TryGetAcceptationValideeParCoach(m.Acceptations);
            var accAnnuleeParParticulier = m.Acceptations.FirstOrDefault(
                a => a.Statut == MissionAcceptationStatut.AnnuleeParParticulier);
            var accPourPrenom = accValidee ?? accAnnuleeParParticulier;
            int? acceptationIdEval = m.Statut == MissionStatut.Terminee ? accValidee?.Id : null;
            string? jeunePrenom = null;

            if ((m.Statut is MissionStatut.Attribuee or MissionStatut.Terminee or MissionStatut.Annulee)
                && accPourPrenom is not null)
            {
                var jeune = await jeuneProfileService.TryGetByIdAsync(accPourPrenom.JeuneProfileId, cancellationToken);
                jeunePrenom = jeune?.Prenoms;
            }

            result.Add(new MissionResumeView(
                m.Id,
                m.Titre,
                m.Categorie,
                m.Lieu,
                m.Statut,
                m.Difficulte,
                m.NiveauEncadrement,
                m.RemunerationMontant,
                m.PresenceEscaliers,
                m.PresenceAnimaux,
                m.PortDeCharge,
                m.AccesDifficile,
                m.RisqueParticulier,
                m.CreatedAt,
                acceptationIdEval,
                jeunePrenom,
                m.MotifAnnulation));
        }

        return result;
    }

    public async Task<bool> AnnulerMissionAsync(string particulierUserId, int missionId, CancellationToken cancellationToken = default)
    {
        var particulier = await particulierProfileService.TryGetByUserIdAsync(particulierUserId, cancellationToken);
        if (particulier is null)
            return false;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var mission = await db.Missions.FirstOrDefaultAsync(m => m.Id == missionId, cancellationToken);
        if (mission is null)
            return false;

        if (mission.ParticulierProfileId != particulier.Id)
            return false;

        if (mission.Statut is not (MissionStatut.Disponible or MissionStatut.EnAttenteModeration))
            return false;

        mission.Statut = MissionStatut.Annulee;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> AnnulerMissionAttribueeAsync(
        string particulierUserId,
        int missionId,
        string motif,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(motif))
            return false;

        var particulier = await particulierProfileService.TryGetByUserIdAsync(particulierUserId, cancellationToken);
        if (particulier is null)
            return false;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var mission = await db.Missions
            .Include(m => m.Acceptations)
            .FirstOrDefaultAsync(m => m.Id == missionId, cancellationToken);
        if (mission is null)
            return false;

        if (mission.ParticulierProfileId != particulier.Id)
            return false;

        if (mission.Statut != MissionStatut.Attribuee)
            return false;

        var acceptation = TryGetAcceptationValideeParCoach(mission.Acceptations);
        if (acceptation is null)
            return false;

        var jeune = await jeuneProfileService.TryGetByIdAsync(acceptation.JeuneProfileId, cancellationToken);
        if (jeune is null)
            return false;

        var motifNorme = motif.Trim();
        if (motifNorme.Length > 2000)
            motifNorme = motifNorme[..2000];

        mission.Statut = MissionStatut.Annulee;
        mission.MotifAnnulation = motifNorme;
        acceptation.Statut = MissionAcceptationStatut.AnnuleeParParticulier;
        await db.SaveChangesAsync(cancellationToken);

        await NotifierAnnulationMissionAttribueeAsync(jeune, mission, motifNorme, cancellationToken);
        return true;
    }

    public async Task<MissionDetailView?> TryGetMissionPourModificationAsync(
        string particulierUserId,
        int missionId,
        CancellationToken cancellationToken = default)
    {
        var mission = await TryGetMissionProprietaireEditableAsync(particulierUserId, missionId, cancellationToken);
        if (mission is null)
            return null;

        return ToDetailView(mission);
    }

    public async Task<bool> ModifierMissionAsync(
        string particulierUserId,
        int missionId,
        PublierMissionInput input,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizePublication(input, out var titre, out var description))
            return false;

        var particulier = await particulierProfileService.TryGetByUserIdAsync(particulierUserId, cancellationToken);
        if (particulier is null)
            return false;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var mission = await db.Missions.FirstOrDefaultAsync(m => m.Id == missionId, cancellationToken);
        if (mission is null)
            return false;

        if (mission.ParticulierProfileId != particulier.Id)
            return false;

        if (mission.Statut is not (MissionStatut.Disponible or MissionStatut.EnAttenteModeration))
            return false;

        ApplyPublicationFields(mission, input, titre, description);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> SignalerProblemeAsync(
        string particulierUserId,
        int missionId,
        string? message,
        CancellationToken cancellationToken = default) =>
        NotifierCoachDepuisParticulierAsync(
            particulierUserId, missionId, message, estProbleme: true, cancellationToken);

    public Task<bool> DemanderContactParticulierAsync(
        string particulierUserId,
        int missionId,
        string? message,
        CancellationToken cancellationToken = default) =>
        NotifierCoachDepuisParticulierAsync(
            particulierUserId, missionId, message, estProbleme: false, cancellationToken);

    public Task<bool> DemanderContactCoachAsync(
        string jeuneUserId,
        int missionAcceptationId,
        string? message,
        CancellationToken cancellationToken = default) =>
        NotifierCoachDepuisJeuneAsync(
            jeuneUserId, missionAcceptationId, message, estProbleme: false, cancellationToken);

    public Task<bool> SignalerProblemeJeuneAsync(
        string jeuneUserId,
        int missionAcceptationId,
        string? message,
        CancellationToken cancellationToken = default) =>
        NotifierCoachDepuisJeuneAsync(
            jeuneUserId, missionAcceptationId, message, estProbleme: true, cancellationToken);

    /// <summary>
    /// Canal unique vers le coach (notification in-app). Pas de fil, pas de coordonnées exposées.
    /// <paramref name="estProbleme"/> distingue l'incident du rappel simple (TypeCodes distincts).
    /// </summary>
    private async Task<bool> NotifierCoachDepuisParticulierAsync(
        string particulierUserId,
        int missionId,
        string? message,
        bool estProbleme,
        CancellationToken cancellationToken)
    {
        var particulier = await particulierProfileService.TryGetByUserIdAsync(particulierUserId, cancellationToken);
        if (particulier is null)
            return false;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var mission = await db.Missions
            .Include(m => m.Acceptations)
            .FirstOrDefaultAsync(m => m.Id == missionId, cancellationToken);
        if (mission is null)
            return false;

        if (mission.ParticulierProfileId != particulier.Id)
            return false;

        if (mission.Statut != MissionStatut.Attribuee)
            return false;

        var acceptation = TryGetAcceptationValideeParCoach(mission.Acceptations);
        if (acceptation is null)
            return false;

        var jeune = await jeuneProfileService.TryGetByIdAsync(acceptation.JeuneProfileId, cancellationToken);
        if (jeune is null)
            return false;

        return await EnvoyerNotificationCoachAsync(
            jeune.UserId, jeune.Prenoms, jeune.Nom, mission, message, estProbleme, depuisJeune: false, cancellationToken);
    }

    private async Task<bool> NotifierCoachDepuisJeuneAsync(
        string jeuneUserId,
        int missionAcceptationId,
        string? message,
        bool estProbleme,
        CancellationToken cancellationToken)
    {
        var jeune = await jeuneProfileService.TryGetByUserIdAsync(jeuneUserId, cancellationToken);
        if (jeune is null)
            return false;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var acceptation = await db.MissionAcceptations
            .Include(a => a.Mission)
            .FirstOrDefaultAsync(a => a.Id == missionAcceptationId, cancellationToken);
        if (acceptation is null)
            return false;

        if (acceptation.JeuneProfileId != jeune.Id)
            return false;

        if (acceptation.Statut != MissionAcceptationStatut.ValideeParCoach)
            return false;

        // Contact (et signalement) possibles pendant la mission et juste après — pas avant attribution.
        if (acceptation.Mission.Statut is not (MissionStatut.Attribuee or MissionStatut.Terminee))
            return false;

        return await EnvoyerNotificationCoachAsync(
            jeune.UserId, jeune.Prenoms, jeune.Nom, acceptation.Mission, message, estProbleme, depuisJeune: true, cancellationToken);
    }

    private async Task<bool> EnvoyerNotificationCoachAsync(
        string jeuneUserId,
        string jeunePrenoms,
        string jeuneNom,
        Mission mission,
        string? message,
        bool estProbleme,
        bool depuisJeune,
        CancellationToken cancellationToken)
    {
        var coachUserId = await FindCoachReferentAsync(jeuneUserId, cancellationToken);
        if (coachUserId is null)
            return false;

        var titreMission = MissionDisplay.TitreAffiche(mission.Categorie, mission.Titre);
        var detail = string.IsNullOrWhiteSpace(message)
            ? "Aucun détail supplémentaire."
            : message.Trim();
        var auteur = depuisJeune ? "Le jeune" : "Le particulier";
        var identiteJeune = $"{jeunePrenoms} {jeuneNom}".Trim();

        string titre;
        string corps;
        string typeCode;
        if (estProbleme)
        {
            titre = "Problème signalé pendant une mission";
            corps = $"{auteur} signale un problème sur la mission « {titreMission} » (jeune : {identiteJeune}). {detail}";
            typeCode = "Missions.ProblemeSignale";
        }
        else
        {
            titre = "Demande de contact — mission";
            corps = $"{auteur} souhaite être contacté au sujet de la mission « {titreMission} » (jeune : {identiteJeune}). {detail}";
            typeCode = "Missions.DemandeContact";
        }

        await notificationService.CreerAsync(
            coachUserId,
            titre,
            corps,
            $"/coach/suivis/{jeuneUserId}/missions",
            typeCode,
            cancellationToken);

        return true;
    }

    /// <summary>
    /// Acceptation confirmée par le coach — même critère que SignalerProbleme / évaluation / suivi coach.
    /// </summary>
    private static MissionAcceptation? TryGetAcceptationValideeParCoach(IEnumerable<MissionAcceptation> acceptations) =>
        acceptations.FirstOrDefault(a => a.Statut == MissionAcceptationStatut.ValideeParCoach);

    private static bool TryNormalizePublication(PublierMissionInput input, out string titre, out string description)
    {
        titre = "";
        description = "";
        if (string.IsNullOrWhiteSpace(input.Description))
            return false;

        if (input.Categorie == MissionCategorie.Autre && string.IsNullOrWhiteSpace(input.Titre))
            return false;

        titre = string.IsNullOrWhiteSpace(input.Titre) ? "" : input.Titre.Trim();
        description = input.Description.Trim();
        return true;
    }

    private static void ApplyPublicationFields(Mission mission, PublierMissionInput input, string titre, string description)
    {
        mission.Categorie = input.Categorie;
        mission.Titre = titre;
        mission.Description = description;
        mission.Lieu = string.IsNullOrWhiteSpace(input.Lieu) ? null : input.Lieu.Trim();
        mission.DureeEstimee = string.IsNullOrWhiteSpace(input.DureeEstimee) ? null : input.DureeEstimee.Trim();
        mission.Difficulte = input.Difficulte;
        mission.RemunerationMontant = input.RemunerationMontant;
        mission.CompetencesTravaillees = string.IsNullOrWhiteSpace(input.CompetencesTravaillees) ? null : input.CompetencesTravaillees.Trim();
        mission.NiveauEncadrement = input.NiveauEncadrement;
        mission.PresenceEscaliers = input.PresenceEscaliers;
        mission.PresenceAnimaux = input.PresenceAnimaux;
        mission.PortDeCharge = input.PortDeCharge;
        mission.AccesDifficile = input.AccesDifficile;
        mission.RisqueParticulier = string.IsNullOrWhiteSpace(input.RisqueParticulier) ? null : input.RisqueParticulier.Trim();
    }

    private async Task<Mission?> TryGetMissionProprietaireEditableAsync(
        string particulierUserId,
        int missionId,
        CancellationToken cancellationToken)
    {
        var particulier = await particulierProfileService.TryGetByUserIdAsync(particulierUserId, cancellationToken);
        if (particulier is null)
            return null;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var mission = await db.Missions.AsNoTracking().FirstOrDefaultAsync(m => m.Id == missionId, cancellationToken);
        if (mission is null)
            return null;

        if (mission.ParticulierProfileId != particulier.Id)
            return null;

        if (mission.Statut is not (MissionStatut.Disponible or MissionStatut.EnAttenteModeration))
            return null;

        return mission;
    }

    /// <summary>
    /// Tout coach authentifié : profil coach déjà créé, ou au moins un lien de suivi
    /// (les coachs de test invitent un jeune sans forcément remplir le profil).
    /// Pas de lien avec le particulier — file d'attente partagée.
    /// </summary>
    private async Task<bool> EstCoachModerateurAsync(string userId, CancellationToken cancellationToken)
    {
        if (await coachSubjectResolver.TryGetCoachProfileIdAsync(userId, cancellationToken) is not null)
            return true;

        var liens = await coachingService.GetLiensPourCoachAsync(userId, cancellationToken);
        return liens.Count > 0;
    }

    private static MissionDetailView ToDetailView(
        Mission mission,
        string particulierPrenoms = "",
        string particulierNom = "",
        int missionsDejaPublieesCount = 0,
        int missionsRefuseesOuAnnuleesCount = 0,
        string? particulierEmail = null,
        string? particulierTelephone = null) =>
        new(
            mission.Id,
            mission.Titre,
            mission.Categorie,
            mission.Description,
            mission.Lieu,
            mission.DureeEstimee,
            mission.Difficulte,
            mission.NiveauEncadrement,
            mission.RemunerationMontant,
            mission.CompetencesTravaillees,
            mission.PresenceEscaliers,
            mission.PresenceAnimaux,
            mission.PortDeCharge,
            mission.AccesDifficile,
            mission.RisqueParticulier,
            mission.Statut,
            mission.CreatedAt,
            particulierPrenoms,
            particulierNom,
            missionsDejaPublieesCount,
            missionsRefuseesOuAnnuleesCount,
            particulierEmail,
            particulierTelephone);

    private async Task NotifierAnnulationMissionAttribueeAsync(
        JeuneProfileView jeune,
        Mission mission,
        string motif,
        CancellationToken cancellationToken)
    {
        var titreMission = MissionDisplay.TitreAffiche(mission.Categorie, mission.Titre);
        var jeuneNom = $"{jeune.Prenoms} {jeune.Nom}".Trim();
        var corps = $"La mission « {titreMission} » a été annulée par le particulier. Motif : {motif}";

        await notificationService.CreerAsync(
            jeune.UserId,
            "Mission annulée",
            corps,
            "/jeune/mes-missions",
            "Missions.MissionAnnuleeParParticulier",
            cancellationToken);

        var coachUserId = await FindCoachReferentAsync(jeune.UserId, cancellationToken);
        if (coachUserId is null)
            return;

        await notificationService.CreerAsync(
            coachUserId,
            "Mission annulée par le particulier",
            $"{jeuneNom} : {corps}",
            $"/coach/suivis/{jeune.UserId}/missions",
            "Missions.MissionAnnuleeParParticulier",
            cancellationToken);
    }

    private async Task<string?> FindCoachReferentAsync(string jeuneUserId, CancellationToken cancellationToken)
    {
        var liens = await coachingService.GetLiensPourSuiviAsync(jeuneUserId, cancellationToken);
        return liens.FirstOrDefault(l => l.Statut == LienCoachingStatut.Actif)?.CoachUserId;
    }

    /// <summary>
    /// Coach référent (lien actif) — même résolution que les autres notifs du fichier.
    /// </summary>
    private async Task NotifierCoachCandidatureRetireeAsync(
        JeuneProfileView jeune,
        Mission mission,
        CancellationToken cancellationToken)
    {
        var coachUserId = await FindCoachReferentAsync(jeune.UserId, cancellationToken);
        if (coachUserId is null)
            return;

        var titreMission = MissionDisplay.TitreAffiche(mission.Categorie, mission.Titre);
        var jeuneNom = $"{jeune.Prenoms} {jeune.Nom}".Trim();
        await notificationService.CreerAsync(
            coachUserId,
            "Candidature retirée",
            $"{jeuneNom} a retiré sa candidature pour « {titreMission} ».",
            $"/coach/suivis/{jeune.UserId}/missions",
            "Missions.CandidatureRetiree",
            cancellationToken);
    }

    /// <summary>
    /// Coachs avec un lien <see cref="LienCoachingStatut.Actif"/>. Un jeune prestataire n'a plus qu'un
    /// seul coach actif à la création de lien ; la boucle Distinct reste pour d'éventuels liens
    /// historiques encore multiples.
    /// </summary>
    private async Task NotifierCoachsDemandeAcceptationAsync(
        JeuneProfileView jeune,
        Mission mission,
        CancellationToken cancellationToken)
    {
        var coachUserIds = (await coachingService.GetLiensPourSuiviAsync(jeune.UserId, cancellationToken))
            .Where(l => l.Statut == LienCoachingStatut.Actif)
            .Select(l => l.CoachUserId)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (coachUserIds.Count == 0)
            return;

        var titreMission = MissionDisplay.TitreAffiche(mission.Categorie, mission.Titre);
        var jeuneNom = $"{jeune.Prenoms} {jeune.Nom}".Trim();
        var lien = $"/coach/suivis/{jeune.UserId}/missions";
        foreach (var coachUserId in coachUserIds)
        {
            await notificationService.CreerAsync(
                coachUserId,
                "Demande de mission à valider",
                $"{jeuneNom} a accepté la mission « {titreMission} ». Merci de valider ou de refuser.",
                lien,
                "Missions.DemandeAcceptationEnAttente",
                cancellationToken);
        }
    }

    private async Task<IReadOnlyList<MissionJeuneView>> MapToJeuneViewsAsync(
        MissionsDbContext db,
        List<MissionAcceptation> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
            return [];

        var particulierIds = rows.Select(a => a.Mission.ParticulierProfileId).Distinct().ToList();
        var prenoms = await db.ParticulierProfiles.AsNoTracking()
            .Where(p => particulierIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Prenoms, cancellationToken);

        return rows.Select(a =>
        {
            prenoms.TryGetValue(a.Mission.ParticulierProfileId, out var prenom);
            return ToMissionJeuneView(a, prenom);
        }).ToList();
    }

    private static MissionJeuneView ToMissionJeuneView(MissionAcceptation a, string? particulierPrenoms) =>
        new(
            a.Id,
            a.MissionId,
            a.Mission.Titre,
            a.Mission.Categorie,
            a.Mission.Lieu,
            a.Mission.Statut,
            a.Statut,
            a.Mission.NiveauEncadrement,
            a.Mission.PresenceEscaliers,
            a.Mission.PresenceAnimaux,
            a.Mission.PortDeCharge,
            a.Mission.AccesDifficile,
            a.Mission.RisqueParticulier,
            a.AccepteeLe,
            a.DecideeLe,
            a.Mission.Statut is MissionStatut.Attribuee or MissionStatut.Terminee
                && !string.IsNullOrWhiteSpace(particulierPrenoms)
                ? particulierPrenoms
                : null);

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

        // Même catégorie Missions (préfixe TypeCode) — TypeCodes distincts par événement métier.
        // Validation : particulier + jeune. Refus : jeune uniquement (le particulier voit juste la
        // mission redevenir Disponible — rien ne « disparaît » de son côté).
        var titreMission = MissionDisplay.TitreAffiche(acceptation.Mission.Categorie, acceptation.Mission.Titre);
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
                    $"{jeuneNom} a été confirmé(e) pour réaliser la mission « {titreMission} ».",
                    "/particulier/mes-missions",
                    "Missions.MissionValidee",
                    cancellationToken);
            }

            await notificationService.CreerAsync(
                jeune.UserId,
                "Mission confirmée",
                $"Ta mission « {titreMission} » a été validée par ton coach.",
                "/jeune/mes-missions",
                "Missions.MissionValidee",
                cancellationToken);
        }
        else
        {
            await notificationService.CreerAsync(
                jeune.UserId,
                "Candidature non retenue",
                $"Ta candidature pour « {titreMission} » n'a pas été retenue. D'autres missions sont disponibles.",
                "/jeune/missions-disponibles",
                "Missions.MissionRefusee",
                cancellationToken);
        }

        return true;
    }

    /// <summary>
    /// Même schéma que <see cref="DeciderAcceptationAsync"/> : <c>CreerAsync</c> après persistance,
    /// TypeCode préfixe <c>Missions.</c> (catégorie déjà au catalogue). Distinct de
    /// <c>Missions.MissionValidee</c> / <c>Missions.MissionRefusee</c> (décision sur une candidature).
    /// </summary>
    private async Task NotifierParticulierPublicationAsync(
        Mission mission,
        bool validee,
        CancellationToken cancellationToken)
    {
        var particulier = await particulierProfileService.TryGetByIdAsync(
            mission.ParticulierProfileId, cancellationToken);
        if (particulier is null)
            return;

        var titreMission = MissionDisplay.TitreAffiche(mission.Categorie, mission.Titre);
        if (validee)
        {
            await notificationService.CreerAsync(
                particulier.UserId,
                "Mission validée",
                $"Votre mission « {titreMission} » a été validée et est maintenant visible des jeunes.",
                "/particulier/mes-missions",
                "Missions.PublicationValidee",
                cancellationToken);
        }
        else
        {
            var motif = string.IsNullOrWhiteSpace(mission.MotifAnnulation)
                ? "Aucune explication fournie."
                : mission.MotifAnnulation;
            await notificationService.CreerAsync(
                particulier.UserId,
                "Mission refusée",
                $"Votre mission « {titreMission} » n'a pas été publiée. Motif : {motif}",
                "/particulier/mes-missions",
                "Missions.PublicationRefusee",
                cancellationToken);
        }
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
                MissionDisplay.TitreAffiche(a.Mission.Categorie, a.Mission.Titre),
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
