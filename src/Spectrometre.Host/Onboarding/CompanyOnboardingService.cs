using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Data;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Compatibilite.Data;
using Spectrometre.Modules.PostesRecrutement.Data;
using Spectrometre.Modules.ProfilEntreprise.Data;
using ProfilCandidatModule = Spectrometre.Modules.ProfilCandidat.ServiceCollectionExtensions;
using ProfilEntrepriseModule = Spectrometre.Modules.ProfilEntreprise.ServiceCollectionExtensions;
using CompatibiliteModule = Spectrometre.Modules.Compatibilite.ServiceCollectionExtensions;
using PostesRecrutementModule = Spectrometre.Modules.PostesRecrutement.ServiceCollectionExtensions;
using VivierModule = Spectrometre.Modules.Vivier.ServiceCollectionExtensions;

namespace Spectrometre.Host.Onboarding;

/// <summary>
/// Orchestration de la création d'une entreprise : ne peut vivre que dans Host, seul projet qui
/// référence à la fois le noyau et les modules tenant-scopés. Active par défaut les 3 modules du
/// premier cycle pour chaque nouvelle entreprise (voir consigne : « les 3 modules seront activés
/// ensemble pour tous les tenants » pour ce cycle).
/// </summary>
public sealed class CompanyOnboardingService(
    ICompanyProvisioningService companyProvisioningService,
    IModuleRegistry moduleRegistry,
    ITenantSchemaProvisioner schemaProvisioner,
    IDbContextFactory<ProfilEntrepriseDbContext> profilEntrepriseDbFactory,
    IDbContextFactory<CompatibiliteDbContext> compatibiliteDbFactory,
    IDbContextFactory<PostesRecrutementDbContext> postesRecrutementDbFactory)
{
    private const string TemplateSchema = "public";

    public async Task<Company> CreateCompanyAsync(string companyName, string ownerUserId, CoreDbContext coreDb, CancellationToken cancellationToken = default)
    {
        var company = await companyProvisioningService.CreateCompanyAsync(companyName, ownerUserId, coreDb, cancellationToken);

        // Applique le schéma (tables) de chaque module tenant-scopé au nouveau schéma — voir la limite documentée sur ITenantSchemaProvisioner.
        // Instances fraîches (schéma "gabarit" par défaut) via la factory, indépendamment de tout tenant déjà actif dans ce scope.
        await using (var profilEntrepriseDb = await profilEntrepriseDbFactory.CreateDbContextAsync(cancellationToken))
            await schemaProvisioner.ApplyModuleSchemaAsync(profilEntrepriseDb, TemplateSchema, company.SchemaName, cancellationToken);

        await using (var compatibiliteDb = await compatibiliteDbFactory.CreateDbContextAsync(cancellationToken))
            await schemaProvisioner.ApplyModuleSchemaAsync(compatibiliteDb, TemplateSchema, company.SchemaName, cancellationToken);

        await using (var postesDb = await postesRecrutementDbFactory.CreateDbContextAsync(cancellationToken))
            await schemaProvisioner.ApplyModuleSchemaAsync(postesDb, TemplateSchema, company.SchemaName, cancellationToken);

        foreach (var manifest in new[]
                 {
                     ProfilCandidatModule.Manifest,
                     ProfilEntrepriseModule.Manifest,
                     CompatibiliteModule.Manifest,
                     PostesRecrutementModule.Manifest,
                     // Vivier n'a pas de schéma propre (voir son ServiceCollectionExtensions) — rien à
                     // provisionner via ITenantSchemaProvisioner, seulement l'activation ci-dessous.
                     VivierModule.Manifest,
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
