namespace Spectrometre.Core.JeunesPrestataires;

/// <summary>
/// Consultation et révocation des invitations jeune prestataire émises par un coach — interface Core pour
/// éviter une dépendance circulaire entre les modules Coaching et JeunesPrestataires.
/// </summary>
public interface IJeunePrestataireInvitationQuery
{
    Task<IReadOnlyList<JeunePrestataireInvitationPendingView>> GetInvitationsEnvoyeesEnAttenteAsync(
        string coachUserId,
        CancellationToken cancellationToken = default);

    Task<bool> RevoquerInvitationEnvoyeeAsync(
        int invitationId,
        string coachUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renvoie l'e-mail d'invitation jeune prestataire — même token si encore valide, nouvelle invitation si expirée.
    /// </summary>
    Task<RenvoyerJeunePrestataireInvitationResult> RenvoyerInvitationAsync(
        int invitationId,
        string requestingCoachUserId,
        string baseUrl,
        CancellationToken cancellationToken = default);
}
