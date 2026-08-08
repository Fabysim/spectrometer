using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.PostesRecrutement.Entities;
using Spectrometre.Modules.PostesRecrutement.Services;
using Spectrometre.Modules.ProfilCandidat.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Contrôle d'accès service-level pour les candidatures d'un poste : <see cref="IPosteService"/>
/// lit toujours le schéma du tenant ambiant (<see cref="ITenantContext"/>). Un propriétaire voit
/// ses candidatures ; un manager d'une AUTRE entreprise (ou sans tenant) ne doit jamais récupérer
/// ni modifier les données d'un poste hors de son schéma — même avec des identifiants connus
/// (critères, évaluation, guide 2ème entrevue, présélection, analyse IA).
/// </summary>
[Collection("Base de données partagée")]
public sealed class PostesRecrutementAccessControlTests(ServiceFixture fixture)
{
    private IServiceScope NewScope() => fixture.Services.CreateScope();

    [Fact]
    public async Task LeProprietaireVoitLesCandidaturesEtLeDetail()
    {
        var suffix = Guid.NewGuid();
        var ownerUserId = $"postes-access-owner-{suffix}";
        var candidatUserId = $"postes-access-candidat-{suffix}";

        var company = await fixture.CreateCompanyAsync($"Entreprise Postes Access {suffix}", ownerUserId);

        int posteId;
        int candidatureId;
        using (var setupScope = NewScope())
        {
            var candidateProfileId = await setupScope.ServiceProvider
                .GetRequiredService<ICandidateProfileService>()
                .GetOrCreateProfileIdAsync(candidatUserId);

            var tenantContext = setupScope.ServiceProvider.GetRequiredService<ITenantContext>();
            tenantContext.SetActiveCompany(company.Id, company.SchemaName);
            var posteService = setupScope.ServiceProvider.GetRequiredService<IPosteService>();

            posteId = await posteService.CreatePosteAsync($"Poste access {suffix}", null, null);
            await posteService.PostulerAsync(company.Id, posteId, candidateProfileId);

            var liste = await posteService.GetCandidaturesAsync(posteId);
            candidatureId = Assert.Single(liste).Id;
        }

        using var scope = NewScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>()
            .SetActiveCompany(company.Id, company.SchemaName);
        var service = scope.ServiceProvider.GetRequiredService<IPosteService>();

        var candidatures = await service.GetCandidaturesAsync(posteId);
        var candidature = Assert.Single(candidatures);
        Assert.Equal(candidatureId, candidature.Id);
        Assert.Equal(posteId, candidature.PosteId);

        var detail = await service.GetCandidatureAsync(candidatureId);
        Assert.NotNull(detail);
        Assert.Equal(candidatureId, detail!.Id);
        Assert.Equal(posteId, detail.PosteId);
    }

    [Fact]
    public async Task UneAutreEntrepriseNeVoitPasLesCandidaturesDuPoste()
    {
        var suffix = Guid.NewGuid();
        var ownerA = $"postes-access-owner-a-{suffix}";
        var ownerB = $"postes-access-owner-b-{suffix}";
        var candidatUserId = $"postes-access-candidat-{suffix}";

        var companyA = await fixture.CreateCompanyAsync($"Entreprise Postes A {suffix}", ownerA);
        var companyB = await fixture.CreateCompanyAsync($"Entreprise Postes B {suffix}", ownerB);

        int posteIdA;
        int candidatureIdA;
        using (var setupScope = NewScope())
        {
            var candidateProfileId = await setupScope.ServiceProvider
                .GetRequiredService<ICandidateProfileService>()
                .GetOrCreateProfileIdAsync(candidatUserId);

            var tenantContext = setupScope.ServiceProvider.GetRequiredService<ITenantContext>();
            tenantContext.SetActiveCompany(companyA.Id, companyA.SchemaName);
            var posteService = setupScope.ServiceProvider.GetRequiredService<IPosteService>();

            posteIdA = await posteService.CreatePosteAsync($"Poste A {suffix}", null, null);
            await posteService.PostulerAsync(companyA.Id, posteIdA, candidateProfileId);

            candidatureIdA = Assert.Single(await posteService.GetCandidaturesAsync(posteIdA)).Id;
        }

        // Tenant B actif : mêmes ids numériques éventuels, mais schéma différent — aucune fuite.
        using var scopeB = NewScope();
        scopeB.ServiceProvider.GetRequiredService<ITenantContext>()
            .SetActiveCompany(companyB.Id, companyB.SchemaName);
        var serviceB = scopeB.ServiceProvider.GetRequiredService<IPosteService>();

        var candidaturesB = await serviceB.GetCandidaturesAsync(posteIdA);
        Assert.Empty(candidaturesB);

        var detailB = await serviceB.GetCandidatureAsync(candidatureIdA);
        Assert.Null(detailB);

        Assert.Null(await serviceB.GetAnalyseIaAsync(candidatureIdA));
    }

