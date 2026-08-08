using Spectrometre.Core.Ai;

namespace Spectrometre.Modules.PostesRecrutement.Services;

/// <summary>
/// Adaptateur PostesRecrutement → <see cref="IReplicateService"/> (même Claude/Replicate que GDT et le MVP).
/// Les tests substituent <see cref="IAnalysePosteIaService"/> ; le noyau conserve <see cref="IReplicateService"/>.
/// </summary>
public sealed class ReplicateAnalysePosteIaService(IReplicateService replicate) : IAnalysePosteIaService
{
    public Task<(string? Output, string? Error)> GenererTexteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default) =>
        replicate.RunClaudeAsync(systemPrompt, userPrompt, cancellationToken);
}
