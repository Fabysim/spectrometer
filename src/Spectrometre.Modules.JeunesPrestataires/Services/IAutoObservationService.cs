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

    /// <summary>
    /// Enregistre les 5 réponses d'orientation (une seule fois), applique la suggestion de
    /// <c>ProfilAccompagnement</c>. N'écrase pas un profil déjà fixé après cette étape
    /// (second appel ignoré — le coach reste prioritaire via la fiche de suivi).
    /// </summary>
    Task<bool> EnregistrerOrientationAsync(
        string requestingUserId,
        int jeuneProfileId,
        IReadOnlyList<AutoObservationAnswerInput> answers,
        CancellationToken cancellationToken = default);

    /// <summary>Ferme l'écran sans changer le profil choisi par le coach à l'invitation.</summary>
    Task<bool> PasserOrientationAsync(
        string requestingUserId,
        int jeuneProfileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Le coach suiveur confirme avoir relu la synthèse auto-générée (horodatage + UserId).
    /// N'édite pas le texte. Une régénération efface cette validation.
    /// </summary>
    Task<bool> ValiderSyntheseAsync(
        string requestingCoachUserId,
        int jeuneProfileId,
        CancellationToken cancellationToken = default);
}
