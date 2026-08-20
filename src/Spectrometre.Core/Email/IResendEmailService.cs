namespace Spectrometre.Core.Email;

/// <summary>
/// Service d'envoi d'emails via l'API Resend — repris de mvp (<c>Spectrometre.Services.IResendEmailService</c>),
/// réduit pour l'instant à la confirmation de compte (seul usage demandé côté inscription), pensé extensible
/// pour d'autres emails transactionnels plus tard (réinitialisation de mot de passe, invitations) sans
/// changement de forme.
/// </summary>
public interface IResendEmailService
{
    /// <summary>
    /// Envoie l'email de vérification de compte avec le lien de confirmation, dans la culture courante
    /// (<see cref="System.Globalization.CultureInfo.CurrentUICulture"/> au moment de l'appel).
    /// </summary>
    /// <returns>
    /// <see langword="false"/> si <c>Resend:ApiKey</c> n'est pas configuré ou si l'appel à l'API Resend
    /// échoue — jamais d'exception propagée : l'inscription elle-même a déjà réussi à ce stade (le compte
    /// existe), c'est à l'appelant de décider comment informer l'utilisateur d'un email non envoyé.
    /// </returns>
    Task<bool> SendConfirmationEmailAsync(string email, string confirmationLink, CancellationToken cancellationToken = default);

    /// <summary>
    /// Envoie l'email d'invitation jeune prestataire avec le lien d'acceptation, dans la culture courante.
    /// </summary>
    /// <returns>
    /// <see langword="false"/> si <c>Resend:ApiKey</c> n'est pas configuré ou si l'appel à l'API Resend
    /// échoue — jamais d'exception propagée : l'invitation a déjà été créée à ce stade.
    /// </returns>
    Task<bool> SendJeunePrestataireInvitationEmailAsync(
        string email,
        string coachNomAffiche,
        string acceptationLink,
        CancellationToken cancellationToken = default);
}
