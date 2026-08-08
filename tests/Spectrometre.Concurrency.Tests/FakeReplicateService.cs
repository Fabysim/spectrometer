using Spectrometre.Core.Ai;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Substitut de <see cref="IReplicateService"/> — jamais d'appel réseau.
/// Utilisé notamment par <c>JobOfferDraftService</c> (ProfilEntreprise).
/// </summary>
public sealed class FakeReplicateService : IReplicateService
{
    public string? Reponse { get; set; }
    public string? Erreur { get; set; }

    public Task<(string? Output, string? Error)> RunClaudeAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<(string?, string?)>(Erreur is not null ? (null, Erreur) : (Reponse, null));

    public Task<(string? Transcript, string? Error)> TranscribeAudioAsync(
        byte[] audioBytes,
        string mimeType,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<(string?, string?)>((null, "Transcription non disponible en test."));
}
