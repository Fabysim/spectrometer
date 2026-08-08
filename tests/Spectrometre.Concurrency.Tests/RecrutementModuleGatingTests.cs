using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Data;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Entretien.Services;
using Spectrometre.Modules.ProfilCandidat.Services;
using Spectrometre.Modules.ProfilEntreprise.Services;
using Spectrometre.Modules.Recrutement.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Gating du module <c>Recrutement</c> (étape 2 scission Postes/Recrutement) : sans activation,
/// l'entreprise conserve le socle postes/candidatures/critères, mais les features entretien/IA
/// sont refusées — même règle que <see cref="IModuleRegistry.IsActiveAsync"/> utilisée par
/// <c>EntretienPage</c>, <c>PosteGuideEntrevuePage</c>, <c>PosteCandidatureDetailPage</c> et
/// l'endpoint PDF analyse IA.
/// </summary>
[Collection("Base de données partagée")]
public sealed class RecrutementModuleGatingTests(ServiceFixture fixture)
{
    private const string Recrutement = "Recrutement";

    private IServiceScope NewScope() => fixture.Services.CreateScope();

    [Fact]
    public async Task SansModuleRecrutement_EntretienEstVerrouille_SoclePosteResteAccessible()
    {
        var suffix = Guid.NewGuid();
        var ownerUserId = $"recrut-gate-owner-{suffix}";
        var candidatUserId = $"recrut-gate-cand-{suffix}";

        var company = await fixture.CreateCompanyAsync($"Entreprise Gate Recrut {suffix}", ownerUserId);

        int posteId;
        int candidatureId;
        int candidateProfileId;

        using (var setupScope = NewScope())
        {
            candidateProfileId = await setupScope.ServiceProvider
                .GetRequiredService<ICandidateProfileService>()
                .GetOrCreateProfileIdAsync(candidatUserId);

            setupScope.ServiceProvider.GetRequiredService<ITenantContext>()
                .SetActiveCompany(company.Id, company.SchemaName);
            var posteService = setupScope.ServiceProvider.GetRequiredService<IPosteService>();
            posteId = await posteService.CreatePosteAsync($"Poste gate {suffix}", null, null);
            await posteService.PostulerAsync(company.Id, posteId, candidateProfileId);
            candidatureId = Assert.Single(await posteService.GetCandidaturesAsync(posteId)).Id;
        }

        using var scope = NewScope();
        var moduleRegistry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
        var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        scope.ServiceProvider.GetRequiredService<ITenantContext>()
            .SetActiveCompany(company.Id, company.SchemaName);

        await moduleRegistry.SetActiveAsync(
            ModuleActivationSubjectType.Company, company.Id, Recrutement, isActive: false, coreDb);

        // (a) Même condition que EntretienPage.razor avant GenererGrilleAsync.
        Assert.False(await moduleRegistry.IsActiveAsync(company.Id, Recrutement, coreDb));

        // Socle : candidature + évaluations toujours lisibles (ProfilEntreprise).
        var posteServiceActif = scope.ServiceProvider.GetRequiredService<IPosteService>();
        Assert.NotNull(await posteServiceActif.GetCandidatureAsync(candidatureId));
        _ = await posteServiceActif.GetEvaluationCriteresAsync(candidatureId);

        // La page n'appelle GenererGrilleAsync quand le module est inactif — on vérifie ici la
        // condition de garde, pas le rendu Blazor. (GenererGrilleAsync lui-même reste utilisable
        // côté service pour le cas candidat / autres appels ; la garde UI+endpoint est la barrière.)
        var shouldGenerateGrille = await moduleRegistry.IsActiveAsync(company.Id, Recrutement, coreDb);
        Assert.False(shouldGenerateGrille);
    }

