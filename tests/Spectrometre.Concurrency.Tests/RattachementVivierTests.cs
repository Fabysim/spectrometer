using Spectrometre.Modules.ProfilEntreprise.Services;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Recrutement.Services;
using Spectrometre.Modules.ProfilCandidat.Services;
using Spectrometre.Modules.Vivier.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Rattachement d'un candidat déjà connu (vivier / index recrutement) vers un autre poste de la
/// même entreprise — <see cref="IPosteService.RattacherCandidatDepuisVivierAsync"/> et
/// <see cref="IVivierService.GetCandidatsEligiblesPourPosteAsync"/>.
/// </summary>
[Collection("Base de données partagée")]
public sealed class RattachementVivierTests(ServiceFixture fixture)
{
    private IServiceScope NewScope() => fixture.Services.CreateScope();

    [Fact]
    public async Task CandidatDuVivierPeutEtreRattacheAUnAutrePosteDeLaMemeEntreprise()
    {
        var suffix = Guid.NewGuid();
        var ownerUserId = $"rattache-owner-{suffix}";
        var candidatUserId = $"rattache-candidat-{suffix}";

        var company = await fixture.CreateCompanyAsync($"Entreprise Rattache {suffix}", ownerUserId);

        int posteX;
        int posteY;
        int candidateProfileId;

        using (var setupScope = NewScope())
        {
            candidateProfileId = await setupScope.ServiceProvider
                .GetRequiredService<ICandidateProfileService>()
                .GetOrCreateProfileIdAsync(candidatUserId);

            setupScope.ServiceProvider.GetRequiredService<ITenantContext>()
                .SetActiveCompany(company.Id, company.SchemaName);
            var posteService = setupScope.ServiceProvider.GetRequiredService<IPosteService>();

            posteX = await posteService.CreatePosteAsync($"Poste X {suffix}", null, null);
            posteY = await posteService.CreatePosteAsync($"Poste Y {suffix}", null, null);
            await posteService.PostulerAsync(company.Id, posteX, candidateProfileId);
        }

        using var scope = NewScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>()
            .SetActiveCompany(company.Id, company.SchemaName);
        var service = scope.ServiceProvider.GetRequiredService<IPosteService>();
        var vivier = scope.ServiceProvider.GetRequiredService<IVivierService>();

        var eligibles = await vivier.GetCandidatsEligiblesPourPosteAsync(posteY);
        Assert.Contains(eligibles, e => e.CandidateProfileId == candidateProfileId);

        var candidatureId = await service.RattacherCandidatDepuisVivierAsync(posteY, candidateProfileId);
        Assert.True(candidatureId > 0);

        var surY = await service.GetCandidaturesAsync(posteY);
        Assert.Contains(surY, c => c.Id == candidatureId && c.CandidateProfileId == candidateProfileId);

        var eligiblesApres = await vivier.GetCandidatsEligiblesPourPosteAsync(posteY);
        Assert.DoesNotContain(eligiblesApres, e => e.CandidateProfileId == candidateProfileId);
    }

