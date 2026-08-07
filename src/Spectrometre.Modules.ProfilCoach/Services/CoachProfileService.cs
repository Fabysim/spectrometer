using Microsoft.EntityFrameworkCore;
using Spectrometre.Modules.ProfilCoach.Data;
using Spectrometre.Modules.ProfilCoach.Entities;

namespace Spectrometre.Modules.ProfilCoach.Services;

/// <summary>
/// Utilise <see cref="IDbContextFactory{TContext}"/> (jamais un <see cref="ProfilCoachDbContext"/> scopé
/// injecté directement) — même raison que <c>CandidateProfileService</c> : une instance fraîche par appel
/// élimine toute classe de bug liée à deux opérations concurrentes sur le même DbContext (interdit par EF
/// Core), pertinent dès qu'un coach édite son profil pendant qu'un autre gestionnaire lit l'annuaire sur le
/// même circuit Blazor Server.
/// </summary>
public sealed class CoachProfileService(IDbContextFactory<ProfilCoachDbContext> dbFactory) : ICoachProfileService
{
    public async Task<int> GetOrCreateProfileIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.CoachProfiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (existing is not null)
            return existing.Id;

        var profile = new CoachProfile { UserId = userId };
        db.CoachProfiles.Add(profile);
        await db.SaveChangesAsync(cancellationToken);
        return profile.Id;
    }

    public async Task<CoachProfileView?> GetProfilAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var profile = await db.CoachProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        return profile is null
            ? null
            : new CoachProfileView(profile.Id, profile.UserId, profile.NomAffiche, profile.BioCourte, profile.Specialites, profile.VisibleDansAnnuaire);
    }

    public async Task SaveProfilAsync(string userId, string nomAffiche, string bioCourte, string specialites, bool visibleDansAnnuaire, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var profile = await db.CoachProfiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (profile is null)
        {
            profile = new CoachProfile { UserId = userId };
            db.CoachProfiles.Add(profile);
        }

        profile.NomAffiche = nomAffiche;
        profile.BioCourte = bioCourte;
        profile.Specialites = specialites;
        profile.VisibleDansAnnuaire = visibleDansAnnuaire;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CoachAnnuaireEntry>> GetAnnuaireVisibleAsync(string? recherche, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var query = db.CoachProfiles.AsNoTracking().Where(p => p.VisibleDansAnnuaire);

        if (!string.IsNullOrWhiteSpace(recherche))
        {
            var terme = $"%{recherche.Trim()}%";
            query = query.Where(p => EF.Functions.ILike(p.NomAffiche, terme) || EF.Functions.ILike(p.Specialites, terme));
        }

        return await query
            .OrderBy(p => p.NomAffiche)
            .Select(p => new CoachAnnuaireEntry(p.Id, p.NomAffiche, p.BioCourte, p.Specialites))
            .ToListAsync(cancellationToken);
    }

    public async Task<string?> GetUserIdAsync(int coachProfileId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.CoachProfiles.AsNoTracking()
            .Where(p => p.Id == coachProfileId)
            .Select(p => p.UserId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
