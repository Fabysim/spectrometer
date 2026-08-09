using Spectrometre.Core.Ai;

namespace Spectrometre.Modules.SuiviEmployes.Services;

/// <summary>
/// Adaptateur IA SuiviEmployes → <see cref="IReplicateService"/> (même pattern que Recrutement).
/// Substituable en test ; JAMAIS d'exception.
/// </summary>
public interface IAnalyseEmployeIaService
{
    Task<(string? Output, string? Error)> GenererTexteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}

public sealed class ReplicateAnalyseEmployeIaService(IReplicateService replicate) : IAnalyseEmployeIaService
{
    public Task<(string? Output, string? Error)> GenererTexteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default) =>
        replicate.RunClaudeAsync(systemPrompt, userPrompt, cancellationToken);
}
