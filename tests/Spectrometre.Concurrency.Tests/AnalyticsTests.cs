using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Compatibility;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Analytics.Services;
using Spectrometre.Modules.Compatibilite.Entities;
using Spectrometre.Modules.PostesRecrutement.Entities;
using Spectrometre.Modules.PostesRecrutement.Services;
using Spectrometre.Modules.ProfilCandidat.Services;
using Spectrometre.Modules.ProfilEntreprise.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Vérifie le tableau de bord Analytics : les métriques sont correctement agrégées depuis l'index partagé
/// de recrutement (voir <see cref="AnalyticsService"/>) et strictement scopées à l'entreprise active — la
/// même exigence, avec le même esprit de test, que les contrôles d'accès des cycles précédents
/// (<c>CompatibiliteAccessControlTests</c>, <c>EntretienAccessControlTests</c>).
/// </summary>
[Collection("Base de données partagée")]
public sealed class AnalyticsTests(ServiceFixture fixture)
{
    /// <summary>
    /// Fait avancer une candidature jusqu'au calcul de score : rejoue le même chemin que
    /// <c>PostesEntreprisePage</c> (lister les candidatures d'un poste déclenche le (re)calcul et la
    /// synchronisation vers l'index partagé — voir <c>PosteService.GetCandidaturesAsync</c>).
    /// </summary>
    private static Task<IReadOnlyList<CandidatureView>> MaterialiserScoresAsync(IPosteService posteService, int posteId) =>
        posteService.GetCandidaturesAsync(posteId);

    [Fact]
    public async Task GetDashboardAsync_AgregeLesMetriquesDeLEntrepriseActive()
    {
        using var scope = fixture.Services.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var companyService = scope.ServiceProvider.GetRequiredService<ICompanyProfileService>();
        var candidateService = scope.ServiceProvider.GetRequiredService<ICandidateProfileService>();
        var posteService = scope.ServiceProvider.GetRequiredService<IPosteService>();
        var analyticsService = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();

        var suffix = Guid.NewGuid();
        var company = await fixture.CreateCompanyAsync($"Entreprise Analytics {suffix}", $"analytics-test-manager-{suffix}");
        tenantContext.SetActiveCompany(company.Id, company.SchemaName);

        var companyProfileId = await companyService.GetOrCreateProfileIdAsync();
        var vigilanceTagPartage = CompatibilityVocabulary.PointsVigilanceTags[0];
        await companyService.ToggleTagAsync(companyProfileId, CriteriaField.Technique, CompatibilityVocabulary.TechniqueTags[0], true);
        await companyService.ToggleTagAsync(companyProfileId, CriteriaField.PointsVigilance, vigilanceTagPartage, true);

        // Candidat 1 : grille H entièrement remplie (tous les axes à tags + rythme + vigilance) ET partage
        // le même point de vigilance que l'entreprise — doit compter dans le taux de complétion ET faire
        // remonter ce tag dans le top des points de vigilance.
        var candidat1Id = await candidateService.GetOrCreateProfileIdAsync($"analytics-candidat-complet-{suffix}");
        await candidateService.ToggleTagAsync(candidat1Id, CriteriaField.Technique, CompatibilityVocabulary.TechniqueTags[0], true);
        await candidateService.ToggleTagAsync(candidat1Id, CriteriaField.Comportementale, CompatibilityVocabulary.ComportementaleTags[0], true);
        await candidateService.ToggleTagAsync(candidat1Id, CriteriaField.Culturelle, CompatibilityVocabulary.CulturelleTags[0], true);
        await candidateService.ToggleTagAsync(candidat1Id, CriteriaField.Motivationnelle, CompatibilityVocabulary.MotivationnelleTags[0], true);
        await candidateService.ToggleTagAsync(candidat1Id, CriteriaField.PointsVigilance, vigilanceTagPartage, true);
        await candidateService.SetRythmeTravailAsync(candidat1Id, 3);

        // Candidat 2 : grille H volontairement incomplète (un seul axe renseigné) — ne doit PAS compter
        // dans le taux de complétion, mais reste une candidature normale pour le reste des métriques.
        var candidat2Id = await candidateService.GetOrCreateProfileIdAsync($"analytics-candidat-incomplet-{suffix}");
        await candidateService.ToggleTagAsync(candidat2Id, CriteriaField.Technique, CompatibilityVocabulary.TechniqueTags[0], true);

        var posteId = await posteService.CreatePosteAsync("Développeur", "Description", "Tech");
        await posteService.PostulerAsync(company.Id, posteId, candidat1Id);
        await posteService.PostulerAsync(company.Id, posteId, candidat2Id);

        var candidatures = await MaterialiserScoresAsync(posteService, posteId);
        var candidature2 = candidatures.Single(c => c.CandidateProfileId == candidat2Id);
        await posteService.SetCandidatureStatutAsync(candidature2.Id, CandidatureStatut.EnRevue);

        var dashboard = await analyticsService.GetDashboardAsync();

        Assert.Equal(1, dashboard.PostesOuverts);
        Assert.Equal(0, dashboard.PostesFermes);
        Assert.Equal(2, dashboard.TotalCandidatures);

        Assert.Equal(5, dashboard.Funnel.Count);
        Assert.Equal(1, dashboard.Funnel.Single(f => f.Statut == "Recue").Nombre);
        Assert.Equal(1, dashboard.Funnel.Single(f => f.Statut == "EnRevue").Nombre);
        Assert.Equal(0, dashboard.Funnel.Single(f => f.Statut == "Entretien").Nombre);

        Assert.NotNull(dashboard.ScoreMoyenGlobal);
        var axeTechnique = dashboard.MoyennesParAxe.Single(a => a.Axe == "Technique");
        Assert.NotNull(axeTechnique.Moyenne);
        // Chevauchement complet sur le tag technique partagé par les deux candidats et l'entreprise → 100%.
        Assert.Equal(100.0, axeTechnique.Moyenne);

        var topVigilance = Assert.Single(dashboard.TopPointsDeVigilance);
        Assert.Equal(vigilanceTagPartage, topVigilance.Tag);
        Assert.Equal(1, topVigilance.Nombre);

        Assert.Equal(2, dashboard.CandidatsUniques);
        Assert.Equal(1, dashboard.CandidatsAvecGrilleComplete);
        Assert.Equal(50.0, dashboard.TauxCompletionGrilleH);
    }

