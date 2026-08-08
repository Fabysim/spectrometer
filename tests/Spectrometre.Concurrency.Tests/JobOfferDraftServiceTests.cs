using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.ProfilEntreprise.Data;
using Spectrometre.Modules.ProfilEntreprise.Entities;
using Spectrometre.Modules.ProfilEntreprise.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Smoke test de la génération .docx (OpenXml) — pas d'accès réseau ni DB ;
/// vérifie qu'un document non vide avec signature ZIP « PK » est produit
/// (même idée que <see cref="AnalysePdfServiceTests"/> pour %PDF).
/// + persistance OffreTexte (IA / repli) via <see cref="IJobOfferDraftService.GenererEtEnregistrerOffreAsync"/>.
/// </summary>
[Collection("Base de données partagée")]
public sealed class JobOfferDraftServiceTests(ServiceFixture fixture)
{
    private IServiceScope NewScope() => fixture.Services.CreateScope();

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

    [Fact]
    public void Parse_DetecteTitresEtPuces()
    {
        var blocks = JobOfferTextParser.Parse("""
            TITRE POSTE

            PRÉSENTATION
            Un paragraphe.

            MISSIONS
            - Faire A
            • Faire B
            """, skipFirstHeading: true);

        Assert.Contains(blocks, b => b.Kind == JobOfferBlockKind.Heading && b.Text == "PRÉSENTATION");
        Assert.Contains(blocks, b => b.Kind == JobOfferBlockKind.Paragraph && b.Text.Contains("paragraphe"));
        Assert.Contains(blocks, b => b.Kind == JobOfferBlockKind.Bullet && b.Text == "Faire A");
        Assert.Contains(blocks, b => b.Kind == JobOfferBlockKind.Bullet && b.Text == "Faire B");
    }

    [Fact]
    public async Task GenererEtEnregistrer_AvecIa_PersisteOffreTexte()
    {
        var suffix = Guid.NewGuid();
        var company = await fixture.CreateCompanyAsync($"Offre IA {suffix}", $"owner-offre-ia-{suffix}");

        int posteId;
        using (var setup = NewScope())
        {
            setup.ServiceProvider.GetRequiredService<ITenantContext>()
                .SetActiveCompany(company.Id, company.SchemaName);
            var postes = setup.ServiceProvider.GetRequiredService<IPosteService>();
            posteId = await postes.CreatePosteAsync(
                $"Dev .NET {suffix}",
                "Description du poste",
                "IT",
                tachesDescription: "- Coder\n- Tester",
                salaire: "60k",
                avantages: "TT");
            await postes.UpsertCritereAsync(posteId, null, "Technique", "C#", (int)NiveauEvaluation.Fort, 0);
        }

        using var scope = NewScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>()
            .SetActiveCompany(company.Id, company.SchemaName);
        var fake = (FakeReplicateService)scope.ServiceProvider
            .GetRequiredService<Spectrometre.Core.Ai.IReplicateService>();
        fake.Erreur = null;
        fake.Reponse = """
            DEV .NET

            PRÉSENTATION
            Offre générée par l'IA de test.
            """;

        var draft = scope.ServiceProvider.GetRequiredService<IJobOfferDraftService>();
        var (texte, erreur) = await draft.GenererEtEnregistrerOffreAsync(posteId);
        Assert.Null(erreur);
        Assert.Contains("Offre générée par l'IA", texte);

        await using var db = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<ProfilEntrepriseDbContext>>()
            .CreateDbContextAsync();
        db.TenantSchema = company.SchemaName;
        var entity = await db.Postes.AsNoTracking().SingleAsync(p => p.Id == posteId);
        Assert.Equal(texte, entity.OffreTexte);
        Assert.True(entity.OffreGenereeParIa);
        Assert.NotNull(entity.OffreGenereeLe);
    }

