namespace Spectrometre.Modules.ProfilEntreprise.Services;

/// <summary>
/// Génère une offre d'emploi .docx via Claude (Replicate) — même idée que le MVP
/// <c>IJobOfferDraftService</c>. JAMAIS d'exception vers l'appelant : erreurs en string nullable.
/// </summary>
public interface IJobOfferDraftService
{
    /// <summary>
    /// Résout le poste et ses critères dans le tenant ambiant, appelle l'IA, puis produit un .docx.
    /// </summary>
    Task<(byte[]? Content, string? FileName, string? Erreur)> GenererOffreDocxAsync(
        int posteId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Génère le texte d'offre (IA ou repli local) et le persiste sur le poste du tenant ambiant.
    /// <paramref name="Erreur"/> n'est renseigné que si le poste est introuvable — un échec IA
    /// produit toujours un texte de repli (jamais de page vide côté candidat).
    /// </summary>
    Task<(string? Texte, string? Erreur)> GenererEtEnregistrerOffreAsync(
        int posteId,
        CancellationToken cancellationToken = default);
}
