using Microsoft.EntityFrameworkCore;
using Spectrometre.Modules.JeunesPrestataires.Data;
using Spectrometre.Modules.JeunesPrestataires.Services;
using Spectrometre.Modules.Missions.Catalog;
using Spectrometre.Modules.Missions.Data;
using Spectrometre.Modules.Missions.Entities;

namespace Spectrometre.Modules.Missions.Services;

/// <summary>État d'un badge recalculé à l'affichage (jamais persisté).</summary>
public sealed record BadgeView(
    string Key,
    string Emoji,
    bool Debloque,
    DateTimeOffset? DebloqueLe,
    int Actuel,
    int Seuil);

public interface IBadgeService
{
    /// <summary><c>null</c> si l'utilisateur n'a pas de profil jeune.</summary>
    Task<IReadOnlyList<BadgeView>?> GetBadgesAsync(string jeuneUserId, CancellationToken cancellationToken = default);
}

public sealed class BadgeService(
    IJeuneProfileService jeuneProfileService,
    IDbContextFactory<MissionsDbContext> missionsDbFactory,
    IDbContextFactory<JeunesPrestatairesDbContext> jeunesDbFactory) : IBadgeService
{
    public async Task<IReadOnlyList<BadgeView>?> GetBadgesAsync(
        string jeuneUserId,
        CancellationToken cancellationToken = default)
    {
        var jeune = await jeuneProfileService.TryGetByUserIdAsync(jeuneUserId, cancellationToken);
        if (jeune is null)
            return null;

        await using var missionsDb = await missionsDbFactory.CreateDbContextAsync(cancellationToken);
        var datesTerminees = await missionsDb.MissionAcceptations
            .AsNoTracking()
            .Where(a => a.JeuneProfileId == jeune.Id && a.Mission.Statut == MissionStatut.Terminee)
            .OrderBy(a => a.DecideeLe ?? a.AccepteeLe)
            .Select(a => a.DecideeLe ?? a.AccepteeLe)
            .ToListAsync(cancellationToken);

        var evals = await missionsDb.MissionEvaluationsParticulier
            .AsNoTracking()
            .Where(e => e.MissionAcceptation.JeuneProfileId == jeune.Id)
            .OrderBy(e => e.CreatedAt)
            .Select(e => new EvalFlags(
                e.CreatedAt,
                e.Ponctualite,
                e.TacheRealiseeCorrectement,
                e.AccepteraitNouvelleMission,
                e.AttitudeRespectueuse))
            .ToListAsync(cancellationToken);

        await using var jeunesDb = await jeunesDbFactory.CreateDbContextAsync(cancellationToken);
        var charte = await jeunesDb.CharteAcceptations.AsNoTracking()
            .FirstOrDefaultAsync(c => c.JeuneProfileId == jeune.Id, cancellationToken);

        var grilles = await jeunesDb.GrilleObservationEvaluations
            .AsNoTracking()
            .Where(e => e.JeuneProfileId == jeune.Id)
            .Include(e => e.Criteres)
            .OrderBy(e => e.EvalueeLe)
            .ToListAsync(cancellationToken);

        return BadgeCatalog.All.Select(def => Compute(def, datesTerminees, evals, charte?.AccepteeLe, grilles)).ToList();
    }

    private static BadgeView Compute(
        BadgeDef def,
        IReadOnlyList<DateTimeOffset> datesTerminees,
        IReadOnlyList<EvalFlags> evals,
        DateTimeOffset? charteAccepteeLe,
        IReadOnlyList<Spectrometre.Modules.JeunesPrestataires.Entities.GrilleObservationEvaluation> grilles)
    {
        return def.Criterion switch
        {
            BadgeCriterion.MissionsTerminees => FromDates(def, datesTerminees),
            BadgeCriterion.EvalPonctualite => FromEvals(def, evals, e => e.Ponctualite == true),
            BadgeCriterion.EvalTacheRealisee => FromEvals(def, evals, e => e.TacheOk == true),
            BadgeCriterion.EvalNouvelleMission => FromEvals(def, evals, e => e.NouvelleMission == true),
            BadgeCriterion.EvalAttitudeRespectueuse => FromEvals(def, evals, e => e.Respect == true),
            BadgeCriterion.CharteAcceptee => new BadgeView(
                def.Key, def.Emoji, charteAccepteeLe is not null, charteAccepteeLe,
                charteAccepteeLe is null ? 0 : 1, 1),
            BadgeCriterion.GrilleScore => FromGrille(def, grilles),
            _ => new BadgeView(def.Key, def.Emoji, false, null, 0, def.Threshold),
        };
    }

    private static BadgeView FromDates(BadgeDef def, IReadOnlyList<DateTimeOffset> dates)
    {
        var actuel = dates.Count;
        var ok = actuel >= def.Threshold;
        return new BadgeView(def.Key, def.Emoji, ok, ok ? dates[def.Threshold - 1] : null, actuel, def.Threshold);
    }

    private static BadgeView FromEvals(BadgeDef def, IReadOnlyList<EvalFlags> evals, Func<EvalFlags, bool> pred)
    {
        var hits = evals.Where(pred).Select(e => e.CreatedAt).ToList();
        var actuel = hits.Count;
        var ok = actuel >= def.Threshold;
        return new BadgeView(def.Key, def.Emoji, ok, ok ? hits[def.Threshold - 1] : null, actuel, def.Threshold);
    }

    private static BadgeView FromGrille(
        BadgeDef def,
        IReadOnlyList<Spectrometre.Modules.JeunesPrestataires.Entities.GrilleObservationEvaluation> grilles)
    {
        var key = def.GrilleCritereKey!;
        var scores = grilles
            .Select(e => (e.EvalueeLe, Score: e.Criteres.FirstOrDefault(c => c.CritereKey == key)?.Score))
            .Where(x => x.Score is int)
            .Select(x => (x.EvalueeLe, Score: x.Score!.Value))
            .ToList();

        var meilleur = scores.Count == 0 ? 0 : scores.Max(s => s.Score);
        DateTimeOffset? premiereLe = null;
        foreach (var s in scores)
        {
            if (s.Score >= def.Threshold)
            {
                premiereLe = s.EvalueeLe;
                break;
            }
        }

        return new BadgeView(
            def.Key, def.Emoji, premiereLe is not null, premiereLe, meilleur, def.Threshold);
    }

    private sealed record EvalFlags(
        DateTimeOffset CreatedAt,
        bool? Ponctualite,
        bool? TacheOk,
        bool? NouvelleMission,
        bool? Respect);
}
