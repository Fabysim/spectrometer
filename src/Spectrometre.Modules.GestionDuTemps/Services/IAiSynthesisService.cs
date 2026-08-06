namespace Spectrometre.Modules.GestionDuTemps.Services;

/// <summary>
/// Appel bas niveau au fournisseur IA — substituable en test (voir le projet de tests, qui enregistre une
/// implémentation factice à la place de celle-ci, jamais un vrai appel réseau). Ne construit aucun prompt
/// métier lui-même : reçoit des prompts déjà formés et retourne le texte généré tel quel, à charge de
/// l'appelant (voir <c>GestionDuTempsService.GenererSyntheseAsync</c>) de l'interpréter.
/// </summary>
public interface IAiSynthesisService
{
    /// <summary>
    /// Texte généré, ou <c>(null, message d'erreur)</c> si le service n'est pas configuré (pas de clé API),
    /// en timeout, ou en erreur — JAMAIS d'exception : l'appelant retombe alors sur un texte généré
    /// localement plutôt que de faire planter la fonctionnalité ou de propager une erreur brute.
    /// </summary>
    Task<(string? Output, string? Error)> GenererTexteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}
