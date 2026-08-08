namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Substitut de <see cref="Spectrometre.Modules.Recrutement.Services.IAnalysePosteIaService"/> —
/// jamais d'appel réseau. Par défaut renvoie une erreur pour forcer le repli local (comme une clé absente).
/// Capture les prompts pour assert sur le contenu envoyé au modèle.
/// </summary>
public sealed class FakeAnalysePosteIaService : Spectrometre.Modules.Recrutement.Services.IAnalysePosteIaService
{
    public string? Reponse { get; set; }
    public string? Erreur { get; set; } = "IA non configurée en test.";
    public string? LastSystemPrompt { get; private set; }
    public string? LastUserPrompt { get; private set; }
    public int CallCount { get; private set; }

    public void ResetCaptures()
    {
        LastSystemPrompt = null;
        LastUserPrompt = null;
        CallCount = 0;
    }

    public Task<(string? Output, string? Error)> GenererTexteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        LastSystemPrompt = systemPrompt;
        LastUserPrompt = userPrompt;
        CallCount++;
        return Task.FromResult<(string?, string?)>(Erreur is not null ? (null, Erreur) : (Reponse, null));
    }
}
