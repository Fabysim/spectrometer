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
    /// <summary>Catégories par défaut reprises telles quelles de <c>GestionDuTempsService.DefaultTypes</c> dans mvp.</summary>
    private static readonly (string Cle, string Libelle, TimeOnly Debut, TimeOnly Fin, string Recurrence, int Ordre)[] DefaultTypes =
    [
        ("sommeil", "Sommeil · Repos", new TimeOnly(22, 0), new TimeOnly(6, 0), "D,L,M,Me,J,V,S", 0),
        ("perso", "Personnel", new TimeOnly(7, 0), new TimeOnly(8, 0), "L,M,Me,J,V", 1),
        ("pro", "Professionnel", new TimeOnly(8, 0), new TimeOnly(17, 0), "L,M,Me,J,V", 2),
        ("admin", "Administratif", new TimeOnly(17, 0), new TimeOnly(18, 0), "L,Me,V", 3),
        ("famille", "Familial · affectif", new TimeOnly(18, 0), new TimeOnly(21, 0), "L,M,Me,J,V,S,D", 4),
        ("social", "Social", new TimeOnly(21, 0), new TimeOnly(22, 0), "S,D", 5),
    ];

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

    public async Task<IReadOnlyList<TypeDeTempsView>> GetTypesDeTempsAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var existants = await db.TypesDeTemps
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.OrdreAffichage)
            .ThenBy(t => t.Cle)
            .ToListAsync(cancellationToken);

        if (existants.Count == 0)
            existants = await CreerTypesParDefautAsync(userId, cancellationToken);

        return existants
            .Select(t => new TypeDeTempsView(t.Id, t.Cle, t.Libelle, t.HeureDebut, t.HeureFin, t.RecurrenceJours, t.OrdreAffichage, t.CompanyId))
            .ToList();
    }

    /// <summary>
    /// Insertion en course : deux requêtes initiales quasi simultanées pour un même utilisateur (deux onglets
    /// ouverts au premier accès) pourraient toutes deux constater une liste vide et tenter d'insérer les 6
    /// catégories par défaut — l'index unique (UserId, Cle) fait alors échouer la seconde en violation
    /// d'unicité plutôt qu'en doublon silencieux ; on retombe simplement sur la lecture existante dans ce cas.
    /// </summary>
    private async Task<List<TypeDeTemps>> CreerTypesParDefautAsync(string userId, CancellationToken cancellationToken)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var maintenant = DateTimeOffset.UtcNow;

        foreach (var (cle, libelle, debut, fin, recurrence, ordre) in DefaultTypes)
        {
            db.TypesDeTemps.Add(new TypeDeTemps
            {
                UserId = userId,
                Cle = cle,
                Libelle = libelle,
                HeureDebut = debut,
                HeureFin = fin,
                RecurrenceJours = recurrence,
                OrdreAffichage = ordre,
                CreatedAt = maintenant,
                UpdatedAt = maintenant,
            });
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation })
        {
            // Un autre appel concurrent a déjà créé les catégories par défaut pour cet utilisateur — rien à faire.
        }

        await using var lecture = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await lecture.TypesDeTemps
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.OrdreAffichage)
            .ThenBy(t => t.Cle)
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertTypeDeTempsAsync(string userId, int? id, string cle, string libelle, TimeOnly heureDebut, TimeOnly heureFin, string recurrenceJours, int ordreAffichage, int? companyId, CancellationToken cancellationToken = default)
    {
        await VerifierCompanyIdAsync(userId, companyId, cancellationToken);

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        TypeDeTemps type;
        if (id is int existingId)
        {
            type = await db.TypesDeTemps.FirstOrDefaultAsync(t => t.Id == existingId && t.UserId == userId, cancellationToken)
                ?? throw new InvalidOperationException("Type de temps introuvable.");
        }
        else
        {
            type = new TypeDeTemps { UserId = userId, Cle = cle, Libelle = libelle };
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

    public async Task<IReadOnlyList<ActiviteView>> GetActivitesAsync(string userId, int? companyId, bool personnelUniquement, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var query = db.Activites.AsNoTracking().Where(a => a.UserId == userId);
        if (companyId is int cid)
            query = query.Where(a => a.CompanyId == cid);
        else if (personnelUniquement)
            query = query.Where(a => a.CompanyId == null);

        var activites = await query
            .OrderBy(a => a.DateActivite).ThenBy(a => a.HeureDebut)
            .ToListAsync(cancellationToken);

        var types = await db.TypesDeTemps.AsNoTracking().Where(t => t.UserId == userId).ToDictionaryAsync(t => t.Id, cancellationToken);

        return activites
            .Select(a =>
            {
                types.TryGetValue(a.TypeDeTempsId, out var type);
                return new ActiviteView(a.Id, a.TypeDeTempsId, type?.Libelle ?? "(catégorie supprimée)", CouleurCategorie(type?.Cle), a.Nom, a.DateActivite, a.HeureDebut, a.DureeMinutes, a.CompanyId, a.Statut);
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

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var typeExiste = await db.TypesDeTemps.AsNoTracking().AnyAsync(t => t.Id == typeDeTempsId && t.UserId == userId, cancellationToken);
        if (!typeExiste)
            throw new InvalidOperationException("Type de temps introuvable.");

        var activite = new Activite
        {
            UserId = userId,
            TypeDeTempsId = typeDeTempsId,
            Nom = nom,
            DateActivite = dateActivite,
            HeureDebut = heureDebut,
            DureeMinutes = dureeMinutes,
            CompanyId = companyId,
        };
        db.Activites.Add(activite);
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

        db.Activites.Remove(activite);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiviteStatutAsync(string userId, int activiteId, string statut, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var activite = await db.Activites.FirstOrDefaultAsync(a => a.Id == activiteId && a.UserId == userId, cancellationToken);
        if (activite is null) return;

        activite.Statut = statut;
        activite.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}
