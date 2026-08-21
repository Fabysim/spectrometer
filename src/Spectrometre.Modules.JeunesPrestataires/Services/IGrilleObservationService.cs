namespace Spectrometre.Modules.JeunesPrestataires.Services;

public interface IGrilleObservationService
{
    Task<GrilleObservationPageView?> TryGetPageAsync(
        string requestingUserId,
        int? jeuneProfileId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GrilleObservationHistoriqueItemView>> GetHistoriqueAsync(
        string requestingUserId,
        int jeuneProfileId,
        CancellationToken cancellationToken = default);

    Task<GrilleObservationEvaluationDetailView?> TryGetEvaluationAsync(
        string requestingUserId,
        int evaluationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Coach autorisé uniquement — retourne l'id de la nouvelle évaluation ou <c>null</c> si refusé.
    /// Notifie le jeune (<c>JeunesPrestataires.GrilleObservationAjoutee</c>) sans scores ni commentaires.
    /// </summary>
    Task<int?> CreerEvaluationAsync(
        string coachUserId,
        int jeuneProfileId,
        IReadOnlyList<GrilleObservationCritereInput> criteres,
        string? commentaireGeneral,
        CancellationToken cancellationToken = default);
}