    [Fact]
    public async Task GenererEtEnregistrer_EchecIa_PersisteRepliLocal()
    {
        var suffix = Guid.NewGuid();
        var company = await fixture.CreateCompanyAsync($"Offre repli {suffix}", $"owner-offre-repli-{suffix}");

        int posteId;
        using (var setup = NewScope())
        {
            setup.ServiceProvider.GetRequiredService<ITenantContext>()
                .SetActiveCompany(company.Id, company.SchemaName);
            var postes = setup.ServiceProvider.GetRequiredService<IPosteService>();
            posteId = await postes.CreatePosteAsync(
                $"Analyste {suffix}",
                "Desc analytique",
                null,
                tachesDescription: "Analyser les données",
                salaire: "55k");
        }

        using var scope = NewScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>()
            .SetActiveCompany(company.Id, company.SchemaName);
        var fake = (FakeReplicateService)scope.ServiceProvider
            .GetRequiredService<Spectrometre.Core.Ai.IReplicateService>();
        fake.Reponse = null;
        fake.Erreur = "Simulateur IA indisponible.";

        var draft = scope.ServiceProvider.GetRequiredService<IJobOfferDraftService>();
        var (texte, erreur) = await draft.GenererEtEnregistrerOffreAsync(posteId);
        Assert.Null(erreur);
        Assert.False(string.IsNullOrWhiteSpace(texte));
        Assert.Contains("Analyste", texte, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Desc analytique", texte);

        await using var db = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<ProfilEntrepriseDbContext>>()
            .CreateDbContextAsync();
        db.TenantSchema = company.SchemaName;
        var entity = await db.Postes.AsNoTracking().SingleAsync(p => p.Id == posteId);
        Assert.Equal(texte, entity.OffreTexte);
        Assert.False(entity.OffreGenereeParIa);
        Assert.NotNull(entity.OffreGenereeLe);
    }

    [Fact]
    public async Task GetPosteOuvertDetail_FermeOuAbsent_RetourneNullUniforme()
    {
        var suffix = Guid.NewGuid();
        var company = await fixture.CreateCompanyAsync($"Offre detail {suffix}", $"owner-offre-detail-{suffix}");
        var candidatUserId = $"cand-offre-detail-{suffix}";

        int posteOuvertId;
        int posteFermeId;
        int candidateProfileId;
        using (var setup = NewScope())
        {
            candidateProfileId = await setup.ServiceProvider
                .GetRequiredService<Spectrometre.Modules.ProfilCandidat.Services.ICandidateProfileService>()
                .GetOrCreateProfileIdAsync(candidatUserId);

            setup.ServiceProvider.GetRequiredService<ITenantContext>()
                .SetActiveCompany(company.Id, company.SchemaName);
            var postes = setup.ServiceProvider.GetRequiredService<IPosteService>();
            posteOuvertId = await postes.CreatePosteAsync($"Ouvert {suffix}", "D", null);
            posteFermeId = await postes.CreatePosteAsync($"Fermé {suffix}", "D", null);
            await postes.SetPosteStatutAsync(posteFermeId, PosteStatut.Ferme);

            await using var db = await setup.ServiceProvider
                .GetRequiredService<IDbContextFactory<ProfilEntrepriseDbContext>>()
                .CreateDbContextAsync();
            db.TenantSchema = company.SchemaName;
            var ouvert = await db.Postes.SingleAsync(p => p.Id == posteOuvertId);
            ouvert.OffreTexte = "TEXTE OFFRE\n\nPRÉSENTATION\nContenu.";
            ouvert.OffreGenereeLe = DateTimeOffset.UtcNow;
            ouvert.OffreGenereeParIa = true;
            await db.SaveChangesAsync();
        }

        using var scope = NewScope();
        var service = scope.ServiceProvider.GetRequiredService<IPosteService>();

        var detail = await service.GetPosteOuvertDetailAsync(company.Id, posteOuvertId, candidateProfileId);
        Assert.NotNull(detail);
        Assert.Equal("TEXTE OFFRE\n\nPRÉSENTATION\nContenu.", detail!.OffreTexte);
        Assert.True(detail.OffreGenereeParIa);

        Assert.Null(await service.GetPosteOuvertDetailAsync(company.Id, posteFermeId, candidateProfileId));
        Assert.Null(await service.GetPosteOuvertDetailAsync(company.Id, 999_999, candidateProfileId));
        Assert.Null(await service.GetPosteOuvertDetailAsync(0, posteOuvertId, candidateProfileId));
    }
}
