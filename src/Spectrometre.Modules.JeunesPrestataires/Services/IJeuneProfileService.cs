using Spectrometre.Core.Invitations;
using Spectrometre.Core.JeunesPrestataires;

namespace Spectrometre.Modules.JeunesPrestataires.Services;

public interface IJeuneProfileService
{
    Task<JeuneProfileView?> TryGetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<InvitationJeunePrestataireDraft?> TryGetDraftForInvitationAsync(int invitationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Crée le profil jeune à partir d'une invitation acceptée. Idempotent si le profil existe déjà pour cet utilisateur.
    /// </summary>
    Task<JeuneProfileView> FinaliserDepuisInvitationAsync(
        Invitation invitation,
        string accepteurUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Émet une invitation jeune (coach = émetteur). Tout âge accepté — le consentement parental ne s'applique qu'aux mineurs.
    /// </summary>
    Task<InviterJeuneResult> InviterJeuneAsync(
        string coachUserId,
        string email,
        string nom,
        string prenoms,
        DateOnly dateNaissance,
        string lienAcceptationBaseUrl,
        CancellationToken cancellationToken = default);

    /// <summary>Calcule l'âge à partir de la date de naissance (affichage).</summary>
    int CalculerAge(DateOnly dateNaissance);

    /// <summary>Indique si le jeune est mineur (&lt; 18 ans) — utile pour le consentement parental.</summary>
    bool EstMineur(DateOnly dateNaissance);

    /// <summary>
    /// Renvoie l'e-mail d'invitation jeune prestataire. Si <see cref="Invitation.ExpireLe"/> est dépassée
    /// mais le statut reste <see cref="InvitationStatus.EnAttente"/>, révoque l'ancienne invitation et en crée
    /// une nouvelle (nouveau token, nouvelle expiration) plutôt que d'envoyer un lien mort.
    /// </summary>
    Task<RenvoyerJeunePrestataireInvitationResult> RenvoyerInvitationAsync(
        int invitationId,
        string requestingCoachUserId,
        string baseUrl,
        CancellationToken cancellationToken = default);
}
