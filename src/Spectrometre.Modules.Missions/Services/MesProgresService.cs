using Spectrometre.Modules.JeunesPrestataires.Services;
using Spectrometre.Modules.Missions.Entities;

namespace Spectrometre.Modules.Missions.Services;

/// <summary>
/// Synthèse « Mes progrès » — agrégation en lecture seule (missions + grille + auto-observation).
/// Placée dans Missions pour consommer <see cref="IMissionService"/> sans dépendance circulaire
/// JeunesPrestataires → Missions.
/// </summary>
public sealed record MesProgresView(
    int MissionsTerminees,
    int MissionsEnCours,
    double? GrilleDerniereMoyenne,
    DateTimeOffset? GrilleDerniereEvaluationLe,
    double? GrilleTendanceDelta,
    bool AutoObsSyntheseGeneree,
    DateTimeOffset? AutoObsSyntheseGenereeLe,
    string? AutoObsSyntheseExtrait);

public interface IMesProgresService
{
    /// <summary>Vue agrégée pour le jeune connecté. <c>null</c> si pas de profil jeune.</summary>
    Task<MesProgresView?> GetAsync(string jeuneUserId, CancellationToken cancellationToken = default);
}

public sealed class MesProgresService(
    IJeuneProfileService jeuneProfileService,
    IMissionService missionService,
    IGrilleObservationService grilleObservationService,
    IAutoObservationService autoObservationService) : IMesProgresService
{
    private const int ExtraitMaxLength = 220;
    public const string AutoObservationSyntheseSectionKey = "p2.s13";

    public async Task<MesProgresView?> GetAsync(string jeuneUserId, CancellationToken cancellationToken = default)
    {
        var jeune = await jeuneProfileService.TryGetByUserIdAsync(jeuneUserId, cancellationToken);
        if (jeune is null)
            return null;

        var missions = await missionService.GetMesMissionsAsync(jeuneUserId, cancellationToken);
        var terminees = missions.Count(m => m.MissionStatut == MissionStatut.Terminee);
        var enCours = missions.Count(m => m.MissionStatut == MissionStatut.Attribuee);

        var historique = await grilleObservationService.GetHistoriqueAsync(
            jeuneUserId, jeune.Id, cancellationToken);
        // Historique déjà trié du plus récent au plus ancien (GetHistoriqueInternalAsync).
        double? derniereMoyenne = null;
        DateTimeOffset? derniereLe = null;
        double? tendanceDelta = null;
        if (historique.Count > 0)
        {
            var derniere = historique[0];
            derniereMoyenne = derniere.MoyenneScore;
            derniereLe = derniere.EvalueeLe;
            if (historique.Count > 1
                && derniere.MoyenneScore is double actuelle
                && historique[1].MoyenneScore is double precedente)
            {
                tendanceDelta = actuelle - precedente;
            }
        }

        var autoObs = await autoObservationService.TryGetPageAsync(jeuneUserId, jeune.Id, cancellationToken);
        var synthese = autoObs?.Synthese;
        var aSynthese = synthese is not null;

        return new MesProgresView(
            terminees,
            enCours,
            derniereMoyenne,
            derniereLe,
            tendanceDelta,
            aSynthese,
            aSynthese ? autoObs!.SyntheseGenereeLe : null,
            aSynthese ? Extraire(synthese!) : null);
    }

    private static string Extraire(AutoObservationSyntheseDocument document) =>
        document.Resume(ExtraitMaxLength);
}
