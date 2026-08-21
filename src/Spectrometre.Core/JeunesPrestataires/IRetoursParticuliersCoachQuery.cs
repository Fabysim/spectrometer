namespace Spectrometre.Core.JeunesPrestataires;

/// <summary>
/// Retour d'un particulier après mission — lecture coach suiveur uniquement
/// (jamais exposé au jeune via cette API).
/// </summary>
public sealed record RetourParticulierCoachItem(
    int MissionAcceptationId,
    string MissionTitre,
    DateTimeOffset EvalueeLe,
    bool? Ponctualite,
    bool? ConsignesComprises,
    bool? TacheRealiseeCorrectement,
    bool? AttitudeRespectueuse,
    string? PointsPositifs,
    string? PointsAAmeliorer,
    bool? AccepteraitNouvelleMission);

/// <summary>
/// Historique des évaluations particulier pour informer la grille d'observation.
/// Interface Core pour que JeunesPrestataires puisse l'injecter sans référencer Missions.
/// </summary>
public interface IRetoursParticuliersCoachQuery
{
    /// <summary>
    /// Coach suiveur uniquement (même garde que la grille d'observation :
    /// GetSuiviUserIdSiAutoriseAsync). Liste vide si non autorisé, si le demandeur
    /// est le jeune, ou s'il n'y a aucun retour.
    /// Tri du plus récent au plus ancien (<c>UpdatedAt</c>).
    /// </summary>
    Task<IReadOnlyList<RetourParticulierCoachItem>> GetHistoriquePourCoachAsync(
        string requestingUserId,
        int jeuneProfileId,
        CancellationToken cancellationToken = default);
}
