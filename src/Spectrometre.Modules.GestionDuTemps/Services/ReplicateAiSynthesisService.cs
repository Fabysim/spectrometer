using Spectrometre.Core.Ai;

namespace Spectrometre.Modules.GestionDuTemps.Services;

/// <summary>
/// Adaptateur GDT → <see cref="IReplicateService"/> (même service Claude/Replicate que mvp).
/// Les tests substituent <see cref="IAiSynthesisService"/> ; le noyau conserve <see cref="IReplicateService"/>.
/// </summary>
public sealed class ReplicateAiSynthesisService(IReplicateService replicate) : IAiSynthesisService
{
    public Task<(string? Output, string? Error)> GenererTexteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default) =>
        replicate.RunClaudeAsync(systemPrompt, userPrompt, cancellationToken);
}
