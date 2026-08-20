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

    Task<ConsentementConfirmationResult> ConfirmerAsync(
        int jeuneProfileId,
        string nomJeune,
        string nomParent1,
        string? nomParent2,
        CancellationToken cancellationToken = default);

    Task<bool> EstConsentementValideAsync(int jeuneProfileId, CancellationToken cancellationToken = default);
}
