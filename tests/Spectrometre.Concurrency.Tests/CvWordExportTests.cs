using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Modules.ProfilCandidat.Entities;
using Spectrometre.Modules.ProfilCandidat.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>Export Word du CV (voir <see cref="ICvWordService"/>) — mise en page uniquement, jamais un nouvel accès aux données.</summary>
[Collection("Base de données partagée")]
public sealed class CvWordExportTests(ServiceFixture fixture)
{
    [Fact]
    public async Task GenerateCvWord_AvecUnCvRempli_ProduitUnDocxNonVide()
    {
        var candidateService = fixture.Services.GetRequiredService<ICandidateProfileService>();
        var wordService = fixture.Services.GetRequiredService<ICvWordService>();

        var userId = $"cv-word-test-{Guid.NewGuid()}";
        var candidateProfileId = await candidateService.GetOrCreateProfileIdAsync(userId);
        await candidateService.SaveCoordonneesAsync(candidateProfileId, new CvCoordonnees
        {
            Nom = "Dupont",
            Prenoms = "Jean",
            Email = "jean.dupont@example.test",
            ProfilOuPosteRecherche = "Développeur",
        });

        var cv = await candidateService.GetCvAsync(candidateProfileId);
        var wordBytes = wordService.GenerateCvWord(cv);

        Assert.NotEmpty(wordBytes);
        // Signature ZIP d'un fichier OOXML (.docx) — même idée que "%PDF" pour CvPdfService.
        Assert.Equal("PK", System.Text.Encoding.ASCII.GetString(wordBytes, 0, 2));
    }

    [Fact]
    public async Task GenerateCvWord_AvecUnCvVide_NeLevePas()
    {
        var candidateService = fixture.Services.GetRequiredService<ICandidateProfileService>();
        var wordService = fixture.Services.GetRequiredService<ICvWordService>();

        var userId = $"cv-word-test-vide-{Guid.NewGuid()}";
        var candidateProfileId = await candidateService.GetOrCreateProfileIdAsync(userId);

        var cv = await candidateService.GetCvAsync(candidateProfileId);
        var wordBytes = wordService.GenerateCvWord(cv);

        Assert.NotEmpty(wordBytes);
        Assert.Equal("PK", System.Text.Encoding.ASCII.GetString(wordBytes, 0, 2));
    }
}