    [Fact]
    public async Task UneAutreEntrepriseNeLitNiModifieCriteresEvaluationGuidePreselectionAnalyse()
    {
        var suffix = Guid.NewGuid();
        var ownerA = $"postes-iso-owner-a-{suffix}";
        var ownerB = $"postes-iso-owner-b-{suffix}";
        var candidatUserId = $"postes-iso-candidat-{suffix}";

        var companyA = await fixture.CreateCompanyAsync($"Entreprise Iso A {suffix}", ownerA);
        var companyB = await fixture.CreateCompanyAsync($"Entreprise Iso B {suffix}", ownerB);

        int posteIdA;
        int candidatureIdA;
        int critereIdA;
        string analyseTexteA;

        // --- Setup A : critères, évaluation, guide, présélection, analyse IA ---
        using (var setupScope = NewScope())
        {
            var candidateProfileId = await setupScope.ServiceProvider
                .GetRequiredService<ICandidateProfileService>()
                .GetOrCreateProfileIdAsync(candidatUserId);

            setupScope.ServiceProvider.GetRequiredService<ITenantContext>()
                .SetActiveCompany(companyA.Id, companyA.SchemaName);
            var serviceA = setupScope.ServiceProvider.GetRequiredService<IPosteService>();

            posteIdA = await serviceA.CreatePosteAsync($"Poste iso {suffix}", "Desc A", "IT");
            await serviceA.PostulerAsync(companyA.Id, posteIdA, candidateProfileId);
            candidatureIdA = Assert.Single(await serviceA.GetCandidaturesAsync(posteIdA)).Id;

            await serviceA.UpsertCritereAsync(posteIdA, null, "Technique", "C#", (int)NiveauEvaluation.Fort, 1);
            critereIdA = Assert.Single(await serviceA.GetCriteresAsync(posteIdA)).Id;

            await serviceA.SetNiveauFinalAsync(candidatureIdA, critereIdA, (int)NiveauEvaluation.Moyen);
            await serviceA.SetPreselectionAsync(candidatureIdA, true);
            await serviceA.SaveGuideDeuxiemeEntrevueAsync(posteIdA, new GuideDeuxiemeEntrevue
            {
                PosteId = posteIdA,
                MissionLivrables = "Mission secrète A",
                Objectifs = "Objectifs A",
            });

            analyseTexteA = (await serviceA.GenererAnalyseIaAsync(candidatureIdA)).AnalyseTexte;
            Assert.False(string.IsNullOrWhiteSpace(analyseTexteA));
        }

        // --- Attaques depuis B (mêmes ids numériques, autre schéma) ---
        using (var scopeB = NewScope())
        {
            scopeB.ServiceProvider.GetRequiredService<ITenantContext>()
                .SetActiveCompany(companyB.Id, companyB.SchemaName);
            var serviceB = scopeB.ServiceProvider.GetRequiredService<IPosteService>();

            // Critères
            Assert.Empty(await serviceB.GetCriteresAsync(posteIdA));
            await serviceB.UpsertCritereAsync(posteIdA, null, "Hack", "Injection", (int)NiveauEvaluation.TresFort, 99);
            await serviceB.UpsertCritereAsync(posteIdA, critereIdA, "Hack", "Overwrite", (int)NiveauEvaluation.PasDuTout, 0);
            await serviceB.DeleteCritereAsync(critereIdA);
            Assert.Empty(await serviceB.GetCriteresAsync(posteIdA));

            // Évaluation finale
            Assert.Empty(await serviceB.GetEvaluationCriteresAsync(candidatureIdA));
            await serviceB.SetNiveauFinalAsync(candidatureIdA, critereIdA, (int)NiveauEvaluation.TresFort);

            // Présélection
            await serviceB.SetPreselectionAsync(candidatureIdA, false);

            // Guide 2ème entrevue
            Assert.Null(await serviceB.GetGuideDeuxiemeEntrevueAsync(posteIdA));
            await serviceB.SaveGuideDeuxiemeEntrevueAsync(posteIdA, new GuideDeuxiemeEntrevue
            {
                PosteId = posteIdA,
                MissionLivrables = "Piraté par B",
            });

            // Analyse IA
            Assert.Null(await serviceB.GetAnalyseIaAsync(candidatureIdA));
            var analyseB = await serviceB.GenererAnalyseIaAsync(candidatureIdA, forcerRegeneration: true);
            Assert.False(analyseB.GenereeParIa);
            // Repli « candidature introuvable » — jamais de persistance dans le schéma A.
            Assert.Contains("introuvable", analyseB.AnalyseTexte, StringComparison.OrdinalIgnoreCase);
            Assert.Null(await serviceB.GetAnalyseIaAsync(candidatureIdA));
        }

        // --- Vérification : données de A intactes ---
        using var scopeA = NewScope();
        scopeA.ServiceProvider.GetRequiredService<ITenantContext>()
            .SetActiveCompany(companyA.Id, companyA.SchemaName);
        var verify = scopeA.ServiceProvider.GetRequiredService<IPosteService>();

        var criteres = await verify.GetCriteresAsync(posteIdA);
        var critere = Assert.Single(criteres);
        Assert.Equal(critereIdA, critere.Id);
        Assert.Equal("Technique", critere.Categorie);
        Assert.Equal("C#", critere.Libelle);
        Assert.Equal(NiveauEvaluation.Fort, critere.NiveauRequis);

        var evals = await verify.GetEvaluationCriteresAsync(candidatureIdA);
        var eval = Assert.Single(evals);
        Assert.Equal(NiveauEvaluation.Moyen, eval.NiveauFinal);

        var candidature = await verify.GetCandidatureAsync(candidatureIdA);
        Assert.NotNull(candidature);
        Assert.True(candidature!.EstPreselectionne);

        var guide = await verify.GetGuideDeuxiemeEntrevueAsync(posteIdA);
        Assert.NotNull(guide);
        Assert.Equal("Mission secrète A", guide!.MissionLivrables);
        Assert.Equal("Objectifs A", guide.Objectifs);

        var analyse = await verify.GetAnalyseIaAsync(candidatureIdA);
        Assert.NotNull(analyse);
        Assert.Equal(analyseTexteA, analyse!.AnalyseTexte);
    }

