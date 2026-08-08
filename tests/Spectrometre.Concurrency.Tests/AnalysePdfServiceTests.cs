using Spectrometre.Modules.PostesRecrutement.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Smoke test de <see cref="IAnalysePdfService"/> (QuestPDF) — pas d'accès réseau ni DB ;
/// vérifie qu'un PDF non vide est produit (même idée que la validation de <c>CvPdfService</c>).
/// </summary>
public sealed class AnalysePdfServiceTests
{
    [Fact]
    public void GenerateAnalysePdf_ProduitUnPdfNonVide()
    {
        IAnalysePdfService service = new AnalysePdfService();
        var bytes = service.GenerateAnalysePdf(new AnalysePdfModel(
            TitrePoste: "Développeur .NET",
            CandidateProfileId: 42,
            NomCandidat: "Alex Martin",
            ScoreCompatibilite: 78,
            AnalyseTexte: "Points forts : expérience Blazor.\nÉcarts : leadership à confirmer.",
            GenereeLe: DateTimeOffset.UtcNow,
            GenereeParIa: false,
            English: false));

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 100);
        // Signature PDF
        Assert.Equal(0x25, bytes[0]); // %
        Assert.Equal(0x50, bytes[1]); // P
        Assert.Equal(0x44, bytes[2]); // D
        Assert.Equal(0x46, bytes[3]); // F
    }

    [Fact]
    public void GenerateAnalysePdf_Anglais_ProduitUnPdfNonVide()
    {
        IAnalysePdfService service = new AnalysePdfService();
        var bytes = service.GenerateAnalysePdf(new AnalysePdfModel(
            TitrePoste: "Product Manager",
            CandidateProfileId: 7,
            NomCandidat: null,
            ScoreCompatibilite: null,
            AnalyseTexte: "Local fallback summary.",
            GenereeLe: DateTimeOffset.UtcNow,
            GenereeParIa: true,
            English: true));

        Assert.True(bytes.Length > 100);
        Assert.Equal("%PDF"u8.ToArray(), bytes.AsSpan(0, 4).ToArray());
    }
}
