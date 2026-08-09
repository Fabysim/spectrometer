namespace Spectrometre.Core.Notifications;

/// <summary>
/// Préférences + pertinence des catégories. Séparé de <see cref="INotificationService"/> pour
/// garder le panneau cloche léger ; <see cref="INotificationService.CreerAsync"/> consulte quand même
/// les préférences avant d'écrire.
/// </summary>
public interface IPreferenceNotificationService
{
    /// <summary>
    /// Catégories pertinentes pour les modules/profils actifs de l'utilisateur.
    /// Sans ligne en base → <c>Active = true</c>.
    /// </summary>
    Task<IReadOnlyList<PreferenceNotificationView>> GetPreferencesAsync(string userId, CancellationToken cancellationToken = default);

    Task SetPreferenceAsync(string userId, string categorieCode, bool active, CancellationToken cancellationToken = default);

    /// <summary><c>true</c> si la catégorie est autorisée (défaut) ou explicitement active.</summary>
    Task<bool> EstCategorieActiveAsync(string userId, string categorieCode, CancellationToken cancellationToken = default);
}
