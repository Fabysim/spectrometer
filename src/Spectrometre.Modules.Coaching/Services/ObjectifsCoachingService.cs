using Microsoft.EntityFrameworkCore;
using Spectrometre.Modules.Coaching.Data;
using Spectrometre.Modules.Coaching.Entities;

namespace Spectrometre.Modules.Coaching.Services;

/// <summary>
/// Implémentation des objectifs de coaching. Utilise <see cref="IDbContextFactory{TContext}"/> comme
/// <see cref="CoachingService"/> (factory enregistrée par <c>AddCoachingModule</c>) — jamais un DbContext
/// scopé partagé sur un circuit Blazor.
/// </summary>
public sealed class ObjectifsCoachingService(IDbContextFactory<CoachingDbContext> coachingDbFactory) : IObjectifsCoachingService
{
    public async Task<PeriodeObjectifsCoachingView?> GetPeriodeCouranteAsync(int lienId, string requestingCoachUserId, CancellationToken cancellationToken = default)
    {
        await using var db = await coachingDbFactory.CreateDbContextAsync(cancellationToken);
        if (!await EstCoachActifAsync(db, lienId, requestingCoachUserId, cancellationToken))
            return null;

        var periode = await ObtenirOuCreerPeriodeCouranteAsync(db, lienId, cancellationToken);
        var lien = await db.LiensCoaching.AsNoTracking().FirstAsync(l => l.Id == lienId, cancellationToken);
        return await ChargerVueAsync(db, periode.Id, lien.SuiviUserId, cancellationToken);
    }