    [Fact]
    public async Task AvecModuleRecrutement_EntretienGrilleToujoursDisponible()
    {
        var suffix = Guid.NewGuid();
        var ownerUserId = $"recrut-gate-ok-owner-{suffix}";
        var candidatUserId = $"recrut-gate-ok-cand-{suffix}";

        var company = await fixture.CreateCompanyAsync($"Entreprise Gate OK {suffix}", ownerUserId);

        int candidateProfileId;
        using (var setupScope = NewScope())
        {
            candidateProfileId = await setupScope.ServiceProvider
                .GetRequiredService<ICandidateProfileService>()
                .GetOrCreateProfileIdAsync(candidatUserId);

            setupScope.ServiceProvider.GetRequiredService<ITenantContext>()
                .SetActiveCompany(company.Id, company.SchemaName);
            var posteService = setupScope.ServiceProvider.GetRequiredService<IPosteService>();
            var posteId = await posteService.CreatePosteAsync($"Poste gate ok {suffix}", null, null);
            await posteService.PostulerAsync(company.Id, posteId, candidateProfileId);
        }

        using var scope = NewScope();
        var moduleRegistry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
        var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        scope.ServiceProvider.GetRequiredService<ITenantContext>()
            .SetActiveCompany(company.Id, company.SchemaName);

        // (b) CreateCompanyAsync active Recrutement par défaut — comportement inchangé.
        Assert.True(await moduleRegistry.IsActiveAsync(company.Id, Recrutement, coreDb));

        var grille = await scope.ServiceProvider.GetRequiredService<IEntretienService>()
            .GenererGrilleAsync(candidateProfileId, ownerUserId);
        Assert.NotNull(grille);
    }

    [Fact]
    public async Task SansModuleRecrutement_ExportPdfAnalyseEstRefuseCommeNonAutorise()
    {
        var suffix = Guid.NewGuid();
        var ownerUserId = $"recrut-pdf-owner-{suffix}";
        var candidatUserId = $"recrut-pdf-cand-{suffix}";

        var company = await fixture.CreateCompanyAsync($"Entreprise PDF Gate {suffix}", ownerUserId);

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
            var entretien = setupScope.ServiceProvider.GetRequiredService<IRecrutementEntretienService>();

            posteId = await posteService.CreatePosteAsync($"Poste PDF {suffix}", null, null);
            await posteService.PostulerAsync(company.Id, posteId, candidateProfileId);
            candidatureId = Assert.Single(await posteService.GetCandidaturesAsync(posteId)).Id;

            // Génère une analyse tant que le module est actif — elle restera en base après désactivation.
            var analyse = await entretien.GenererAnalyseIaAsync(candidatureId, forcerRegeneration: false);
            Assert.NotNull(analyse);
        }

        using var scope = NewScope();
        var moduleRegistry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
        var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var companyProvisioning = scope.ServiceProvider.GetRequiredService<ICompanyProvisioningService>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var postes = scope.ServiceProvider.GetRequiredService<IPosteService>();
        var recrutementEntretien = scope.ServiceProvider.GetRequiredService<IRecrutementEntretienService>();

        await moduleRegistry.SetActiveAsync(
            ModuleActivationSubjectType.Company, company.Id, Recrutement, isActive: false, coreDb);

        // Données conservées (pas de purge à la désactivation).
        tenantContext.SetActiveCompany(company.Id, company.SchemaName);
        Assert.NotNull(await recrutementEntretien.GetAnalyseIaAsync(candidatureId));

        // (c) Même boucle que Program.cs : module inactif → skip (= NotFound final).
        var companies = await companyProvisioning.GetCompaniesForUserAsync(ownerUserId, coreDb);
        var exportAutorise = false;
        foreach (var c in companies)
        {
            if (!await moduleRegistry.IsActiveAsync(c.Id, Recrutement, coreDb))
                continue;

            tenantContext.SetActiveCompany(c.Id, c.SchemaName);
            var candidature = await postes.GetCandidatureAsync(candidatureId);
            if (candidature is null || candidature.PosteId != posteId)
                continue;

            var analyseExistante = await recrutementEntretien.GetAnalyseIaAsync(candidatureId);
            if (analyseExistante is not null)
                exportAutorise = true;
        }

        Assert.False(exportAutorise);

        // Réactivation → l'export redevient possible sans recréer poste/candidature/analyse.
        await moduleRegistry.SetActiveAsync(
            ModuleActivationSubjectType.Company, company.Id, Recrutement, isActive: true, coreDb);
        Assert.True(await moduleRegistry.IsActiveAsync(company.Id, Recrutement, coreDb));
        tenantContext.SetActiveCompany(company.Id, company.SchemaName);
        Assert.NotNull(await recrutementEntretien.GetAnalyseIaAsync(candidatureId));
        Assert.NotNull(await postes.GetCandidatureAsync(candidatureId));
    }
}