    [Fact]
    public async Task GetDashboardAsync_EstScopeALEntrepriseActive_NeVoitPasLesDonneesDUneAutreEntreprise()
    {
        using var scope = fixture.Services.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var candidateService = scope.ServiceProvider.GetRequiredService<ICandidateProfileService>();
        var posteService = scope.ServiceProvider.GetRequiredService<IPosteService>();
        var analyticsService = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();

        var suffix = Guid.NewGuid();
        var companyA = await fixture.CreateCompanyAsync($"Entreprise Analytics A {suffix}", $"analytics-test-manager-a-{suffix}");
        var companyB = await fixture.CreateCompanyAsync($"Entreprise Analytics B {suffix}", $"analytics-test-manager-b-{suffix}");

        // Seule l'entreprise A reçoit un poste et une candidature.
        tenantContext.SetActiveCompany(companyA.Id, companyA.SchemaName);
        var candidatId = await candidateService.GetOrCreateProfileIdAsync($"analytics-candidat-scope-{suffix}");
        var posteId = await posteService.CreatePosteAsync("Poste entreprise A", null, null);
        await posteService.PostulerAsync(companyA.Id, posteId, candidatId);

        // L'entreprise B n'a RIEN reçu — son tableau de bord doit être entièrement vide, jamais un agrégat
        // qui fuiterait les données de l'entreprise A (même exigence que le Vivier/PostesRecrutement).
        tenantContext.SetActiveCompany(companyB.Id, companyB.SchemaName);
        var dashboardB = await analyticsService.GetDashboardAsync();

        Assert.Equal(0, dashboardB.PostesOuverts);
        Assert.Equal(0, dashboardB.PostesFermes);
        Assert.Equal(0, dashboardB.TotalCandidatures);
        Assert.All(dashboardB.Funnel, f => Assert.Equal(0, f.Nombre));
        Assert.Null(dashboardB.ScoreMoyenGlobal);
        Assert.All(dashboardB.MoyennesParAxe, a => Assert.Null(a.Moyenne));
        Assert.Empty(dashboardB.TopPointsDeVigilance);
        Assert.Equal(0, dashboardB.CandidatsUniques);
        Assert.Null(dashboardB.TauxCompletionGrilleH);

        // Ré-active A : ses propres données doivent être toujours là, intactes — la vue vide de B n'était
        // pas un effet de bord d'une perte de données côté A.
        tenantContext.SetActiveCompany(companyA.Id, companyA.SchemaName);
        var dashboardA = await analyticsService.GetDashboardAsync();
        Assert.Equal(1, dashboardA.PostesOuverts);
        Assert.Equal(1, dashboardA.TotalCandidatures);
    }
}
