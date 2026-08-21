using Spectrometre.Modules.ProfilCandidat.Services;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Substitut de <see cref="ICvImportIaService"/> — jamais d'appel réseau.
/// Configurable via <see cref="Brouillon"/>.
/// </summary>
public sealed class FakeCvImportIaService : ICvImportIaService
{
    public CvView? Brouillon { get; set; }

    public int Appels { get; private set; }

    public void ResetAppels() => Appels = 0;

    public Task<CvView?> ExtraireCvAsync(string texteDocument, CancellationToken cancellationToken = default)
    {
        Appels++;
        return Task.FromResult(Brouillon);
    }
}
