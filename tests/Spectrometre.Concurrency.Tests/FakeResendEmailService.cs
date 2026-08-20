using Spectrometre.Core.Email;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Double Resend pour les tests : simule l'absence de clé API (retourne toujours <see langword="false"/>).
/// </summary>
public sealed class FakeResendEmailService : IResendEmailService
{
    public Task<bool> SendConfirmationEmailAsync(string email, string confirmationLink, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> SendJeunePrestataireInvitationEmailAsync(
        string email,
        string coachNomAffiche,
        string acceptationLink,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
