using Spectrometre.Modules.GestionDuTemps.Services;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Substitut de <see cref="IAiSynthesisService"/> pour les tests — jamais d'appel réseau réel à Replicate.
/// Comportement configurable par test via <see cref="Reponse"/>/<see cref="Erreur"/> (mutable, un scope DI
/// par test grâce à <c>AddScoped</c> dans <c>ServiceFixture</c> ne suffirait pas à isoler ces champs entre
/// tests parallèles : chaque test doit donc résoudre sa propre instance via un scope dédié plutôt que de
/// muter un singleton partagé — voir les tests de <c>GestionDuTempsSyntheseTests</c>).
/// </summary>
public sealed class FakeAiSynthesisService : IAiSynthesisService
{
    /// <summary>Sortie JSON à renvoyer comme si elle venait de Claude — <c>null</c> pour simuler l'absence de contenu.</summary>
    public string? Reponse { get; set; }

    /// <summary>Message d'erreur à renvoyer (simulate un échec réseau/timeout/clé absente) — a priorité sur <see cref="Reponse"/>.</summary>
    public string? Erreur { get; set; }

    public Task<(string? Output, string? Error)> GenererTexteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default) =>
        Task.FromResult<(string?, string?)>(Erreur is not null ? (null, Erreur) : (Reponse, null));
}