    [Fact]
    public async Task CandidatSansCandidatureDansLEntrepriseNePeutPasEtreRattache()
    {
        var suffix = Guid.NewGuid();
        var ownerUserId = $"rattache-jamais-owner-{suffix}";
        var candidatUserId = $"rattache-jamais-candidat-{suffix}";

        var company = await fixture.CreateCompanyAsync($"Entreprise Rattache Jamais {suffix}", ownerUserId);

        int posteId;
        int candidateProfileId;

        using (var setupScope = NewScope())
        {
            candidateProfileId = await setupScope.ServiceProvider
                .GetRequiredService<ICandidateProfileService>()
                .GetOrCreateProfileIdAsync(candidatUserId);

            setupScope.ServiceProvider.GetRequiredService<ITenantContext>()
                .SetActiveCompany(company.Id, company.SchemaName);
            posteId = await setupScope.ServiceProvider.GetRequiredService<IPosteService>()
                .CreatePosteAsync($"Poste seul {suffix}", null, null);
        }

        using var scope = NewScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>()
            .SetActiveCompany(company.Id, company.SchemaName);
        var service = scope.ServiceProvider.GetRequiredService<IPosteService>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RattacherCandidatDepuisVivierAsync(posteId, candidateProfileId));
    }

    [Fact]
    public async Task CandidatDUneEntrepriseNePeutPasEtreRattacheSurUneAutre()
    {
        var suffix = Guid.NewGuid();
        var ownerA = $"rattache-iso-a-{suffix}";
        var ownerB = $"rattache-iso-b-{suffix}";
        var candidatUserId = $"rattache-iso-candidat-{suffix}";

        var companyA = await fixture.CreateCompanyAsync($"Entreprise Rattache A {suffix}", ownerA);
        var companyB = await fixture.CreateCompanyAsync($"Entreprise Rattache B {suffix}", ownerB);

        int posteIdB;
        int candidateProfileId;

        using (var setupScope = NewScope())
        {
            candidateProfileId = await setupScope.ServiceProvider
                .GetRequiredService<ICandidateProfileService>()
                .GetOrCreateProfileIdAsync(candidatUserId);

            var tenantContext = setupScope.ServiceProvider.GetRequiredService<ITenantContext>();
            var posteService = setupScope.ServiceProvider.GetRequiredService<IPosteService>();

            tenantContext.SetActiveCompany(companyA.Id, companyA.SchemaName);
            var posteXA = await posteService.CreatePosteAsync($"Poste A {suffix}", null, null);
            await posteService.PostulerAsync(companyA.Id, posteXA, candidateProfileId);

            tenantContext.SetActiveCompany(companyB.Id, companyB.SchemaName);
            posteIdB = await posteService.CreatePosteAsync($"Poste B {suffix}", null, null);
        }

        using var scopeB = NewScope();
        scopeB.ServiceProvider.GetRequiredService<ITenantContext>()
            .SetActiveCompany(companyB.Id, companyB.SchemaName);
        var serviceB = scopeB.ServiceProvider.GetRequiredService<IPosteService>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => serviceB.RattacherCandidatDepuisVivierAsync(posteIdB, candidateProfileId));

        Assert.Empty(await serviceB.GetCandidaturesAsync(posteIdB));
    }

    [Fact]
    public async Task RattachementEstIdempotent()
    {
        var suffix = Guid.NewGuid();
        var ownerUserId = $"rattache-idem-owner-{suffix}";
        var candidatUserId = $"rattache-idem-candidat-{suffix}";

        var company = await fixture.CreateCompanyAsync($"Entreprise Rattache Idem {suffix}", ownerUserId);

        int posteX;
        int posteY;
        int candidateProfileId;

        using (var setupScope = NewScope())
        {
            candidateProfileId = await setupScope.ServiceProvider
                .GetRequiredService<ICandidateProfileService>()
                .GetOrCreateProfileIdAsync(candidatUserId);

            setupScope.ServiceProvider.GetRequiredService<ITenantContext>()
                .SetActiveCompany(company.Id, company.SchemaName);
            var posteService = setupScope.ServiceProvider.GetRequiredService<IPosteService>();

            posteX = await posteService.CreatePosteAsync($"Poste X idem {suffix}", null, null);
            posteY = await posteService.CreatePosteAsync($"Poste Y idem {suffix}", null, null);
            await posteService.PostulerAsync(company.Id, posteX, candidateProfileId);
        }

        using var scope = NewScope();
        scope.ServiceProvider.GetRequiredService<ITenantContext>()
            .SetActiveCompany(company.Id, company.SchemaName);
        var service = scope.ServiceProvider.GetRequiredService<IPosteService>();

        var premierId = await service.RattacherCandidatDepuisVivierAsync(posteY, candidateProfileId);
        var secondId = await service.RattacherCandidatDepuisVivierAsync(posteY, candidateProfileId);

        Assert.Equal(premierId, secondId);
        Assert.Single(await service.GetCandidaturesAsync(posteY));
    }
}
