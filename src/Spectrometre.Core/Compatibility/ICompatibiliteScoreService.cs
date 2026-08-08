namespace Spectrometre.Core.Compatibility;

/// <summary>
/// Snapshot de scores pour les consommateurs hors module Compatibilité (ex. index recrutement /
/// listes de candidatures) — évite qu'un module « socle » comme ProfilEntreprise référence
/// directement <c>Spectrometre.Modules.Compatibilite</c> (dépendance circulaire : Compatibilite
/// consomme déjà <c>ICompanyProfileService</c>).
/// </summary>
public sealed record CompatibiliteScoresSnapshot(
    int ScoreGlobal,
    int? Technique,
    int? Comportementale,
    int? Culturelle,
    int? Organisationnelle,
    int? Motivationnelle,
    IReadOnlyList<string> PointsVigilanceTags);

/// <summary>
/// Inversion de dépendance : Compatibilite enregistre l'implémentation ; ProfilEntreprise
/// (PosteService) consomme uniquement ce contrat Core.
/// </summary>
public interface ICompatibiliteScoreService
{
    Task<CompatibiliteScoresSnapshot?> CalculerScoresAsync(
        int candidateProfileId,
        int companyProfileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Score global pour affichage liste : lit le dernier score stocké, sinon estime sans persister.
    /// </summary>
    Task<int?> GetScoreGlobalAffichageAsync(
        int candidateProfileId,
        int companyProfileId,
        CancellationToken cancellationToken = default);
}