    [Fact]
    public async Task LeProprietairePeutUtiliserCriteresEvaluationGuidePreselection()
    {
        var suffix = Guid.NewGuid();
        var ownerUserId = $"postes-owner-full-{suffix}";
        var candidatUserId = $"postes-candidat-full-{suffix}";

        var company = await fixture.CreateCompanyAsync($"Entreprise Full {suffix}", ownerUserId);

        using var scope = NewScope();
        var candidateProfileId = await scope.ServiceProvider
            .GetRequiredService<ICandidateProfileService>()
            .GetOrCreateProfileIdAsync(candidatUserId);

        scope.ServiceProvider.GetRequiredService<ITenantContext>()
            .SetActiveCompany(company.Id, company.SchemaName);
        var service = scope.ServiceProvider.GetRequiredService<IPosteService>();

        var posteId = await service.CreatePosteAsync($"Poste full {suffix}", null, null);
        await service.PostulerAsync(company.Id, posteId, candidateProfileId);
        var candidatureId = Assert.Single(await service.GetCandidaturesAsync(posteId)).Id;

        await service.UpsertCritereAsync(posteId, null, "Soft", "Communication", (int)NiveauEvaluation.Moyen, 2);
        var critere = Assert.Single(await service.GetCriteresAsync(posteId));
        Assert.Equal("Soft", critere.Categorie);

        await service.UpsertCritereAsync(posteId, critere.Id, "Soft", "Communication orale", (int)NiveauEvaluation.Fort, 2);
        critere = Assert.Single(await service.GetCriteresAsync(posteId));
        Assert.Equal("Communication orale", critere.Libelle);
        Assert.Equal(NiveauEvaluation.Fort, critere.NiveauRequis);

        await service.SetNiveauFinalAsync(candidatureId, critere.Id, (int)NiveauEvaluation.TresFort);
        var eval = Assert.Single(await service.GetEvaluationCriteresAsync(candidatureId));
        Assert.Equal(NiveauEvaluation.TresFort, eval.NiveauFinal);

        await service.SetPreselectionAsync(candidatureId, true);
        Assert.True((await service.GetCandidatureAsync(candidatureId))!.EstPreselectionne);
        await service.SetPreselectionAsync(candidatureId, false);
        Assert.False((await service.GetCandidatureAsync(candidatureId))!.EstPreselectionne);

        var guideVide = await service.GetGuideDeuxiemeEntrevueAsync(posteId);
        Assert.NotNull(guideVide);
        Assert.Equal(0, guideVide!.Id);
        Assert.Equal(posteId, guideVide.PosteId);

        await service.SaveGuideDeuxiemeEntrevueAsync(posteId, new GuideDeuxiemeEntrevue
        {
            PosteId = posteId,
            Suivi = "Suivi Q2",
            Echeances = "30 jours",
        });
        var guide = await service.GetGuideDeuxiemeEntrevueAsync(posteId);
        Assert.NotNull(guide);
        Assert.True(guide!.Id > 0);
        Assert.Equal("Suivi Q2", guide.Suivi);
        Assert.Equal("30 jours", guide.Echeances);

        await service.DeleteCritereAsync(critere.Id);
        Assert.Empty(await service.GetCriteresAsync(posteId));
    }

