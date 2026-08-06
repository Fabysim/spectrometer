using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Data;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Tenancy;
using ProfilCandidatModule = Spectrometre.Modules.ProfilCandidat.ServiceCollectionExtensions;
using ProfilEntrepriseModule = Spectrometre.Modules.ProfilEntreprise.ServiceCollectionExtensions;
using CompatibiliteModule = Spectrometre.Modules.Compatibilite.ServiceCollectionExtensions;
using PostesRecrutementModule = Spectrometre.Modules.PostesRecrutement.ServiceCollectionExtensions;
using VivierModule = Spectrometre.Modules.Vivier.ServiceCollectionExtensions;
using EntretienModule = Spectrometre.Modules.Entretien.ServiceCollectionExtensions;
using SuiviEvolutifModule = Spectrometre.Modules.SuiviEvolutif.ServiceCollectionExtensions;
using AnalyticsModule = Spectrometre.Modules.Analytics.ServiceCollectionExtensions;

namespace Spectrometre.Host.Onboarding;

/// <summary>
/// Orchestration de la création d'une entreprise : ne peut vivre que dans Host, seul projet qui
/// référence à la fois le noyau et les modules tenant-scopés. Active par défaut tous les modules connus
/// pour chaque nouvelle entreprise (dans l'ordre de dépendance de leur manifeste).
/// </summary>
/// <remarks>
/// Le provisionnement de schéma boucle sur <see cref="TenantSchemaModuleCatalog"/> plutôt que d'injecter
/// une <c>IDbContextFactory&lt;TContext&gt;</c> par module — c'est cette liste unique (partagée avec
/// <c>TenantSchemaSynchronizer</c>, exécuté au démarrage pour les entreprises déjà existantes) qui évite
/// de dupliquer le code à chaque nouveau module tenant-scopé.
/// </remarks>
public sealed class CompanyOnboardingService(
    ICompanyProvisioningService companyProvisioningService,
    IModuleRegistry moduleRegistry,
    ITenantSchemaProvisioner schemaProvisioner,
    IServiceProvider serviceProvider)
{
    private const string TemplateSchema = "public";

    public async Task<Company> CreateCompanyAsync(string companyName, string ownerUserId, CoreDbContext coreDb, CancellationToken cancellationToken = default)
    {
        var company = await companyProvisioningService.CreateCompanyAsync(companyName, ownerUserId, coreDb, cancellationToken);

        // Applique le schéma (tables) de chaque module tenant-scopé au nouveau schéma — voir la limite documentée sur ITenantSchemaProvisioner.
        // Instances fraîches (schéma "gabarit" par défaut) via la factory, indépendamment de tout tenant déjà actif dans ce scope.
        foreach (var module in TenantSchemaModuleCatalog.Modules)
        {
            await using var db = await module.CreateDbContextAsync(serviceProvider, cancellationToken);
            await schemaProvisioner.ApplyModuleSchemaAsync(db, TemplateSchema, company.SchemaName, cancellationToken);
        }

        foreach (var manifest in new[]
                 {
                     ProfilCandidatModule.Manifest,
                     ProfilEntrepriseModule.Manifest,
                     CompatibiliteModule.Manifest,
                     PostesRecrutementModule.Manifest,
                     // Vivier n'a pas de schéma propre (voir son ServiceCollectionExtensions) — rien à
                     // provisionner via ITenantSchemaProvisioner, seulement l'activation ci-dessous.
                     VivierModule.Manifest,
                     EntretienModule.Manifest,
                     SuiviEvolutifModule.Manifest,
                     // Analytics n'a pas non plus de schéma propre (voir son ServiceCollectionExtensions) —
                     // même situation que Vivier, seulement l'activation ci-dessous.
                     AnalyticsModule.Manifest,
                 })
        {
            if (await moduleRegistry.IsActiveAsync(company.Id, manifest.Code, coreDb, cancellationToken))
                continue;

            var activeCodes = await moduleRegistry.GetActiveModuleCodesAsync(company.Id, coreDb, cancellationToken);
            if (moduleRegistry.CanActivate(manifest.Code, activeCodes, out _) || manifest.RequiredModuleCodes.Count == 0)
                await moduleRegistry.ActivateForCompanyAsync(company.Id, manifest.Code, coreDb, cancellationToken);
        }

        return company;
    }
}
