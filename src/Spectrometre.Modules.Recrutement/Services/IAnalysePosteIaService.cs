namespace Spectrometre.Modules.Recrutement.Services;

/// <summary>
/// Appel bas niveau au fournisseur IA pour l'analyse poste/candidature — même contrat que
/// <c>IAiSynthesisService</c> (Gestion du temps). Substituable en test ; JAMAIS d'exception.
/// </summary>
public interface IAnalysePosteIaService
{
    /// <summary>
    /// Texte généré, ou <c>(null, message d'erreur)</c> si non configuré / timeout / erreur —
    /// l'appelant retombe alors sur un texte de repli local.
    /// </summary>
    Task<(string? Output, string? Error)> GenererTexteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}
