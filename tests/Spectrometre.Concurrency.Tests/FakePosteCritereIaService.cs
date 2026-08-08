using Spectrometre.Modules.ProfilEntreprise.Services;
namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Substitut de <see cref="Spectrometre.Modules.ProfilEntreprise.Services.IPosteCritereIaService"/> —
/// jamais d'appel réseau. Configurable via <see cref="Suggestions"/>.
/// </summary>
public sealed class FakePosteCritereIaService : Spectrometre.Modules.ProfilEntreprise.Services.IPosteCritereIaService
{
    public List<(string Categorie, string Libelle, int NiveauRequis)> Suggestions { get; } = [];

    public int Appels { get; private set; }

    public void ResetAppels() => Appels = 0;

    public Task<IReadOnlyList<(string Categorie, string Libelle, int NiveauRequis)>> SuggererCriteresAsync(
        string titrePoste,
        string? description,
        string? tachesDescription,
        string? competencesRequises,
        CancellationToken cancellationToken = default)
    {
        Appels++;
        return Task.FromResult<IReadOnlyList<(string, string, int)>>(Suggestions.ToList());
    }
}
