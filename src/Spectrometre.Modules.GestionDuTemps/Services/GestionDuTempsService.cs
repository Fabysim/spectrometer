using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Data;
using Spectrometre.Modules.GestionDuTemps.Data;
using Spectrometre.Modules.GestionDuTemps.Entities;

namespace Spectrometre.Modules.GestionDuTemps.Services;

/// <summary>
/// DbContext à schéma fixe instancié via <see cref="IDbContextFactory{TContext}"/> à chaque opération
/// (jamais injecté directement) — même raison que <c>CandidateProfileService</c> : en Blazor Server, une
/// instance partagée pour tout le circuit serait utilisée concurremment par deux gestionnaires
/// d'évènements qui se chevauchent, ce qu'EF Core interdit explicitement.
/// </summary>
public sealed class GestionDuTempsService(
    IDbContextFactory<GestionDuTempsDbContext> dbFactory,
    IDbContextFactory<CoreDbContext> coreDbFactory,
    IAiSynthesisService aiSynthesisService) : IGestionDuTempsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };


    private sealed record TypeSeed(string Cle, string Libelle, TimeOnly Debut, TimeOnly Fin, string Recurrence, int Ordre, int? CompanyId);

    /// <summary>Catégories par défaut reprises telles quelles de <c>GestionDuTempsService.DefaultTypes</c> dans mvp.</summary>
    private static readonly TypeSeed[] DefaultTypes =
    [
        new("sommeil", "Sommeil · Repos", new TimeOnly(22, 0), new TimeOnly(6, 0), "D,L,M,Me,J,V,S", 0, null),
        new("perso", "Personnel", new TimeOnly(7, 0), new TimeOnly(8, 0), "L,M,Me,J,V", 1, null),
        new("pro", "Professionnel", new TimeOnly(8, 0), new TimeOnly(17, 0), "L,M,Me,J,V", 2, null),
        new("admin", "Administratif", new TimeOnly(17, 0), new TimeOnly(18, 0), "L,Me,V", 3, null),
        new("famille", "Familial · affectif", new TimeOnly(18, 0), new TimeOnly(21, 0), "L,M,Me,J,V,S,D", 4, null),
        new("social", "Social", new TimeOnly(21, 0), new TimeOnly(22, 0), "S,D", 5, null),
    ];

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation };

    private static CycleView ToCycleView(Cycle c) => new(c.Id, c.NumeroCycle, c.Statut, c.DemarreLe, c.ClotureLe);

    private async Task VerifierCompanyIdAsync(string userId, int? companyId, CancellationToken cancellationToken)
    {
        if (companyId is null)
            return;

        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        var geree = await coreDb.UserCompanyLinks
            .AsNoTracking()
            .AnyAsync(l => l.UserId == userId && l.CompanyId == companyId, cancellationToken);

        if (!geree)
            throw new InvalidOperationException($"L'entreprise #{companyId} n'est pas gérée par cet utilisateur.");
    }

    // ── Cycles ───────────────────────────────────────────────────────────────

    public async Task<CycleView?> GetCycleActifAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var existant = await db.Cycles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Statut == CycleStatuts.EnCours, cancellationToken);
        return existant is null ? null : ToCycleView(existant);
    }

    public async Task<CycleView> GetOrCreateCycleActifAsync(string userId, CancellationToken cancellationToken = default)
    {
        var existant = await GetCycleActifAsync(userId, cancellationToken);
        if (existant is not null)
            return existant;

        return ToCycleView(await CreerEtSeedCycleAsync(userId, DefaultTypes, cancellationToken));
    }

    public async Task<CycleView> ClotureEtDemarrerNouveauCycleAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var cycleActuel = await db.Cycles.FirstOrDefaultAsync(c => c.UserId == userId && c.Statut == CycleStatuts.EnCours, cancellationToken)
            ?? throw new InvalidOperationException("Aucun cycle en cours à clôturer.");

        // Capturé AVANT la clôture : ce sont ces types-là qui seront recopiés vers le nouveau cycle — les
        // activités, elles, ne sont PAS recopiées (voir Cycle : archivage passif, comme dans mvp).
        var typesASeed = await db.TypesDeTemps.AsNoTracking()
            .Where(t => t.CycleId == cycleActuel.Id)
            .Select(t => new TypeSeed(t.Cle, t.Libelle, t.HeureDebut, t.HeureFin, t.RecurrenceJours, t.OrdreAffichage, t.CompanyId))
            .ToListAsync(cancellationToken);

        cycleActuel.Statut = CycleStatuts.Termine;
        cycleActuel.ClotureLe = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return ToCycleView(await CreerEtSeedCycleAsync(userId, typesASeed, cancellationToken));
    }

    /// <summary>
    /// Insertion en course : deux appels quasi simultanés pour un même utilisateur (deux onglets ouverts au
    /// premier accès) pourraient toutes deux constater l'absence de cycle actif — l'index unique filtré sur
    /// (UserId) WHERE Statut='EnCours' fait alors échouer la seconde en violation d'unicité plutôt qu'en
    /// doublon silencieux ; on retombe simplement sur le cycle déjà créé dans ce cas.
    /// </summary>
    private async Task<Cycle> CreerEtSeedCycleAsync(string userId, IReadOnlyList<TypeSeed> typesASeed, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var maxNumero = await db.Cycles.Where(c => c.UserId == userId).Select(c => (int?)c.NumeroCycle).MaxAsync(cancellationToken) ?? 0;

        var cycle = new Cycle { UserId = userId, NumeroCycle = maxNumero + 1, Statut = CycleStatuts.EnCours };
        db.Cycles.Add(cycle);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            await using var lecture = await dbFactory.CreateDbContextAsync(cancellationToken);
            return await lecture.Cycles.AsNoTracking().FirstAsync(c => c.UserId == userId && c.Statut == CycleStatuts.EnCours, cancellationToken);
        }

        foreach (var t in typesASeed)
        {
            db.TypesDeTemps.Add(new TypeDeTemps
            {
                UserId = userId,
                CycleId = cycle.Id,
                Cle = t.Cle,
                Libelle = t.Libelle,
                HeureDebut = t.Debut,
                HeureFin = t.Fin,
                RecurrenceJours = t.Recurrence,
                OrdreAffichage = t.Ordre,
                CompanyId = t.CompanyId,
            });
        }
        await db.SaveChangesAsync(cancellationToken);

        return cycle;
    }

    // ── Types de temps ───────────────────────────────────────────────────────

    public async Task<IReadOnlyList<TypeDeTempsView>> GetTypesDeTempsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var cycle = await GetCycleActifAsync(userId, cancellationToken);
        if (cycle is null)
            return [];

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var types = await db.TypesDeTemps
            .AsNoTracking()
            .Where(t => t.CycleId == cycle.Id)
            .OrderBy(t => t.OrdreAffichage)
            .ThenBy(t => t.Cle)
            .ToListAsync(cancellationToken);

        return types
            .Select(t => new TypeDeTempsView(t.Id, t.Cle, t.Libelle, t.HeureDebut, t.HeureFin, t.RecurrenceJours, t.OrdreAffichage, t.CompanyId))
            .ToList();
    }

    public async Task UpsertTypeDeTempsAsync(string userId, int? id, string cle, string libelle, TimeOnly heureDebut, TimeOnly heureFin, string recurrenceJours, int ordreAffichage, int? companyId, CancellationToken cancellationToken = default)
    {
        await VerifierCompanyIdAsync(userId, companyId, cancellationToken);
        var cycle = await GetOrCreateCycleActifAsync(userId, cancellationToken);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        TypeDeTemps type;
        if (id is int existingId)
        {
            // Scopé au cycle ACTIF : un type d'un cycle clôturé n'est plus modifiable (archive figée).
            type = await db.TypesDeTemps.FirstOrDefaultAsync(t => t.Id == existingId && t.UserId == userId && t.CycleId == cycle.Id, cancellationToken)
                ?? throw new InvalidOperationException("Type de temps introuvable.");
        }
        else
        {
            type = new TypeDeTemps { UserId = userId, CycleId = cycle.Id, Cle = cle, Libelle = libelle };
            db.TypesDeTemps.Add(type);
        }

        type.Cle = cle;
        type.Libelle = libelle;
        type.HeureDebut = heureDebut;
        type.HeureFin = heureFin;
        type.RecurrenceJours = recurrenceJours;
        type.OrdreAffichage = ordreAffichage;
        type.CompanyId = companyId;
        type.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }

    // ── Activités ────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ActiviteView>> GetActivitesAsync(string userId, int? companyId, bool personnelUniquement, CancellationToken cancellationToken = default)
    {
        var cycle = await GetCycleActifAsync(userId, cancellationToken);
        if (cycle is null)
            return [];

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var query = db.Activites.AsNoTracking().Where(a => a.UserId == userId && a.CycleId == cycle.Id);
        if (companyId is int cid)
            query = query.Where(a => a.CompanyId == cid);
        else if (personnelUniquement)
            query = query.Where(a => a.CompanyId == null);

        var activites = await query
            .OrderBy(a => a.DateActivite).ThenBy(a => a.HeureDebut)
            .ToListAsync(cancellationToken);

        var types = await db.TypesDeTemps.AsNoTracking().Where(t => t.CycleId == cycle.Id).ToDictionaryAsync(t => t.Id, cancellationToken);

        var english = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "fr";
        return activites
            .Select(a =>
            {
                types.TryGetValue(a.TypeDeTempsId, out var type);
                var libelle = type is null ? (english ? "(deleted category)" : "(catégorie supprimée)") : DefaultCategoryLabels.Display(type.Cle, type.Libelle, english);
                var cle = type?.Cle ?? "";
                return new ActiviteView(a.Id, a.TypeDeTempsId, cle, libelle, CouleurCategorie(type?.Cle), a.Nom, a.DateActivite, a.HeureDebut, a.DureeMinutes, a.CompanyId);
            })
            .ToList();
    }

    private static string CouleurCategorie(string? cle) => cle switch
    {
        "sommeil" => "#4a3f6b",
        "perso" => "#345f6d",
        "pro" => "#1e6b4a",
        "admin" => "#7a5c1e",
        "famille" => "#7a3f2d",
        "social" => "#3d4f8a",
        _ => "#6b5248",
    };

    public async Task<int> CreateActiviteAsync(string userId, int typeDeTempsId, string nom, DateOnly dateActivite, TimeOnly heureDebut, int dureeMinutes, int? companyId, CancellationToken cancellationToken = default)
    {
        await VerifierCompanyIdAsync(userId, companyId, cancellationToken);
        var cycle = await GetOrCreateCycleActifAsync(userId, cancellationToken);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var typeExiste = await db.TypesDeTemps.AsNoTracking().AnyAsync(t => t.Id == typeDeTempsId && t.CycleId == cycle.Id, cancellationToken);
        if (!typeExiste)
            throw new InvalidOperationException("Type de temps introuvable dans le cycle actif.");

        var activite = new Activite
        {
            UserId = userId,
            CycleId = cycle.Id,
            TypeDeTempsId = typeDeTempsId,
            Nom = nom,
            DateActivite = dateActivite,
            HeureDebut = heureDebut,
            DureeMinutes = dureeMinutes,
            CompanyId = companyId,
        };
        db.Activites.Add(activite);
        await db.SaveChangesAsync(cancellationToken);

        // Créé juste après, une fois l'identifiant connu — comme InsertActiviteAsync dans mvp : une
        // activité sans ligne KanbanStatut ne devrait jamais exister.
        db.KanbanStatuts.Add(new KanbanStatut { ActiviteId = activite.Id, Statut = KanbanColonnes.AFaire });
        await db.SaveChangesAsync(cancellationToken);

        return activite.Id;
    }

    public async Task UpdateActiviteAsync(string userId, int activiteId, string nom, DateOnly dateActivite, TimeOnly heureDebut, int dureeMinutes, int? companyId, int? typeDeTempsId = null, CancellationToken cancellationToken = default)
    {
        await VerifierCompanyIdAsync(userId, companyId, cancellationToken);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var activite = await db.Activites.FirstOrDefaultAsync(a => a.Id == activiteId && a.UserId == userId, cancellationToken);
        if (activite is null) return;

        if (typeDeTempsId is int tid)
        {
            var typeExiste = await db.TypesDeTemps.AsNoTracking().AnyAsync(t => t.Id == tid && t.CycleId == activite.CycleId, cancellationToken);
            if (!typeExiste)
                throw new InvalidOperationException("Type de temps introuvable dans le cycle actif.");
            activite.TypeDeTempsId = tid;
        }

        activite.Nom = nom;
        activite.DateActivite = dateActivite;
        activite.HeureDebut = heureDebut;
        activite.DureeMinutes = Math.Clamp(dureeMinutes, 5, 720);
        activite.CompanyId = companyId;
        activite.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteActiviteAsync(string userId, int activiteId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var activite = await db.Activites.FirstOrDefaultAsync(a => a.Id == activiteId && a.UserId == userId, cancellationToken);
        if (activite is null) return;

        // Le KanbanStatut associé est supprimé par la cascade déclarée au niveau base (voir
        // GestionDuTempsDbContext) — pas besoin de le charger/supprimer explicitement ici.
        db.Activites.Remove(activite);
        await db.SaveChangesAsync(cancellationToken);
    }

    // ── Kanban & minuteur ────────────────────────────────────────────────────

    public async Task<IReadOnlyList<KanbanCarteView>> GetKanbanAsync(string userId, CancellationToken cancellationToken = default)
    {
        var cycle = await GetCycleActifAsync(userId, cancellationToken);
        if (cycle is null)
            return [];

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var activites = await db.Activites.AsNoTracking().Where(a => a.CycleId == cycle.Id).ToListAsync(cancellationToken);
        var types = await db.TypesDeTemps.AsNoTracking().Where(t => t.CycleId == cycle.Id).ToDictionaryAsync(t => t.Id, cancellationToken);
        var activiteIds = activites.Select(a => a.Id).ToList();
        var statuts = await db.KanbanStatuts.AsNoTracking().Where(s => activiteIds.Contains(s.ActiviteId)).ToDictionaryAsync(s => s.ActiviteId, cancellationToken);

        var maintenant = DateTimeOffset.UtcNow;
        var english = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "fr";
        return activites
            .Select(a =>
            {
                types.TryGetValue(a.TypeDeTempsId, out var type);
                var statut = statuts.GetValueOrDefault(a.Id) ?? new KanbanStatut { ActiviteId = a.Id };
                var tempsReelMs = KanbanTimer.GetElapsedMs(statut, maintenant);
                var libelle = type is null ? (english ? "(deleted category)" : "(catégorie supprimée)") : DefaultCategoryLabels.Display(type.Cle, type.Libelle, english);
                return new KanbanCarteView(
                    a.Id, a.TypeDeTempsId, a.Nom, libelle, CouleurCategorie(type?.Cle), a.DureeMinutes, a.CompanyId,
                    statut.Statut, tempsReelMs, KanbanTimer.IsOvertime(tempsReelMs, a.DureeMinutes),
                    statut.UpdatedAt, a.CreatedAt);
            })
            .OrderBy(c => ColonneOrdre(c.Statut)).ThenBy(c => c.Nom)
            .ToList();
    }

    private static int ColonneOrdre(string statut) => statut switch
    {
        KanbanColonnes.AFaire => 0,
        KanbanColonnes.EnCours => 1,
        _ => 2,
    };

    public Task MarquerDebutAsync(string userId, int activiteId, CancellationToken cancellationToken = default) =>
        DefinirStatutMinuteurAsync(userId, activiteId, KanbanColonnes.EnCours, demarrerMinuteur: true, finaliser: false, cancellationToken);

    public Task MarquerPauseAsync(string userId, int activiteId, CancellationToken cancellationToken = default) =>
        MettreEnPauseAsync(userId, activiteId, cancellationToken);

    public Task MarquerTermineAsync(string userId, int activiteId, CancellationToken cancellationToken = default) =>
        DefinirStatutMinuteurAsync(userId, activiteId, KanbanColonnes.Termine, demarrerMinuteur: false, finaliser: true, cancellationToken);

    /// <summary>Même logique que <c>PauseTimerAsync</c> dans mvp : accumule l'écoulé si le minuteur tournait, puis repasse en "À faire".</summary>
    private async Task MettreEnPauseAsync(string userId, int activiteId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var statut = await RequireKanbanStatutAsync(db, userId, activiteId, cancellationToken);
        var maintenant = DateTimeOffset.UtcNow;

        if (statut.Statut == KanbanColonnes.EnCours && statut.TimerDebutUtc is DateTimeOffset debut)
        {
            statut.TimerDureeReelleMs += (long)(maintenant - debut).TotalMilliseconds;
            statut.TimerDebutUtc = null;
        }

        statut.Statut = KanbanColonnes.AFaire;
        statut.UpdatedAt = maintenant;
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Même logique que <c>SetTimerStatutAsync</c> dans mvp.</summary>
    private async Task DefinirStatutMinuteurAsync(string userId, int activiteId, string statutCible, bool demarrerMinuteur, bool finaliser, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var statut = await RequireKanbanStatutAsync(db, userId, activiteId, cancellationToken);
        var maintenant = DateTimeOffset.UtcNow;

        if (finaliser && statut.Statut == KanbanColonnes.EnCours && statut.TimerDebutUtc is DateTimeOffset debut)
            statut.TimerDureeReelleMs += (long)(maintenant - debut).TotalMilliseconds;

        statut.Statut = statutCible;
        statut.TimerDebutUtc = demarrerMinuteur && !finaliser ? maintenant : null;
        statut.UpdatedAt = maintenant;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<KanbanStatut> RequireKanbanStatutAsync(GestionDuTempsDbContext db, string userId, int activiteId, CancellationToken cancellationToken)
    {
        var activite = await db.Activites.AsNoTracking().FirstOrDefaultAsync(a => a.Id == activiteId && a.UserId == userId, cancellationToken)
            ?? throw new InvalidOperationException("Activité introuvable.");

        return await db.KanbanStatuts.FirstOrDefaultAsync(s => s.ActiviteId == activite.Id, cancellationToken)
            ?? throw new InvalidOperationException("Statut Kanban introuvable pour cette activité.");
    }

    // ── Profil psychosocial ──────────────────────────────────────────────────

    public async Task<ProfilPsychosocial?> GetProfilPsychosocialAsync(string userId, CancellationToken cancellationToken = default)
    {
        var cycle = await GetCycleActifAsync(userId, cancellationToken);
        if (cycle is null)
            return null;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.ProfilsPsychosociaux.AsNoTracking().FirstOrDefaultAsync(p => p.CycleId == cycle.Id, cancellationToken);
    }

    public async Task SaveProfilPsychosocialAsync(string userId, ProfilPsychosocial profil, CancellationToken cancellationToken = default)
    {
        var cycle = await GetOrCreateCycleActifAsync(userId, cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var entity = await db.ProfilsPsychosociaux.FirstOrDefaultAsync(p => p.CycleId == cycle.Id, cancellationToken);
        if (entity is null)
        {
            entity = new ProfilPsychosocial { CycleId = cycle.Id, UserId = userId };
            db.ProfilsPsychosociaux.Add(entity);
        }

        CopierChampsProfil(profil, entity);
        entity.CompletedAt = DateTimeOffset.UtcNow;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Copie uniquement les champs de CONTENU — jamais Id/CycleId/UserId/CreatedAt, résolus côté serveur (voir la remarque sur IGestionDuTempsService.SaveProfilPsychosocialAsync).</summary>
    private static void CopierChampsProfil(ProfilPsychosocial source, ProfilPsychosocial target)
    {
        target.SommeilCoucher = source.SommeilCoucher;
        target.SommeilLever = source.SommeilLever;
        target.SommeilReparateur = source.SommeilReparateur;
        target.ReveilsNocturnes = source.ReveilsNocturnes;
        target.EcransAvantSommeil = source.EcransAvantSommeil;
        target.TempsPersoQuotidien = source.TempsPersoQuotidien;
        target.ManqueTempsPerso = source.ManqueTempsPerso;
        target.ActivitesRessourcantes = source.ActivitesRessourcantes;
        target.HoraireTravail = source.HoraireTravail;
        target.SurchargePrevisible = source.SurchargePrevisible;
        target.TravailHorsHeures = source.TravailHorsHeures;
        target.DureeTrajets = source.DureeTrajets;
        target.DeplacementsImprevus = source.DeplacementsImprevus;
        target.EngagementCommunautaire = source.EngagementCommunautaire;
        target.EngagementNature = source.EngagementNature;
        target.ProcheMalade = source.ProcheMalade;
        target.ModeTravail = source.ModeTravail;
        target.InterruptionsTravail = source.InterruptionsTravail;
        target.AutonomieGestion = source.AutonomieGestion;
        target.SentimentPression = source.SentimentPression;
        target.DifficultesConcentration = source.DifficultesConcentration;
        target.DecisionsPrecipitees = source.DecisionsPrecipitees;
        target.Culpabilite = source.Culpabilite;
        target.ToleranceImprevu = source.ToleranceImprevu;
        target.UtiliseAgenda = source.UtiliseAgenda;
        target.PlanificationAvance = source.PlanificationAvance;
        target.RituelsQuotidiens = source.RituelsQuotidiens;
        target.OuiTropFacile = source.OuiTropFacile;
        target.CapaciteNon = source.CapaciteNon;
        target.GestionAdmin = source.GestionAdmin;
        target.StressAdmin = source.StressAdmin;
        target.Tendances = source.Tendances.Distinct().ToList();
        target.Desequilibres = source.Desequilibres.Distinct().ToList();
        target.EmotionsNegatives = source.EmotionsNegatives.Distinct().ToList();
        target.EmotionsPositives = source.EmotionsPositives.Distinct().ToList();
        target.ObjectifsProfessionnels = source.ObjectifsProfessionnels.Distinct().ToList();
        target.Obstacles = source.Obstacles.Distinct().ToList();
        target.SatisfactionSommeil = source.SatisfactionSommeil;
        target.SatisfactionPerso = source.SatisfactionPerso;
        target.SatisfactionPro = source.SatisfactionPro;
        target.SatisfactionAdmin = source.SatisfactionAdmin;
        target.SatisfactionFamille = source.SatisfactionFamille;
        target.SatisfactionSocial = source.SatisfactionSocial;
        target.DirectionSommeil = source.DirectionSommeil;
        target.DirectionPerso = source.DirectionPerso;
        target.DirectionPro = source.DirectionPro;
        target.DirectionAdmin = source.DirectionAdmin;
        target.DirectionFamille = source.DirectionFamille;
        target.DirectionSocial = source.DirectionSocial;
    }

    // ── Réflexion consciente ─────────────────────────────────────────────────

    public async Task<ReflexionConsciente?> GetReflexionConscienteAsync(string userId, CancellationToken cancellationToken = default)
    {
        var cycle = await GetCycleActifAsync(userId, cancellationToken);
        if (cycle is null)
            return null;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.ReflexionsConscientes.AsNoTracking().FirstOrDefaultAsync(r => r.CycleId == cycle.Id, cancellationToken);
    }

    public async Task SaveReflexionConscienteAsync(string userId, ReflexionConsciente reflexion, CancellationToken cancellationToken = default)
    {
        var cycle = await GetOrCreateCycleActifAsync(userId, cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var entity = await db.ReflexionsConscientes.FirstOrDefaultAsync(r => r.CycleId == cycle.Id, cancellationToken);
        if (entity is null)
        {
            entity = new ReflexionConsciente { CycleId = cycle.Id, UserId = userId };
            db.ReflexionsConscientes.Add(entity);
        }

        entity.SituationActuelle = reflexion.SituationActuelle;
        entity.SourceIdentifiee = reflexion.SourceIdentifiee;
        entity.DateEcheance = reflexion.DateEcheance;
        entity.Ressentis = reflexion.Ressentis.Distinct().ToList();
        entity.Sources = reflexion.Sources.Distinct().ToList();
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    // ── Synthèse (calcul déterministe + narration IA ou de repli) ───────────────

    public async Task<SyntheseView?> GetSyntheseAsync(string userId, CancellationToken cancellationToken = default)
    {
        var cycle = await GetCycleActifAsync(userId, cancellationToken);
        if (cycle is null)
            return null;

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Syntheses.AsNoTracking().FirstOrDefaultAsync(s => s.CycleId == cycle.Id, cancellationToken);
        return entity is null ? null : ToSyntheseView(entity);
    }

    public async Task<SyntheseView> GenererSyntheseAsync(string userId, bool forcerRegeneration = false, CancellationToken cancellationToken = default)
    {
        var cycle = await GetOrCreateCycleActifAsync(userId, cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var profil = await db.ProfilsPsychosociaux.AsNoTracking().FirstOrDefaultAsync(p => p.CycleId == cycle.Id, cancellationToken);
        var reflexion = await db.ReflexionsConscientes.AsNoTracking().FirstOrDefaultAsync(r => r.CycleId == cycle.Id, cancellationToken);
        var resume = await BuildResumeActiviteAsync(db, cycle.Id, cancellationToken);
        var english = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "fr";

        var snapshotHash = CalculerHashSnapshot(cycle.Id, userId, profil, reflexion, resume, english);

        var existante = await db.Syntheses.FirstOrDefaultAsync(s => s.CycleId == cycle.Id, cancellationToken);
        if (!forcerRegeneration && existante is not null && existante.ProfilSnapshotHash == snapshotHash)
            return ToSyntheseView(existante);

        var (profilType, indice, maturite) = SyntheseCalculator.Calculer(profil);

        SyntheseContenuNarratif contenu;
        string? avertissementIa = null;
        if (profil is null)
        {
            // Rien à analyser de significatif — pas d'appel IA, texte de repli directement.
            contenu = SyntheseNarrativeBuilder.BuildFallback(new ProfilPsychosocial { CycleId = cycle.Id, UserId = userId }, profilType, indice, maturite, english);
            avertissementIa = "Profil psychosocial non renseigné pour ce cycle — complétez /gestion-du-temps/profil puis régénérez.";
        }
        else
        {
            var systemPrompt = SyntheseNarrativeBuilder.BuildSystemPrompt(english);
            var userPrompt = SyntheseNarrativeBuilder.BuildUserPrompt(profil, reflexion, resume);
            var (output, error) = await aiSynthesisService.GenererTexteAsync(systemPrompt, userPrompt, cancellationToken);

            if (error is not null || string.IsNullOrWhiteSpace(output))
            {
                contenu = SyntheseNarrativeBuilder.BuildFallback(profil, profilType, indice, maturite, english);
                avertissementIa = error ?? "Réponse IA vide.";
            }
            else
            {
                try
                {
                    contenu = SyntheseNarrativeBuilder.ParseResponse(output);
                }
                catch (Exception ex)
                {
                    // Réponse IA reçue mais mal formée (JSON invalide/inattendu) — même repli que si l'IA
                    // avait échoué : jamais une exception remontée jusqu'à l'utilisateur.
                    contenu = SyntheseNarrativeBuilder.BuildFallback(profil, profilType, indice, maturite, english);
                    avertissementIa = "Réponse IA illisible : " + ex.Message;
                }
            }
        }

        if (existante is null)
        {
            existante = new Synthese { CycleId = cycle.Id, UserId = userId, ProfilType = contenu.ProfilType };
            db.Syntheses.Add(existante);
        }

        existante.ProfilType = contenu.ProfilType;
        existante.IndiceEquilibre = indice;
        existante.NiveauMaturite = maturite;
        existante.ProfilTexte = contenu.ProfilTexte;
        existante.IndiceCommentaire = contenu.IndiceCommentaire;
        existante.MaturiteCommentaire = contenu.MaturiteCommentaire;
        existante.RecommandationsJson = contenu.Recommandations.Count > 0 ? JsonSerializer.Serialize(contenu.Recommandations, JsonOptions) : null;
        existante.AlertesJson = contenu.Alertes.Count > 0 ? JsonSerializer.Serialize(contenu.Alertes, JsonOptions) : null;
        existante.GenereeParIa = contenu.GenereeParIa;
        existante.ProfilSnapshotHash = snapshotHash;
        existante.CalculatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return ToSyntheseView(existante, avertissementIa);
    }

    private static SyntheseView ToSyntheseView(Synthese s, string? avertissementIa = null) => new(
        s.ProfilType, s.IndiceEquilibre, s.NiveauMaturite,
        s.ProfilTexte, s.IndiceCommentaire, s.MaturiteCommentaire,
        string.IsNullOrEmpty(s.RecommandationsJson) ? [] : JsonSerializer.Deserialize<List<RecommandationIa>>(s.RecommandationsJson, JsonOptions) ?? [],
        string.IsNullOrEmpty(s.AlertesJson) ? [] : JsonSerializer.Deserialize<List<string>>(s.AlertesJson, JsonOptions) ?? [],
        s.GenereeParIa, s.CalculatedAt, avertissementIa);

    /// <summary>Résumé construit exclusivement à partir des données du module (Activite/KanbanStatut du cycle) — jamais d'un autre module.</summary>
    private static async Task<ResumeActiviteCycle> BuildResumeActiviteAsync(GestionDuTempsDbContext db, int cycleId, CancellationToken cancellationToken)
    {
        var activiteIds = await db.Activites.AsNoTracking().Where(a => a.CycleId == cycleId).Select(a => a.Id).ToListAsync(cancellationToken);
        if (activiteIds.Count == 0)
            return new ResumeActiviteCycle(0, 0, 0, 0, 0);

        var statuts = await db.KanbanStatuts.AsNoTracking().Where(s => activiteIds.Contains(s.ActiviteId)).ToListAsync(cancellationToken);
        var maintenant = DateTimeOffset.UtcNow;

        return new ResumeActiviteCycle(
            TotalActivites: activiteIds.Count,
            Terminees: statuts.Count(s => s.Statut == KanbanColonnes.Termine),
            EnCours: statuts.Count(s => s.Statut == KanbanColonnes.EnCours),
            AFaire: statuts.Count(s => s.Statut == KanbanColonnes.AFaire),
            TempsReelTotalMs: statuts.Sum(s => KanbanTimer.GetElapsedMs(s, maintenant)));
    }

    /// <summary>
    /// Hash scopé par cycle + utilisateur (jamais global) — comme demandé, cohérent avec le reste du module :
    /// deux utilisateurs (ou deux cycles du même utilisateur) ne partagent jamais un cache de synthèse, même
    /// à contenu de profil identique.
    /// </summary>
    private static string CalculerHashSnapshot(int cycleId, string userId, ProfilPsychosocial? profil, ReflexionConsciente? reflexion, ResumeActiviteCycle resume, bool english)
    {
        var payload = new
        {
            cycleId,
            userId,
            english,
            profil?.SommeilCoucher, profil?.SommeilLever, profil?.SommeilReparateur, profil?.ReveilsNocturnes, profil?.EcransAvantSommeil,
            profil?.TempsPersoQuotidien, profil?.ManqueTempsPerso, profil?.ActivitesRessourcantes,
            profil?.HoraireTravail, profil?.SurchargePrevisible, profil?.TravailHorsHeures,
            profil?.DureeTrajets, profil?.DeplacementsImprevus, profil?.EngagementCommunautaire, profil?.EngagementNature, profil?.ProcheMalade,
            profil?.ModeTravail, profil?.InterruptionsTravail, profil?.AutonomieGestion,
            profil?.SentimentPression, profil?.DifficultesConcentration, profil?.DecisionsPrecipitees, profil?.Culpabilite, profil?.ToleranceImprevu,
            profil?.UtiliseAgenda, profil?.PlanificationAvance, profil?.RituelsQuotidiens, profil?.OuiTropFacile, profil?.CapaciteNon, profil?.GestionAdmin, profil?.StressAdmin,
            Tendances = profil?.Tendances.OrderBy(x => x, StringComparer.Ordinal).ToList(),
            Desequilibres = profil?.Desequilibres.OrderBy(x => x, StringComparer.Ordinal).ToList(),
            EmotionsNegatives = profil?.EmotionsNegatives.OrderBy(x => x, StringComparer.Ordinal).ToList(),
            EmotionsPositives = profil?.EmotionsPositives.OrderBy(x => x, StringComparer.Ordinal).ToList(),
            ObjectifsProfessionnels = profil?.ObjectifsProfessionnels.OrderBy(x => x, StringComparer.Ordinal).ToList(),
            Obstacles = profil?.Obstacles.OrderBy(x => x, StringComparer.Ordinal).ToList(),
            profil?.SatisfactionSommeil, profil?.SatisfactionPerso, profil?.SatisfactionPro, profil?.SatisfactionAdmin, profil?.SatisfactionFamille, profil?.SatisfactionSocial,
            profil?.DirectionSommeil, profil?.DirectionPerso, profil?.DirectionPro, profil?.DirectionAdmin, profil?.DirectionFamille, profil?.DirectionSocial,
            reflexion?.SituationActuelle, reflexion?.SourceIdentifiee, reflexion?.DateEcheance,
            Ressentis = reflexion?.Ressentis.OrderBy(x => x, StringComparer.Ordinal).ToList(),
            Sources = reflexion?.Sources.OrderBy(x => x, StringComparer.Ordinal).ToList(),
            resume.TotalActivites, resume.Terminees, resume.EnCours, resume.AFaire, resume.TempsReelTotalMs,
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes);
    }
}
