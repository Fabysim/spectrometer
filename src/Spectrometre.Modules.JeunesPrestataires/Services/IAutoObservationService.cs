namespace Spectrometre.Modules.JeunesPrestataires.Services;

public interface IAutoObservationService
{
    Task<AutoObservationPageView?> TryGetPageAsync(
        string requestingUserId,
        int? jeuneProfileId = null,
        CancellationToken cancellationToken = default);

    Task<AutoObservationSectionView?> TryGetSectionAsync(
        string requestingUserId,
        int jeuneProfileId,
        string sectionKey,
        CancellationToken cancellationToken = default);

    Task<bool> SaveSectionAsync(
        string requestingUserId,
        int jeuneProfileId,
        string sectionKey,
        IReadOnlyList<AutoObservationAnswerInput> answers,
        CancellationToken cancellationToken = default);

    Task<bool> DemanderAideAsync(
        string requestingUserId,
        int jeuneProfileId,
        string sectionKey,
        CancellationToken cancellationToken = default);

    Task<string?> RegenererSyntheseAsync(
        string requestingUserId,
        int jeuneProfileId,
        CancellationToken cancellationToken = default);
}
