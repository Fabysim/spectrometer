using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Compatibility;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.ProfilCandidat.Services;
using Spectrometre.Modules.ProfilEntreprise.Services;
using Spectrometre.Modules.SuiviEvolutif.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Vérifie que les mutations ciblées de la grille H/K (déjà en place depuis le correctif de concurrence)
/// tracent bien un changement via <c>IProfileChangeRecorder</c>, sans que ProfilCandidat/ProfilEntreprise
/// n'aient de référence de projet vers SuiviEvolutif (voir le manifeste).
/// </summary>
[Collection("Base de données partagée")]
public sealed class SuiviEvolutifTests(ServiceFixture fixture)
{
    [Fact]
    public async Task ToggleTagAsync_CoteCandidat_EstTraceDansLHistorique()
    {
        using var scope = fixture.Services.CreateScope();
        var candidateUserId = $"suivi-test-candidat-{Guid.NewGuid()}";
        var candidateProfileId = await scope.ServiceProvider.GetRequiredService<ICandidateProfileService>()
            .GetOrCreateProfileIdAsync(candidateUserId);

        var candidateService = scope.ServiceProvider.GetRequiredService<ICandidateProfileService>();
        await candidateService.ToggleTagAsync(candidateProfileId, CriteriaField.Technique, "Gestion de projet", isChecked: true);

        var historique = await scope.ServiceProvider.GetRequiredService<ISuiviEvolutifService>()
            .GetHistoriqueCandidatAsync(candidateProfileId);

        var entree = Assert.Single(historique);
        Assert.Equal("Compétences techniques", entree.Description);
        Assert.Equal("", entree.AncienneValeur);
        Assert.Equal("Gestion de projet", entree.NouvelleValeur);

        // Décocher génère une DEUXIÈME entrée distincte (avant/après inversés), pas une modification de la première.
        await candidateService.ToggleTagAsync(candidateProfileId, CriteriaField.Technique, "Gestion de projet", isChecked: false);
        var historiqueApres = await scope.ServiceProvider.GetRequiredService<ISuiviEvolutifService>()
            .GetHistoriqueCandidatAsync(candidateProfileId);

        Assert.Equal(2, historiqueApres.Count);
        // Ordre chronologique inversé : la plus récente en premier.
        Assert.Equal("Gestion de projet", historiqueApres[0].AncienneValeur);
        Assert.Equal("", historiqueApres[0].NouvelleValeur);
    }

    [Fact]
    public async Task SetRythmeTravailAsync_CoteEntreprise_EstTraceSiSuiviEvolutifActif()
    {
        var suffix = Guid.NewGuid();
        var company = await fixture.CreateCompanyAsync($"Entreprise Suivi {suffix}", $"suivi-test-manager-{suffix}");

        using var scope = fixture.Services.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenantContext.SetActiveCompany(company.Id, company.SchemaName);

        var companyService = scope.ServiceProvider.GetRequiredService<ICompanyProfileService>();
        var companyProfileId = await companyService.GetOrCreateProfileIdAsync();

        await companyService.SetRythmeTravailAsync(companyProfileId, 4);

        var historique = await scope.ServiceProvider.GetRequiredService<ISuiviEvolutifService>()
            .GetHistoriqueEntrepriseAsync(companyProfileId);

        var entree = Assert.Single(historique);
        Assert.Equal("Rythme de travail", entree.Description);
        Assert.Null(entree.AncienneValeur);
        Assert.Equal("4", entree.NouvelleValeur);
    }

    [Fact]
    public async Task ToggleTagAsync_CoteEntreprise_NEstPasTraceSiSuiviEvolutifNonActif()
    {
        // Entreprise volontairement SANS SuiviEvolutif actif — voir ProfileChangeRecorder : aucune trace
        // ne doit être écrite, comportement par défaut sûr documenté sur IProfileChangeRecorder.
        var suffix = Guid.NewGuid();
        var company = await fixture.CreateCompanyAsync(
            $"Entreprise SansSuivi {suffix}", $"suivi-test-manager-sans-{suffix}",
            skipSchemaForModuleCodes: [Spectrometre.Modules.SuiviEvolutif.ServiceCollectionExtensions.Manifest.Code]);

        // On désactive explicitement le module pour ce test (CreateCompanyAsync l'active par défaut).
        using (var setupScope = fixture.Services.CreateScope())
        {
            var coreDb = setupScope.ServiceProvider.GetRequiredService<Spectrometre.Core.Data.CoreDbContext>();
            var activation = await coreDb.ModuleActivations
                .FirstAsync(a => a.CompanyId == company.Id && a.ModuleCode == Spectrometre.Modules.SuiviEvolutif.ServiceCollectionExtensions.Manifest.Code);
            activation.IsActive = false;
            await coreDb.SaveChangesAsync();
        }

        using var scope = fixture.Services.CreateScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenantContext.SetActiveCompany(company.Id, company.SchemaName);

        var companyService = scope.ServiceProvider.GetRequiredService<ICompanyProfileService>();
        var companyProfileId = await companyService.GetOrCreateProfileIdAsync();

        // Ne doit pas lever malgré le schéma SuiviEvolutif manquant pour ce tenant (module non actif).
        await companyService.ToggleTagAsync(companyProfileId, CriteriaField.Culturelle, "Respect", isChecked: true);
    }
}
