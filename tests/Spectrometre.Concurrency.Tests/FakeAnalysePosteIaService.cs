namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Substitut de <see cref="Spectrometre.Modules.PostesRecrutement.Services.IAnalysePosteIaService"/> —
/// jamais d'appel réseau. Par défaut renvoie une erreur pour forcer le repli local (comme une clé absente).
/// </summary>
public sealed class FakeAnalysePosteIaService : Spectrometre.Modules.PostesRecrutement.Services.IAnalysePosteIaService
{
    public string? Reponse { get; set; }
    public string? Erreur { get; set; } = "IA non configurée en test.";

    public Task<(string? Output, string? Error)> GenererTexteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<(string?, string?)>(Erreur is not null ? (null, Erreur) : (Reponse, null));
}
