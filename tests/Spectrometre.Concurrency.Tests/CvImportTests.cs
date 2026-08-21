using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Spectrometre.Modules.ProfilCandidat.Entities;
using Spectrometre.Modules.ProfilCandidat.Services;
using Xunit;
using WordDocument = DocumentFormat.OpenXml.Wordprocessing.Document;

namespace Spectrometre.Concurrency.Tests;

[Collection("Base de données partagée")]
public sealed class CvImportTests(ServiceFixture fixture)
{
    [Fact]
    public async Task ExtraireAsync_PdfSimple_RetourneLeTexte()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var pdf = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Content().Text("Jean Dupont — développeur web 0470123456");
            });
        }).GeneratePdf();

        var extractor = new CvDocumentTextExtractor();
        await using var stream = new MemoryStream(pdf);
        var texte = await extractor.ExtraireAsync(stream, "cv.pdf");

        Assert.NotNull(texte);
        Assert.Contains("Jean Dupont", texte, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExtraireAsync_DocxSimple_RetourneLeTexte()
    {
        await using var docx = CreerDocx("Marie Martin développeuse");
        var extractor = new CvDocumentTextExtractor();
        var texte = await extractor.ExtraireAsync(docx, "cv.docx");

        Assert.NotNull(texte);
        Assert.Contains("Marie Martin", texte, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImporterAsync_FormatRefuse_SansAppelIa()
    {
        var fakeIa = (FakeCvImportIaService)fixture.Services.GetRequiredService<ICvImportIaService>();
        fakeIa.ResetAppels();
        var import = fixture.Services.GetRequiredService<ICvImportService>();

        await using var stream = new MemoryStream("pas un cv"u8.ToArray());
        var result = await import.ImporterAsync(stream, "notes.txt", "text/plain", 10);

        Assert.False(result.Success);
        Assert.Equal("Cv_Import_FormatRefuse", result.MessageKey);
        Assert.Equal(0, fakeIa.Appels);
    }

    [Fact]
    public async Task ImporterAsync_FichierTropVolumineux_SansAppelIa()
    {
        var fakeIa = (FakeCvImportIaService)fixture.Services.GetRequiredService<ICvImportIaService>();
        fakeIa.ResetAppels();
        var import = fixture.Services.GetRequiredService<ICvImportService>();

        await using var stream = new MemoryStream([1, 2, 3]);
        var result = await import.ImporterAsync(
            stream, "cv.pdf", "application/pdf", ICvDocumentTextExtractor.TailleMaxOctets + 1);

        Assert.False(result.Success);
        Assert.Equal("Cv_Import_FichierTropVolumineux", result.MessageKey);
        Assert.Equal(0, fakeIa.Appels);
    }

    [Fact]
    public async Task ImporterAsync_PdfAvecFakeIa_RetourneBrouillonSansEcrire()
    {
        var fakeIa = (FakeCvImportIaService)fixture.Services.GetRequiredService<ICvImportIaService>();
        fakeIa.Brouillon = new CvView(
            new CvCoordonnees { Nom = "Dupont", Prenoms = "Jean" },
            [],
            null,
            [],
            null,
            null,
            [],
            null);
        fakeIa.ResetAppels();

        QuestPDF.Settings.License = LicenseType.Community;
        var pdf = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page => page.Content().Text("Jean Dupont"));
        }).GeneratePdf();

        var import = fixture.Services.GetRequiredService<ICvImportService>();
        var candidate = fixture.Services.GetRequiredService<ICandidateProfileService>();
        var userId = $"cv-import-{Guid.NewGuid()}";
        var profileId = await candidate.GetOrCreateProfileIdAsync(userId);

        await using var stream = new MemoryStream(pdf);
        var result = await import.ImporterAsync(stream, "cv.pdf", "application/pdf", pdf.Length);

        Assert.True(result.Success);
        Assert.Equal("Cv_Import_PretARelire", result.MessageKey);
        Assert.Equal("Dupont", result.Brouillon!.Coordonnees!.Nom);
        Assert.Equal(1, fakeIa.Appels);

        var enBase = await candidate.GetCvAsync(profileId);
        Assert.Null(enBase.Coordonnees);
    }

    [Fact]
    public void ParseCv_JsonValide_RemplitLesCoordonnees()
    {
        var json = """
            { "coordonnees": { "nom": "Leroy", "prenoms": "Inès", "telephone": "0470000001" } }
            """;

        var view = CvImportIaService.ParseCv(json);

        Assert.NotNull(view);
        Assert.Equal("Leroy", view!.Coordonnees!.Nom);
        Assert.Equal("Inès", view.Coordonnees.Prenoms);
        Assert.Equal("0470000001", view.Coordonnees.Telephone);
    }

    [Fact]
    public void ParseCv_JsonMalForme_RetourneNull()
    {
        Assert.Null(CvImportIaService.ParseCv("ceci n'est pas du json"));
    }

    [Fact]
    public async Task ExtraireCvAsync_FakeReplicateJson_SansReseau()
    {
        var replicate = new FakeReplicateService
        {
            Reponse = """{"coordonnees":{"nom":"Bernard","prenoms":"Nina"}}""",
        };
        var ia = new CvImportIaService(replicate);

        var view = await ia.ExtraireCvAsync("Nina Bernard, Lyon");

        Assert.NotNull(view);
        Assert.Equal("Bernard", view!.Coordonnees!.Nom);
        Assert.Equal("Nina", view.Coordonnees.Prenoms);
    }

    private static MemoryStream CreerDocx(string texte)
    {
        var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true))
        {
            var main = doc.AddMainDocumentPart();
            main.Document = new WordDocument(new Body(new Paragraph(new Run(new Text(texte)))));
            main.Document.Save();
        }

        ms.Position = 0;
        return ms;
    }
}
