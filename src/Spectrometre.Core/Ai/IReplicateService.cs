namespace Spectrometre.Core.Ai;

/// <summary>
/// Service d'appel à l'API Replicate (modèles IA, ex. Claude) — même contrat que mvp
/// (<c>Spectrometre.Services.IReplicateService</c>). Clé : <c>Replicate:ApiToken</c> (user secrets)
/// ou variable d'environnement <c>REPLICATE_API_TOKEN</c>.
/// </summary>
public interface IReplicateService
{
    /// <summary>
    /// Exécute une prédiction Claude avec les prompts fournis.
    /// </summary>
    /// <returns>Texte généré, ou (null, message d'erreur) si échec ou non configuré.</returns>
    Task<(string? Output, string? Error)> RunClaudeAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);

    /// <summary>Transcrit un flux audio via Whisper (Replicate).</summary>
    Task<(string? Transcript, string? Error)> TranscribeAudioAsync(
        byte[] audioBytes,
        string mimeType,
        CancellationToken cancellationToken = default);
}
