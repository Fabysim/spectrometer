using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Tenancy;
using ProfilCandidatModule = Spectrometre.Modules.ProfilCandidat.ServiceCollectionExtensions;
using ProfilEntrepriseModule = Spectrometre.Modules.ProfilEntreprise.ServiceCollectionExtensions;
using CompatibiliteModule = Spectrometre.Modules.Compatibilite.ServiceCollectionExtensions;
using PostesRecrutementModule = Spectrometre.Modules.PostesRecrutement.ServiceCollectionExtensions;
using VivierModule = Spectrometre.Modules.Vivier.ServiceCollectionExtensions;
using EntretienModule = Spectrometre.Modules.Entretien.ServiceCollectionExtensions;
using AnalyticsModule = Spectrometre.Modules.Analytics.ServiceCollectionExtensions;
using GestionDuTempsModule = Spectrometre.Modules.GestionDuTemps.ServiceCollectionExtensions;

namespace Spectrometre.Host.Onboarding;

/// <summary>
/// Orchestration de la création d'une entreprise : ne peut vivre que dans Host, seul projet qui
/// référence à la fois le noyau et les modules tenant-scopés.
/// </summary>
/// <remarks>
/// Le provisionnement de schéma boucle sur <see cref="TenantSchemaModuleCatalog"/> plutôt que d'injecter
/// une <c>IDbContextFactory&lt;TContext&gt;</c> par module — c'est cette liste unique (partagée avec
/// <c>TenantSchemaSynchronizer</c>, exécuté au démarrage pour les entreprises déjà existantes) qui évite
/// de dupliquer le code à chaque nouveau module tenant-scopé.
/// <para/>
/// Depuis le cycle inscription : une nouvelle entreprise démarre en abonnement d'ESSAI avec SEULEMENT le
/// module gratuit (Profil Entreprise) activé — plus tous les autres modules d'un coup comme avant. Les
/// modules restants (suite Recrutement, Gestion du temps) s'activent explicitement depuis l'écran
/// « Ajouter un module » (<see cref="ActivateRecruitmentBundleAsync"/>/<see cref="ActivateGestionDuTempsAsync"/>),
/// qui réutilisent la MÊME boucle d'activation (<see cref="ActivateModulesInOrderAsync"/>) que celle utilisée
/// ici pour le module gratuit — aucune logique d'activation dupliquée.
/// </remarks>
public sealed class CompanyOnboardingService(
    ICompanyProvisioningService companyProvisioningService,
    IModuleRegistry moduleRegistry,
    ITenantSchemaProvisioner schemaProvisioner,
    IServiceProvider serviceProvider)
{
    private const string TemplateSchema = "public";

    /// <summary>Seul module activé automatiquement à la création d'une entreprise — gratuit, inclus dans <see cref="PlanCodes.Standard"/>.</summary>
    private static readonly IReadOnlyList<string> FreeTierModuleCodes = [ProfilEntrepriseModule.Manifest.Code];

    /// <summary>
    /// « Recrutement (Postes & Recrutement et modules associés) » de l'écran Ajouter un module — exclut
    /// délibérément SuiviEvolutif (affiché « à venir », non activable dans ce cycle) et GestionDuTemps
    /// (vendu à part, voir <see cref="ActivateGestionDuTempsAsync"/>).
    /// </summary>
    /// <remarks>
    /// <see cref="ProfilCandidatModule"/> n'y figure PLUS (cycle correctif « modules personnels ») : son
    /// manifeste n'est plus une dépendance de Compatibilite (voir le commentaire sur
    /// <c>Spectrometre.Modules.Compatibilite.ServiceCollectionExtensions.Manifest</c>) — l'activer pour le
    /// sujet Company n'a jamais eu d'utilité réelle et faisait apparaître à tort ce module personnel
    /// (scopé UserId, sans écran entreprise) dans le menu/tableau de bord d'une entreprise.
    /// </remarks>
    public static readonly IReadOnlyList<string> RecruitmentBundleModuleCodes =
    [
        CompatibiliteModule.Manifest.Code,
        PostesRecrutementModule.Manifest.Code,
        VivierModule.Manifest.Code,
        EntretienModule.Manifest.Code,
        AnalyticsModule.Manifest.Code,
    ];

    public async Task<Company> CreateCompanyAsync(string companyName, string ownerUserId, CoreDbContext coreDb, CancellationToken cancellationToken = default)
    {
        var company = await companyProvisioningService.CreateCompanyAsync(companyName, ownerUserId, coreDb, cancellationToken);

        // Essai (pas Active) : cohérent avec un abonnement qui commence gratuitement et peut être requalifié
        // plus tard (facturation hors périmètre de ce cycle) — voir ModuleRegistry.IsActiveAsync, Essai vaut
        // Active pour le gating. Sans cette ligne, AUCUN module ne serait effectivement actif pour une
        // entreprise neuve (échec fermé si aucun abonnement), quel que soit l'état de ModuleActivation.
        coreDb.TenantSubscriptions.Add(new TenantSubscription
        {
            CompanyId = company.Id,
            PlanCode = PlanCodes.Standard,
            Status = SubscriptionStatus.Essai,
        });
        await coreDb.SaveChangesAsync(cancellationToken);

        // Applique le schéma (tables) de chaque module tenant-scopé au nouveau schéma, QUE le module soit
        // activé ou non — le provisionnement de schéma et l'activation sont deux notions volontairement
        // découplées (voir IModuleRegistry) : une entreprise peut ainsi activer plus tard un module déjà
        // provisionné, sans repasser par une synchronisation de schéma à ce moment-là.
        foreach (var module in TenantSchemaModuleCatalog.Modules)
        {
            await using var db = await module.CreateDbContextAsync(serviceProvider, cancellationToken);
            await schemaProvisioner.ApplyModuleSchemaAsync(db, TemplateSchema, company.SchemaName, cancellationToken);
        }

        await ActivateModulesInOrderAsync(company.Id, FreeTierModuleCodes, coreDb, cancellationToken);

        return company;
    }

    /// <summary>Active la suite Recrutement (écran Ajouter un module) — tous ses codes sont déjà inclus dans <see cref="PlanCodes.Standard"/>, aucune mise à niveau de plan nécessaire.</summary>
    public Task ActivateRecruitmentBundleAsync(int companyId, CoreDbContext coreDb, CancellationToken cancellationToken = default) =>
        ActivateModulesInOrderAsync(companyId, RecruitmentBundleModuleCodes, coreDb, cancellationToken);

    /// <summary>Active Gestion du temps (écran Ajouter un module) — nécessite de faire passer le plan à <see cref="PlanCodes.StandardPlusTemps"/> (le seul qui l'inclut) avant d'activer.</summary>
    public async Task ActivateGestionDuTempsAsync(int companyId, CoreDbContext coreDb, CancellationToken cancellationToken = default)
    {
        await UpgradeToStandardPlusTempsAsync(companyId, coreDb, cancellationToken);
        await ActivateModulesInOrderAsync(companyId, [GestionDuTempsModule.Manifest.Code], coreDb, cancellationToken);
    }

    private static async Task UpgradeToStandardPlusTempsAsync(int companyId, CoreDbContext coreDb, CancellationToken cancellationToken)
    {
        var subscription = await coreDb.TenantSubscriptions.FirstOrDefaultAsync(s => s.CompanyId == companyId, cancellationToken)
            ?? throw new InvalidOperationException($"Aucun abonnement pour l'entreprise {companyId}.");

        if (subscription.PlanCode != PlanCodes.StandardPlusTemps)
        {
            subscription.PlanCode = PlanCodes.StandardPlusTemps;
            await coreDb.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Boucle d'activation partagée (dans l'ordre de dépendance des manifestes, ignore ce qui est déjà
    /// effectivement actif) — même logique que l'ancienne boucle unique de <c>CreateCompanyAsync</c>,
    /// désormais réutilisée pour le module gratuit à la création ET pour chaque activation ultérieure
    /// depuis l'écran Ajouter un module, sans jamais la dupliquer.
    /// </summary>
    private async Task ActivateModulesInOrderAsync(int companyId, IReadOnlyList<string> moduleCodes, CoreDbContext coreDb, CancellationToken cancellationToken)
    {
        foreach (var moduleCode in moduleCodes)
        {
            if (await moduleRegistry.IsActiveAsync(companyId, moduleCode, coreDb, cancellationToken))
                continue;

            var activeCodes = await moduleRegistry.GetActiveModuleCodesAsync(companyId, coreDb, cancellationToken);
            var manifest = moduleRegistry.Find(moduleCode) ?? throw new InvalidOperationException($"Module inconnu : {moduleCode}");
            if (moduleRegistry.CanActivate(moduleCode, activeCodes, out _) || manifest.RequiredModuleCodes.Count == 0)
                await moduleRegistry.ActivateForCompanyAsync(companyId, moduleCode, coreDb, cancellationToken);
        }
    }
}
