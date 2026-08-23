using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;
using Spectrometre.Core.Resources;

namespace Spectrometre.Core.Email;

/// <summary>
/// Implémentation réelle de <see cref="IResendEmailService"/> — même template HTML et mêmes clés de
/// ressources que mvp (<c>Spectrometre.Services.ResendEmailService</c>), porté tel quel pour le seul email
/// utilisé ici (confirmation de compte).
/// </summary>
public sealed class ResendEmailService(
    IResend resend,
    IOptions<ResendEmailServiceOptions> options,
    IStringLocalizer<EmailResource> localizer,
    ILogger<ResendEmailService> logger) : IResendEmailService
{
    private readonly ResendEmailServiceOptions _options = options.Value;

    private const string EmailTemplate = """
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>{{title}}</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif; background-color: #f9fafb; margin: 0; padding: 0; }
        .container { max-width: 600px; margin: 20px auto; background: #ffffff; border-radius: 8px; overflow: hidden; border: 1px solid #e5e7eb; }
        .header { padding: 40px 20px; text-align: center; background-color: #ffffff; }
        .content { padding: 20px 40px; color: #374151; line-height: 1.6; }
        .button-container { text-align: center; padding: 30px 0; }
        .button { background-color: #000000; color: #ffffff !important; padding: 12px 30px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block; }
        .footer { padding: 20px; text-align: center; color: #9ca3af; font-size: 12px; }
        @media (prefers-color-scheme: dark) {
            body { background-color: #111827; }
            .container { background-color: #1f2937; border-color: #374151; }
            .content { color: #d1d5db; }
            .button { background-color: #ffffff; color: #111827 !important; }
        }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1 style="margin:0; font-size: 24px; color: #111827;">{{appName}}</h1>
        </div>
        <div class="content">
            <p>{{hello}}</p>
            <p>{{body}}</p>
            <div class="button-container">
                <a href="{{verificationLink}}" class="button">{{button}}</a>
            </div>
            <p style="font-size: 14px; color: #6b7280;">
                {{linkFallback}}<br>
                <a href="{{verificationLink}}" style="color: #3b82f6;">{{verificationLink}}</a>
            </p>
        </div>
        <div class="footer">
            {{footer}}
        </div>
    </div>
</body>
</html>
""";

    /// <inheritdoc />
    public Task<bool> SendConfirmationEmailAsync(string email, string confirmationLink, CancellationToken cancellationToken = default)
    {
        var appName = ResolveAppName();
        var year = DateTime.UtcNow.Year.ToString();
        var html = BuildHtmlEmail(
            localizer["Email_Confirmation_Title"].Value,
            localizer["Email_Confirmation_Body"].Value,
            localizer["Email_Confirmation_Button"].Value,
            string.Format(localizer["Email_Confirmation_Footer"].Value, year, appName),
            confirmationLink);

        return SendEmailAsync(
            email,
            localizer["Email_Confirmation_Subject"].Value,
            html,
            "confirmation",
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> SendJeunePrestataireInvitationEmailAsync(
        string email,
        string coachNomAffiche,
        string acceptationLink,
        CancellationToken cancellationToken = default)
    {
        var appName = ResolveAppName();
        var year = DateTime.UtcNow.Year.ToString();
        var html = BuildHtmlEmail(
            localizer["Email_InvitationJeune_Title"].Value,
            string.Format(localizer["Email_InvitationJeune_Body"].Value, coachNomAffiche),
            localizer["Email_InvitationJeune_Button"].Value,
            string.Format(localizer["Email_InvitationJeune_Footer"].Value, year, appName),
            acceptationLink);

        return SendEmailAsync(
            email,
            localizer["Email_InvitationJeune_Subject"].Value,
            html,
            "invitation jeune prestataire",
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> SendPasswordResetEmailAsync(string email, string resetLink, CancellationToken cancellationToken = default)
    {
        var appName = ResolveAppName();
        var year = DateTime.UtcNow.Year.ToString();
        var html = BuildHtmlEmail(
            localizer["Email_ResetPassword_Title"].Value,
            localizer["Email_ResetPassword_Body"].Value,
            localizer["Email_ResetPassword_Button"].Value,
            string.Format(localizer["Email_ResetPassword_Footer"].Value, year, appName),
            resetLink);

        return SendEmailAsync(
            email,
            localizer["Email_ResetPassword_Subject"].Value,
            html,
            "réinitialisation de mot de passe",
            cancellationToken);
    }

    private string ResolveAppName() =>
        string.IsNullOrWhiteSpace(_options.AppName) ? "Spectromètre" : _options.AppName;

    private string BuildHtmlEmail(string title, string body, string buttonText, string footer, string actionLink)
    {
        var appName = ResolveAppName();
        var year = DateTime.UtcNow.Year.ToString();
        return EmailTemplate
            .Replace("{{title}}", title)
            .Replace("{{hello}}", localizer["Email_Hello"].Value)
            .Replace("{{body}}", body)
            .Replace("{{button}}", buttonText)
            .Replace("{{linkFallback}}", localizer["Email_LinkFallback"].Value)
            .Replace("{{footer}}", footer)
            .Replace("{{verificationLink}}", actionLink)
            .Replace("{{appName}}", appName)
            .Replace("{{year}}", year);
    }

    private async Task<bool> SendEmailAsync(
        string email,
        string subject,
        string html,
        string emailKind,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            logger.LogError("Impossible d'envoyer l'email {EmailKind} à {Email} : Resend:ApiKey n'est pas configuré.", emailKind, email);
            return false;
        }

        var message = new EmailMessage
        {
            From = _options.From,
            To = { email },
            Subject = subject,
            HtmlBody = html,
        };

        try
        {
            await resend.EmailSendAsync(message, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Échec de l'envoi de l'email {EmailKind} à {Email} via Resend.", emailKind, email);
            return false;
        }
    }
}
