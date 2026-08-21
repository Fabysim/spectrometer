using Spectrometre.Modules.JeunesPrestataires.Services;

namespace Spectrometre.Modules.JeunesPrestataires.Services;

public interface IConsentementParentalService
{
    Task<ConsentementParentalView> GetAsync(int jeuneProfileId, CancellationToken cancellationToken = default);

    Task SaveBrouillonAsync(
        int jeuneProfileId,
        ConsentementParentalFormModel form,
        CancellationToken cancellationToken = default);

    Task ReprendreEditionAsync(int jeuneProfileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finalise le consentement. Si succès et première validation d'un mineur, notifie le coach
    /// référent (<c>JeunesPrestataires.ConsentementConfirme</c>). Service propre au module
    /// jeunes prestataires ; un majeur n'est pas notifié (hors périmètre dashboard).
    /// </summary>
    Task<ConsentementConfirmationResult> ConfirmerAsync(
        int jeuneProfileId,
        string nomJeune,
        string nomParent1,
        string? nomParent2,
        CancellationToken cancellationToken = default);

    Task<bool> EstConsentementValideAsync(int jeuneProfileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Coordonnées des représentants légaux + engagements (section 6), pour un coach suiveur.
    /// <c>null</c> si non autorisé, profil introuvable, ou consentement pas encore validé.
    /// N'altère pas <see cref="GetAsync"/> (écran jeune inchangé).
    /// </summary>
    Task<ConsentementParentalCoachView?> TryGetPourCoachAsync(
        string coachUserId,
        int jeuneProfileId,
        CancellationToken cancellationToken = default);
}