    [Fact]
    public async Task LeProprietairePeutGenererUneAnalyseIa_ExportPdfPossible()
    {
        var suffix = Guid.NewGuid();
        var ownerUserId = $"postes-analyse-owner-{suffix}";
        var candidatUserId = $"postes-analyse-candidat-{suffix}";

        var company = await fixture.CreateCompanyAsync($"Entreprise Analyse IA {suffix}", ownerUserId);

        int posteId;
        int candidatureId;
        using (var setupScope = NewScope())
        {
            var candidateProfileId = await setupScope.ServiceProvider
                .GetRequiredService<ICandidateProfileService>()
                .GetOrCreateProfileIdAsync(candidatUserId);

            setupScope.ServiceProvider.GetRequiredService<ITenantContext>()
                .SetActiveCompany(company.Id, company.SchemaName);
            var posteService = setupScope.ServiceProvider.GetRequiredService<IPosteService>();

            posteId = await posteService.CreatePosteAsync($"Poste analyse {suffix}", "Desc", null);
            await posteService.PostulerAsync(company.Id, posteId, candidateProfileId);
            candidatureId = Assert.Single(await posteService.GetCandidaturesAsync(posteId)).Id;
        }

        using var scope = NewScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>()
            .SetActiveCompany(company.Id, company.SchemaName);
        var service = scope.ServiceProvider.GetRequiredService<IPosteService>();
        var pdfService = scope.ServiceProvider.GetRequiredService<IAnalysePdfService>();

        Assert.Null(await service.GetAnalyseIaAsync(candidatureId));

        var analyse = await service.GenererAnalyseIaAsync(candidatureId, forcerRegeneration: false);
        Assert.False(string.IsNullOrWhiteSpace(analyse.AnalyseTexte));
        // FakeAnalysePosteIaService force le repli local en test.
        Assert.False(analyse.GenereeParIa);

        var cached = await service.GetAnalyseIaAsync(candidatureId);
        Assert.NotNull(cached);
        Assert.Equal(analyse.AnalyseTexte, cached!.AnalyseTexte);

        var poste = Assert.Single(await service.GetPostesAsync(), p => p.Id == posteId);
        var candidature = await service.GetCandidatureAsync(candidatureId);
        Assert.NotNull(candidature);

        var pdf = pdfService.GenerateAnalysePdf(new AnalysePdfModel(
            TitrePoste: poste.Titre,
            CandidateProfileId: candidature!.CandidateProfileId,
            NomCandidat: null,
            ScoreCompatibilite: candidature.ScoreCompatibilite,
            AnalyseTexte: analyse.AnalyseTexte,
            GenereeLe: analyse.GenereeLe,
            GenereeParIa: analyse.GenereeParIa));
        Assert.True(pdf.Length > 100);
        Assert.Equal("%PDF"u8.ToArray(), pdf.AsSpan(0, 4).ToArray());
    }

