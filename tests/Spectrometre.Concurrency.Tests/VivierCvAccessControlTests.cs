using Spectrometre.Modules.ProfilEntreprise.Services;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Compatibility;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Recrutement.Services;
using Spectrometre.Modules.ProfilCandidat.Entities;
using Spectrometre.Modules.ProfilCandidat.Services;
using Spectrometre.Modules.Vivier.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Vérifie que l'affichage du CV côté entreprise (ajouté à <c>IVivierService.GetCandidateDetailAsync</c>,
/// voir sa remarque) respecte EXACTEMENT la même garde de confidentialité que les critères de compatibilité
/// déjà en place — jamais un nouveau chemin d'accès parallèle. Même structure que
/// <see cref="CompatibiliteAccessControlTests"/>.
/// </summary>
[Collection("Base de données partagée")]
public sealed class VivierCvAccessControlTests(ServiceFixture fixture)
{
    private IServiceScope NewScope() => fixture.Services.CreateScope();

    private async Task<int> CreateCandidateWithCvAndCriteriaAsync(string userId)
    {
        using var scope = NewScope();
        var candidateService = scope.ServiceProvider.GetRequiredService<ICandidateProfileService>();
        var candidateProfileId = await candidateService.GetOrCreateProfileIdAsync(userId);

        await candidateService.SaveCoordonneesAsync(candidateProfileId, new CvCoordonnees
        {
            Nom = "Martin",
            Prenoms = "Alex",
            ProfilOuPosteRecherche = "Chef de projet",
        });
        // Critères de grille optionnels pour GetCandidateDetailAsync (null → Empty) — le CV
        // reste lisible dès qu'il y a une candidature réelle vers l'entreprise active.
        await candidateService.ToggleTagAsync(candidateProfileId, CriteriaField.Technique, "tag-test-vivier-cv", true);

        return candidateProfileId;
    }

    private async Task PostulerAsync(Company company, int candidateProfileId)
    {
        using var scope = NewScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenantContext.SetActiveCompany(company.Id, company.SchemaName);

        var posteService = scope.ServiceProvider.GetRequiredService<IPosteService>();
        var posteId = await posteService.CreatePosteAsync($"Poste de test {Guid.NewGuid()}", null, null);
        await posteService.PostulerAsync(company.Id, posteId, candidateProfileId);
    }

    [Fact]
    public async Task EntrepriseAvecCandidatureReelle_VoitLeCvDuCandidat()
    {
        var suffix = Guid.NewGuid();
        var candidatUserId = $"vivier-cv-candidat-{suffix}";
        var employeUserId = $"vivier-cv-manager-{suffix}";

        var company = await fixture.CreateCompanyAsync($"Entreprise Vivier CV {suffix}", employeUserId);
        var candidateProfileId = await CreateCandidateWithCvAndCriteriaAsync(candidatUserId);
        await PostulerAsync(company, candidateProfileId);

        using var scope = NewScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>().SetActiveCompany(company.Id, company.SchemaName);
        var vivierService = scope.ServiceProvider.GetRequiredService<IVivierService>();

        var detail = await vivierService.GetCandidateDetailAsync(candidateProfileId);

        Assert.NotNull(detail);
        Assert.NotNull(detail!.Cv.Coordonnees);
        Assert.Equal("Martin", detail.Cv.Coordonnees!.Nom);
    }

    [Fact]
    public async Task CandidatAyantPostule_SansGrilleCompatibilite_ObtientDetailAvecCriteresVides()
    {
        var suffix = Guid.NewGuid();
        var candidatUserId = $"vivier-cv-candidat-sans-grille-{suffix}";
        var employeUserId = $"vivier-cv-manager-sans-grille-{suffix}";

        var company = await fixture.CreateCompanyAsync($"Entreprise Vivier grille vide {suffix}", employeUserId);

        int candidateProfileId;
        using (var scope = NewScope())
        {
            var candidateService = scope.ServiceProvider.GetRequiredService<ICandidateProfileService>();
            candidateProfileId = await candidateService.GetOrCreateProfileIdAsync(candidatUserId);
            // Profil créé, CV éventuellement vide, AUCUNE ligne CandidateCompatibilityCriteria.
            Assert.Null(await candidateService.GetCompatibilityCriteriaAsync(candidateProfileId));
        }

        await PostulerAsync(company, candidateProfileId);

        using var readScope = NewScope();
        readScope.ServiceProvider.GetRequiredService<ITenantContext>()
            .SetActiveCompany(company.Id, company.SchemaName);
        var vivierService = readScope.ServiceProvider.GetRequiredService<IVivierService>();

        var detail = await vivierService.GetCandidateDetailAsync(candidateProfileId);

        Assert.NotNull(detail);
        Assert.True(detail!.Criteres.EstVide);
        Assert.Empty(detail.Criteres.TechniqueTags);
        Assert.Null(detail.Criteres.RythmeTravail);
    }

    [Fact]
    public async Task EntrepriseSansCandidatureReelle_NAccedePasAuCv()
    {
        var suffix = Guid.NewGuid();
        var candidatUserId = $"vivier-cv-candidat-jamais-{suffix}";
        var employeUserId = $"vivier-cv-manager-{suffix}";

        var company = await fixture.CreateCompanyAsync($"Entreprise Vivier CV {suffix}", employeUserId);
        // Le candidat existe et a rempli son CV, mais n'a jamais postulé à CETTE entreprise.
        var candidateProfileId = await CreateCandidateWithCvAndCriteriaAsync(candidatUserId);

        using var scope = NewScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>().SetActiveCompany(company.Id, company.SchemaName);
        var vivierService = scope.ServiceProvider.GetRequiredService<IVivierService>();

        var detail = await vivierService.GetCandidateDetailAsync(candidateProfileId);

        Assert.Null(detail);
    }
}
