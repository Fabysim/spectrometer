using Spectrometre.Modules.ProfilEntreprise.Services;
using Spectrometre.Modules.Recrutement.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Smoke test de la génération .docx (OpenXml) — pas d'accès réseau ni DB ;
/// vérifie qu'un document non vide avec signature ZIP « PK » est produit
/// (même idée que <see cref="AnalysePdfServiceTests"/> pour %PDF).
/// </summary>
public sealed class JobOfferDraftServiceTests
{
    [Fact]
    public void BuildDocx_ProduitUnDocxValide_SignatureZip()
    {
        var body = """
            DÉVELOPPEUR .NET

            PRÉSENTATION
            Nous recherchons un développeur expérimenté.

            MISSIONS
            - Concevoir des applications Blazor
            - Collaborer avec l'équipe produit
            * Participer aux revues de code

            PROFIL RECHERCHÉ
            • Communication : niveau Moyen
            • C# : niveau Fort
            """;

        var bytes = JobOfferDraftService.BuildDocx("Développeur .NET", body, english: false);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 100);
        // Signature ZIP (docx = package Open XML)
        Assert.Equal((byte)'P', bytes[0]);
        Assert.Equal((byte)'K', bytes[1]);
    }

    [Fact]
    public void BuildDocx_Anglais_ProduitUnDocxValide()
    {
        var bytes = JobOfferDraftService.BuildDocx(
            "Product Manager",
            "JOB OVERVIEW\n\nWe are hiring.\n\nRESPONSIBILITIES\n- Lead the roadmap\n",
            english: true);

        Assert.True(bytes.Length > 100);
        Assert.Equal("PK"u8.ToArray(), bytes.AsSpan(0, 2).ToArray());
    }
}
