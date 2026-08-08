namespace Spectrometre.Core.Email;

/// <summary>
/// Options du service d'email Resend (clé API, expéditeur, nom de l'application) — mêmes clés que mvp
/// (<c>Resend:ApiKey</c>/<c>Resend:From</c>/<c>Resend:AppName</c>, variable d'environnement
/// <c>RESEND_API_KEY</c> en repli), pour ne pas dupliquer le provisionnement de secret côté déploiement.
/// </summary>
public sealed class ResendEmailServiceOptions
{
    public const string SectionName = "Resend";

    /// <summary>Clé API Resend. Vide par défaut : voir <see cref="IResendEmailService"/> pour le comportement dans ce cas.</summary>
    public string ApiKey { get; set; } = string.Empty;

    public string From { get; set; } = "Spectromètre <noreply@thinkeens.com>";

    public string AppName { get; set; } = "Spectromètre";
}