    [Fact]
    public async Task SupprimerPosteRetireLePosteEtSesCandidatures_SansToucherUneAutreEntreprise()
    {
        var suffix = Guid.NewGuid();
        var ownerA = $"postes-del-owner-a-{suffix}";
        var ownerB = $"postes-del-owner-b-{suffix}";
        var candidatUserId = $"postes-del-candidat-{suffix}";

        var companyA = await fixture.CreateCompanyAsync($"Entreprise Del A {suffix}", ownerA);
        var companyB = await fixture.CreateCompanyAsync($"Entreprise Del B {suffix}", ownerB);

        int posteIdA;
        int posteIdB;
        int candidatureIdA;

        using (var setupScope = NewScope())
        {
            var candidateProfileId = await setupScope.ServiceProvider
                .GetRequiredService<ICandidateProfileService>()
                .GetOrCreateProfileIdAsync(candidatUserId);

            var tenantContext = setupScope.ServiceProvider.GetRequiredService<ITenantContext>();
            var posteService = setupScope.ServiceProvider.GetRequiredService<IPosteService>();

            // Deux postes chez A pour décaler les ids numériques vs B (ids locaux par schéma).
            tenantContext.SetActiveCompany(companyA.Id, companyA.SchemaName);
            _ = await posteService.CreatePosteAsync($"Poste del A padding {suffix}", null, null);
            posteIdA = await posteService.CreatePosteAsync($"Poste del A {suffix}", null, null);
            await posteService.PostulerAsync(companyA.Id, posteIdA, candidateProfileId);
            candidatureIdA = Assert.Single(await posteService.GetCandidaturesAsync(posteIdA)).Id;

            tenantContext.SetActiveCompany(companyB.Id, companyB.SchemaName);
            posteIdB = await posteService.CreatePosteAsync($"Poste del B {suffix}", null, null);
            Assert.NotEqual(posteIdA, posteIdB);
        }

        // B appelle Delete avec l'id de A : no-op (id inexistant dans le schéma B).
        using (var scopeB = NewScope())
        {
            scopeB.ServiceProvider.GetRequiredService<ITenantContext>()
                .SetActiveCompany(companyB.Id, companyB.SchemaName);
            var serviceB = scopeB.ServiceProvider.GetRequiredService<IPosteService>();
            await serviceB.DeletePosteAsync(posteIdA);
            Assert.Contains(await serviceB.GetPostesAsync(), p => p.Id == posteIdB);
        }

        using (var scopeA = NewScope())
        {
            scopeA.ServiceProvider.GetRequiredService<ITenantContext>()
                .SetActiveCompany(companyA.Id, companyA.SchemaName);
            var serviceA = scopeA.ServiceProvider.GetRequiredService<IPosteService>();

            Assert.Contains(await serviceA.GetPostesAsync(), p => p.Id == posteIdA);
            Assert.NotNull(await serviceA.GetCandidatureAsync(candidatureIdA));

            await serviceA.DeletePosteAsync(posteIdA);

            Assert.DoesNotContain(await serviceA.GetPostesAsync(), p => p.Id == posteIdA);
            Assert.Empty(await serviceA.GetCandidaturesAsync(posteIdA));
            Assert.Null(await serviceA.GetCandidatureAsync(candidatureIdA));
        }

        using var scopeBCheck = NewScope();
        scopeBCheck.ServiceProvider.GetRequiredService<ITenantContext>()
            .SetActiveCompany(companyB.Id, companyB.SchemaName);
        Assert.Contains(
            await scopeBCheck.ServiceProvider.GetRequiredService<IPosteService>().GetPostesAsync(),
            p => p.Id == posteIdB);
    }
}
