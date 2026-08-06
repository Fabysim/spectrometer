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
    IDbContextFactory<CoreDbContext> coreDbFactory) : IGestionDuTempsService
{
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

    public async Task<CycleView> GetOrCreateCycleActifAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var existant = await db.Cycles.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Statut == CycleStatuts.EnCours, cancellationToken);

        return ToCycleView(existant ?? await CreerEtSeedCycleAsync(userId, DefaultTypes, cancellationToken));
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
        var cycle = await GetOrCreateCycleActifAsync(userId, cancellationToken);

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
        var cycle = await GetOrCreateCycleActifAsync(userId, cancellationToken);
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

        return activites
            .Select(a =>
            {
                types.TryGetValue(a.TypeDeTempsId, out var type);
                return new ActiviteView(a.Id, a.TypeDeTempsId, type?.Libelle ?? "(catégorie supprimée)", CouleurCategorie(type?.Cle), a.Nom, a.DateActivite, a.HeureDebut, a.DureeMinutes, a.CompanyId);
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

    public async Task UpdateActiviteAsync(string userId, int activiteId, string nom, DateOnly dateActivite, TimeOnly heureDebut, int dureeMinutes, int? companyId, CancellationToken cancellationToken = default)
    {
        await VerifierCompanyIdAsync(userId, companyId, cancellationToken);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var activite = await db.Activites.FirstOrDefaultAsync(a => a.Id == activiteId && a.UserId == userId, cancellationToken);
        if (activite is null) return;

        activite.Nom = nom;
        activite.DateActivite = dateActivite;
        activite.HeureDebut = heureDebut;
        activite.DureeMinutes = dureeMinutes;
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
        var cycle = await GetOrCreateCycleActifAsync(userId, cancellationToken);
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var activites = await db.Activites.AsNoTracking().Where(a => a.CycleId == cycle.Id).ToListAsync(cancellationToken);
        var types = await db.TypesDeTemps.AsNoTracking().Where(t => t.CycleId == cycle.Id).ToDictionaryAsync(t => t.Id, cancellationToken);
        var activiteIds = activites.Select(a => a.Id).ToList();
        var statuts = await db.KanbanStatuts.AsNoTracking().Where(s => activiteIds.Contains(s.ActiviteId)).ToDictionaryAsync(s => s.ActiviteId, cancellationToken);

        var maintenant = DateTimeOffset.UtcNow;
        return activites
            .Select(a =>
            {
                types.TryGetValue(a.TypeDeTempsId, out var type);
                var statut = statuts.GetValueOrDefault(a.Id) ?? new KanbanStatut { ActiviteId = a.Id };
                var tempsReelMs = KanbanTimer.GetElapsedMs(statut, maintenant);
                return new KanbanCarteView(
                    a.Id, a.Nom, type?.Libelle ?? "(catégorie supprimée)", CouleurCategorie(type?.Cle), a.DureeMinutes, a.CompanyId,
                    statut.Statut, tempsReelMs, KanbanTimer.IsOvertime(tempsReelMs, a.DureeMinutes));
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
}
