namespace Spectrometre.Modules.ProfilEntreprise.Services;

/// <summary>
/// Génère une offre d'emploi .docx via Claude (Replicate) — même idée que le MVP
/// <c>IJobOfferDraftService</c>. JAMAIS d'exception : retourne
/// <c>(null, null, message)</c> si le poste est introuvable ou si l'IA échoue.
/// </summary>
public interface IJobOfferDraftService
{
    /// <summary>
    /// Résout le poste et ses critères dans le tenant ambiant, appelle l'IA, puis produit un .docx.
    /// </summary>
    Task<(byte[]? Content, string? FileName, string? Erreur)> GenererOffreDocxAsync(
        int posteId,
        CancellationToken cancellationToken = default);
}