    public async Task<bool> SaveObjectifsAsync(int lienId, string requestingCoachUserId, IReadOnlyList<ObjectifCoachingInput> objectifs, CancellationToken cancellationToken = default)
    {
        await using var db = await coachingDbFactory.CreateDbContextAsync(cancellationToken);
        if (!await EstCoachActifAsync(db, lienId, requestingCoachUserId, cancellationToken))
            return false;

        var periode = await ObtenirOuCreerPeriodeCouranteAsync(db, lienId, cancellationToken);
        await RemplacerObjectifsAsync(db, periode, objectifs, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TerminerPeriodeAsync(int lienId, string requestingCoachUserId, IReadOnlyList<ObjectifCoachingInput>? objectifs = null, CancellationToken cancellationToken = default)
    {
        await using var db = await coachingDbFactory.CreateDbContextAsync(cancellationToken);
        if (!await EstCoachActifAsync(db, lienId, requestingCoachUserId, cancellationToken))
            return false;

        var periode = await ObtenirOuCreerPeriodeCouranteAsync(db, lienId, cancellationToken);
        if (objectifs is not null)
            await RemplacerObjectifsAsync(db, periode, objectifs, cancellationToken);

        periode.Archivee = true;
        periode.DateFin = DateOnly.FromDateTime(DateTime.UtcNow);

        // Nouvelle période ouverte immédiatement (comme le rechargement GetPeriodeCourante après archive côté SuiviEmployes).
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.PeriodesObjectifsCoaching.Add(new PeriodeObjectifsCoaching
        {
            LienCoachingId = lienId,
            DateDebut = today,
            DateFin = today.AddMonths(3),
            Archivee = false,
        });

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<PeriodeObjectifsCoachingView>> GetArchivesAsync(int lienId, string requestingCoachUserId, CancellationToken cancellationToken = default)
    {
        await using var db = await coachingDbFactory.CreateDbContextAsync(cancellationToken);
        if (!await EstCoachActifAsync(db, lienId, requestingCoachUserId, cancellationToken))
            return [];

        var ids = await db.PeriodesObjectifsCoaching.AsNoTracking()
            .Where(p => p.LienCoachingId == lienId && p.Archivee)
            .OrderByDescending(p => p.DateFin)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var suiviUserId = await db.LiensCoaching.AsNoTracking()
            .Where(l => l.Id == lienId)
            .Select(l => l.SuiviUserId)
            .FirstAsync(cancellationToken);

        var result = new List<PeriodeObjectifsCoachingView>(ids.Count);
        foreach (var id in ids)
        {
            var vue = await ChargerVueAsync(db, id, suiviUserId, cancellationToken);
            if (vue is not null)
                result.Add(vue);
        }

        return result;
    }

    public async Task<int?> TryGetPremierLienIdAvecObjectifsOuvertsAsync(
        string coachUserId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await coachingDbFactory.CreateDbContextAsync(cancellationToken);

        // Objectif « ouvert » = Atteinte ≠ Oui (NonDefini / Non / NonImputable encore à traiter).
        // Ne pas appeler GetPeriodeCouranteAsync : cela créerait des périodes vides pour chaque lien.
        return await db.LiensCoaching.AsNoTracking()
            .Where(l => l.CoachUserId == coachUserId && l.Statut == LienCoachingStatut.Actif)
            .Where(l => db.PeriodesObjectifsCoaching.Any(p =>
                p.LienCoachingId == l.Id
                && !p.Archivee
                && p.Objectifs.Any(o => o.Atteinte != AtteinteObjectifCoaching.Oui)))
            .OrderBy(l => l.CreatedAt)
            .Select(l => (int?)l.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static async Task<bool> EstCoachActifAsync(CoachingDbContext db, int lienId, string requestingCoachUserId, CancellationToken cancellationToken)
    {
        return await db.LiensCoaching.AsNoTracking().AnyAsync(
            l => l.Id == lienId
                 && l.CoachUserId == requestingCoachUserId
                 && l.Statut == LienCoachingStatut.Actif,
            cancellationToken);
    }

    private static async Task<PeriodeObjectifsCoaching> ObtenirOuCreerPeriodeCouranteAsync(CoachingDbContext db, int lienId, CancellationToken cancellationToken)
    {
        var courante = await db.PeriodesObjectifsCoaching
            .Include(p => p.Objectifs)
            .FirstOrDefaultAsync(p => p.LienCoachingId == lienId && !p.Archivee, cancellationToken);

        if (courante is not null)
            return courante;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        courante = new PeriodeObjectifsCoaching
        {
            LienCoachingId = lienId,
            DateDebut = today,
            DateFin = today.AddMonths(3),
            Archivee = false,
        };
        db.PeriodesObjectifsCoaching.Add(courante);
        await db.SaveChangesAsync(cancellationToken);
        return courante;
    }

    private static async Task RemplacerObjectifsAsync(
        CoachingDbContext db,
        PeriodeObjectifsCoaching periode,
        IReadOnlyList<ObjectifCoachingInput> objectifs,
        CancellationToken cancellationToken)
    {
        // Recharger la navigation si absente (cas après création).
        if (!db.Entry(periode).Collection(p => p.Objectifs).IsLoaded)
            await db.Entry(periode).Collection(p => p.Objectifs).LoadAsync(cancellationToken);

        db.ObjectifsCoaching.RemoveRange(periode.Objectifs);
        periode.Objectifs.Clear();

        foreach (var o in objectifs)
        {
            var titre = (o.Titre ?? "").Trim();
            if (string.IsNullOrWhiteSpace(titre))
                continue;

            periode.Objectifs.Add(new ObjectifCoaching
            {
                Date = o.Date,
                Titre = titre,
                Moyens = string.IsNullOrWhiteSpace(o.Moyens) ? null : o.Moyens.Trim(),
                Atteinte = o.Atteinte,
                Observation = string.IsNullOrWhiteSpace(o.Observation) ? null : o.Observation.Trim(),
                Note = o.Note is >= 0 and <= 100 ? o.Note : null,
            });
        }
    }

    private static async Task<PeriodeObjectifsCoachingView?> ChargerVueAsync(CoachingDbContext db, int periodeId, string suiviUserId, CancellationToken cancellationToken)
    {
        var periode = await db.PeriodesObjectifsCoaching.AsNoTracking()
            .Include(p => p.Objectifs)
            .FirstOrDefaultAsync(p => p.Id == periodeId, cancellationToken);
        if (periode is null)
            return null;

        var objectifs = periode.Objectifs
            .OrderBy(o => o.Date)
            .ThenBy(o => o.Id)
            .Select(o => new ObjectifCoachingView(o.Id, o.Date, o.Titre, o.Moyens, o.Atteinte, o.Observation, o.Note))
            .ToList();

        return new PeriodeObjectifsCoachingView(
            periode.Id,
            periode.LienCoachingId,
            suiviUserId,
            periode.DateDebut,
            periode.DateFin,
            periode.Archivee,
            objectifs);
    }
}
