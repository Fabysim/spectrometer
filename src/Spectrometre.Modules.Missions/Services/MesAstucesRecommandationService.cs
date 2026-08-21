using Microsoft.EntityFrameworkCore;
using Spectrometre.Modules.JeunesPrestataires.Data;
using Spectrometre.Modules.JeunesPrestataires.Services;
using Spectrometre.Modules.Missions.Catalog;
using Spectrometre.Modules.Missions.Data;
using Spectrometre.Modules.Missions.Entities;

namespace Spectrometre.Modules.Missions.Services;

public interface IMesAstucesRecommandationService
{
    /// <summary><c>null</c> si pas de profil jeune. Liste vide = pas de mise en avant (scores OK ou rien à signaler).</summary>
    Task<IReadOnlyList<MesAstucesFicheDef>?> GetRecommandeesAsync(
        string jeuneUserId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Recommandations passives à la consultation de « Mes astuces » — pas de notification, pas de badge.
/// Factories Missions + JeunesPrestataires, comme <see cref="BadgeService"/>.
/// </summary>
public sealed class MesAstucesRecommandationService(
    IJeuneProfileService jeuneProfileService,
    IDbContextFactory<MissionsDbContext> missionsDbFactory,
    IDbContextFactory<JeunesPrestatairesDbContext> jeunesDbFactory) : IMesAstucesRecommandationService
{
    public async Task<IReadOnlyList<MesAstucesFicheDef>?> GetRecommandeesAsync(
        string jeuneUserId,
        CancellationToken cancellationToken = default)
    {
        var jeune = await jeuneProfileService.TryGetByUserIdAsync(jeuneUserId, cancellationToken);
        if (jeune is null)
            return null;

        await using var missionsDb = await missionsDbFactory.CreateDbContextAsync(cancellationToken);
        var missionsTerminees = await missionsDb.MissionAcceptations.AsNoTracking()
            .CountAsync(a => a.JeuneProfileId == jeune.Id && a.Mission.Statut == MissionStatut.Terminee, cancellationToken);

        var derniereEval = await missionsDb.MissionEvaluationsParticulier.AsNoTracking()
            .Where(e => e.MissionAcceptation.JeuneProfileId == jeune.Id)
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new MesAstucesEvalSignaux(
                e.Ponctualite,
                e.ConsignesComprises,
                e.TacheRealiseeCorrectement,
                e.AttitudeRespectueuse))
            .FirstOrDefaultAsync(cancellationToken);

        await using var jeunesDb = await jeunesDbFactory.CreateDbContextAsync(cancellationToken);
        var derniereGrille = await jeunesDb.GrilleObservationEvaluations.AsNoTracking()
            .Where(e => e.JeuneProfileId == jeune.Id)
            .Include(e => e.Criteres)
            .OrderByDescending(e => e.EvalueeLe)
            .FirstOrDefaultAsync(cancellationToken);

        int? scoreComm = derniereGrille?.Criteres.FirstOrDefault(c => c.CritereKey == "communication")?.Score;
        int? scoreAuto = derniereGrille?.Criteres.FirstOrDefault(c => c.CritereKey == "autonomie")?.Score;

        return MesAstucesRecommandationsCatalog.Selectionner(
            missionsTerminees == 0,
            jeune.ProfilAccompagnement,
            derniereEval,
            scoreComm,
            scoreAuto);
    }
}
