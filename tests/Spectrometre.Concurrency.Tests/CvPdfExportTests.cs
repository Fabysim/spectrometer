using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Modules.ProfilCandidat.Entities;
using Spectrometre.Modules.ProfilCandidat.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>Export PDF du CV (voir <see cref="ICvPdfService"/>) — vérifie seulement la mise en page à partir d'un CV déjà chargé, jamais un nouvel accès aux données.</summary>
[Collection("Base de données partagée")]
public sealed class CvPdfExportTests(ServiceFixture fixture)
{
    [Fact]
    public async Task GenerateCvPdf_AvecUnCvRempli_ProduitUnPdfNonVide()
    {
        var candidateService = fixture.Services.GetRequiredService<ICandidateProfileService>();
        var pdfService = fixture.Services.GetRequiredService<ICvPdfService>();

        var userId = $"cv-pdf-test-{Guid.NewGuid()}";
        var candidateProfileId = await candidateService.GetOrCreateProfileIdAsync(userId);
        await candidateService.SaveCoordonneesAsync(candidateProfileId, new CvCoordonnees
        {
            Nom = "Dupont",
            Prenoms = "Jean",
            Email = "jean.dupont@example.test",
            ProfilOuPosteRecherche = "Développeur",
        });

        var cv = await candidateService.GetCvAsync(candidateProfileId);
        var pdfBytes = pdfService.GenerateCvPdf(cv);

        Assert.NotEmpty(pdfBytes);
        // En-tête standard d'un fichier PDF ("%PDF") — confirme un document réellement généré, pas un flux vide.
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 4));
    }

    [Fact]
    public async Task GenerateCvPdf_AvecUnCvVide_NeLevePas()
    {
        var candidateService = fixture.Services.GetRequiredService<ICandidateProfileService>();
        var pdfService = fixture.Services.GetRequiredService<ICvPdfService>();

        var userId = $"cv-pdf-test-vide-{Guid.NewGuid()}";
        var candidateProfileId = await candidateService.GetOrCreateProfileIdAsync(userId);

        var cv = await candidateService.GetCvAsync(candidateProfileId);
        var pdfBytes = pdfService.GenerateCvPdf(cv);

        Assert.NotEmpty(pdfBytes);
    }
}
