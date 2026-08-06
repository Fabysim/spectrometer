using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Recruitment;
using Spectrometre.Core.Suivi;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Analytics;
using Spectrometre.Modules.Compatibilite;
using Spectrometre.Modules.Compatibilite.Data;
using Spectrometre.Modules.Entretien;
using Spectrometre.Modules.Entretien.Data;
using Spectrometre.Modules.GestionDuTemps;
using Spectrometre.Modules.GestionDuTemps.Data;
using Spectrometre.Modules.GestionDuTemps.Services;
using Spectrometre.Modules.PostesRecrutement;
using Spectrometre.Modules.PostesRecrutement.Data;
using Spectrometre.Modules.PostesRecrutement.Services;
using Spectrometre.Modules.ProfilCandidat;
using Spectrometre.Modules.ProfilCandidat.Data;
using Spectrometre.Modules.ProfilEntreprise;
using Spectrometre.Modules.ProfilEntreprise.Data;
using Spectrometre.Modules.SuiviEvolutif;
using Spectrometre.Modules.SuiviEvolutif.Data;
using Spectrometre.Modules.SuiviEvolutif.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Construit un conteneur DI minimal, exactement avec les mêmes extensions <c>AddXxxModule</c> que
/// <c>Spectrometre.Host.Program</c>, pour tester les services publics tels qu'ils sont réellement
/// utilisés — pas des doublures. Nécessite une instance Postgres accessible (voir <see cref="ConnectionString"/>) :
/// mêmes migrations que l'application réelle, appliquées une fois ici. Un <see cref="ITenantContext"/>
/// neutre pointant vers le schéma "public" est utilisé pour le côté entreprise — ce schéma "gabarit"
/// n'héberge jamais de vraie entreprise (voir ITenantSchemaNameGenerator), c'est donc un bac à sable sûr.
/// </summary>
/// <remarks>
/// N'appelle PAS <c>AddSpectrometreCore</c> (qui enregistre aussi ASP.NET Core Identity — SignInManager,
/// UserManager — inutile ici et qui suppose un hôte ASP.NET Core complet) : seul le sous-ensemble
/// réellement utilisé par les tests est reproduit (CoreDbContext, registre de modules, provisioning).
/// </remarks>
public sealed class ServiceFixture : IAsyncLifetime
{
    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("SPECTROMETRE_TEST_CONNECTION")
        ?? "Host=localhost;Port=5432;Database=spectrometre_v2;Username=postgres;Password=Pil@tes2025";

    public ServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
            })
            .Build();

        var services = new ServiceCollection();

        Action<DbContextOptionsBuilder> configureCoreDbContext = options =>
        {
            options.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
            options.UseNpgsql(ConnectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "core"));
        };
        // Voir Spectrometre.Core.ServiceCollectionExtensions.AddSpectrometreCore : CoreDbContext est
        // enregistré Scoped via un délégué sur la factory plutôt que par un second appel à AddDbContext, qui
        // enregistrerait une configuration DbContextOptions concurrente (dépendance captive + résolution
        // cassée pour les outils de conception EF Core).
        services.AddDbContextFactory<CoreDbContext>(configureCoreDbContext);
        services.AddScoped<CoreDbContext>(sp => sp.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContext());
        services.AddSingleton<ITenantSchemaNameGenerator, TenantSchemaNameGenerator>();
        services.AddScoped<ICompanyProvisioningService, CompanyProvisioningService>();
        services.AddScoped<ITenantSchemaProvisioner, TenantSchemaProvisioner>();
        services.AddSingleton<IModuleRegistry, ModuleRegistry>();
        services.AddScoped<IRecruitmentIndexService, RecruitmentIndexService>();

        services.AddScoped<ITenantContext, TenantContext>();
        services.AddProfilCandidatModule(config);
        services.AddProfilEntrepriseModule(config);
        services.AddCompatibiliteModule(config);
        services.AddPostesRecrutementModule(config);
        services.AddEntretienModule(config);
        services.AddSuiviEvolutifModule(config);
        services.AddAnalyticsModule();
        services.AddGestionDuTempsModule(config);

        // Jamais d'appel réseau réel à Replicate en test — substitue l'implémentation réelle enregistrée
        // par AddGestionDuTempsModule par un double configurable (voir FakeAiSynthesisService).
        services.Replace(ServiceDescriptor.Scoped<IAiSynthesisService, FakeAiSynthesisService>());

        // Même câblage que Spectrometre.Host.Program : l'implémentation réelle est fournie par
        // PostesRecrutement mais enregistrée ici (pas depuis Compatibilite).
        services.AddScoped<ICandidatureExistenceChecker, CandidatureExistenceChecker>();

        // Idem pour ProfilCandidat/ProfilEntreprise → SuiviEvolutif : implémentation réelle par-dessus le
        // no-op (absent ici puisqu'on n'appelle pas AddSpectrometreCore — on l'enregistre directement).
        services.AddScoped<IProfileChangeRecorder, ProfileChangeRecorder>();

        Services = services.BuildServiceProvider();

        using (var scope = Services.CreateScope())
        {
            var moduleRegistry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
            moduleRegistry.Register(Spectrometre.Modules.ProfilCandidat.ServiceCollectionExtensions.Manifest);
            moduleRegistry.Register(Spectrometre.Modules.ProfilEntreprise.ServiceCollectionExtensions.Manifest);
            moduleRegistry.Register(Spectrometre.Modules.Compatibilite.ServiceCollectionExtensions.Manifest);
            moduleRegistry.Register(Spectrometre.Modules.PostesRecrutement.ServiceCollectionExtensions.Manifest);
            moduleRegistry.Register(Spectrometre.Modules.Entretien.ServiceCollectionExtensions.Manifest);
            moduleRegistry.Register(Spectrometre.Modules.SuiviEvolutif.ServiceCollectionExtensions.Manifest);
            moduleRegistry.Register(Spectrometre.Modules.Analytics.ServiceCollectionExtensions.Manifest);
            moduleRegistry.Register(Spectrometre.Modules.GestionDuTemps.ServiceCollectionExtensions.Manifest);

            var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            await coreDb.Database.MigrateAsync();
        }

        // Les migrations de ProfilCandidat (schéma fixe) et le schéma "gabarit" public de
        // ProfilEntreprise/Compatibilite/PostesRecrutement (tenant-scopés) doivent déjà être en place —
        // idempotent.
        await using var candidatDb = await Services.GetRequiredService<IDbContextFactory<ProfilCandidatDbContext>>().CreateDbContextAsync();
        await candidatDb.Database.MigrateAsync();

        await using var entrepriseDb = await Services.GetRequiredService<IDbContextFactory<ProfilEntrepriseDbContext>>().CreateDbContextAsync();
        await entrepriseDb.Database.MigrateAsync();

        await using var compatibiliteDb = await Services.GetRequiredService<IDbContextFactory<CompatibiliteDbContext>>().CreateDbContextAsync();
        await compatibiliteDb.Database.MigrateAsync();

        await using var postesDb = await Services.GetRequiredService<IDbContextFactory<PostesRecrutementDbContext>>().CreateDbContextAsync();
        await postesDb.Database.MigrateAsync();

        await using var entretienDb = await Services.GetRequiredService<IDbContextFactory<EntretienDbContext>>().CreateDbContextAsync();
        await entretienDb.Database.MigrateAsync();

        await using var suiviCandidatDb = await Services.GetRequiredService<IDbContextFactory<SuiviEvolutifCandidatDbContext>>().CreateDbContextAsync();
        await suiviCandidatDb.Database.MigrateAsync();

        await using var suiviEntrepriseDb = await Services.GetRequiredService<IDbContextFactory<SuiviEvolutifEntrepriseDbContext>>().CreateDbContextAsync();
        await suiviEntrepriseDb.Database.MigrateAsync();

        await using var gestionDuTempsDb = await Services.GetRequiredService<IDbContextFactory<GestionDuTempsDbContext>>().CreateDbContextAsync();
        await gestionDuTempsDb.Database.MigrateAsync();
    }

    /// <summary>
    /// Catalogue local des modules tenant-scopés, même principe que
    /// <c>Spectrometre.Host.Onboarding.TenantSchemaModuleCatalog</c> (le projet de test ne référence pas
    /// Host) — utilisé à la fois par <see cref="CreateCompanyAsync"/> et par les tests de synchronisation.
    /// </summary>
    public static readonly IReadOnlyList<TenantSchemaModule> TenantModules =
    [
        new(Spectrometre.Modules.ProfilEntreprise.ServiceCollectionExtensions.Manifest.Code,
            async (sp, ct) => await sp.GetRequiredService<IDbContextFactory<ProfilEntrepriseDbContext>>().CreateDbContextAsync(ct)),
        new(Spectrometre.Modules.Compatibilite.ServiceCollectionExtensions.Manifest.Code,
            async (sp, ct) => await sp.GetRequiredService<IDbContextFactory<CompatibiliteDbContext>>().CreateDbContextAsync(ct)),
        new(Spectrometre.Modules.PostesRecrutement.ServiceCollectionExtensions.Manifest.Code,
            async (sp, ct) => await sp.GetRequiredService<IDbContextFactory<PostesRecrutementDbContext>>().CreateDbContextAsync(ct)),
        new(Spectrometre.Modules.Entretien.ServiceCollectionExtensions.Manifest.Code,
            async (sp, ct) => await sp.GetRequiredService<IDbContextFactory<EntretienDbContext>>().CreateDbContextAsync(ct)),
        new(Spectrometre.Modules.SuiviEvolutif.ServiceCollectionExtensions.Manifest.Code,
            async (sp, ct) => await sp.GetRequiredService<IDbContextFactory<SuiviEvolutifEntrepriseDbContext>>().CreateDbContextAsync(ct)),
    ];

    /// <summary>
    /// Crée une entreprise de test complète : schéma provisionné, propriétaire lié (<c>UserCompanyLink</c>),
    /// et tous les modules tenant-scopés activés — le strict nécessaire pour que
    /// <c>CandidatureExistenceChecker</c> puisse réellement trouver une candidature dans ce tenant.
    /// </summary>
    /// <param name="skipSchemaForModuleCodes">
    /// Codes de module à activer SANS provisionner leur schéma — simule une entreprise "en retard" (module
    /// actif mais schéma jamais appliqué), pour tester <c>TenantSchemaSynchronizer</c>.
    /// </param>
    public async Task<Company> CreateCompanyAsync(string name, string ownerUserId, IReadOnlyCollection<string>? skipSchemaForModuleCodes = null)
    {
        skipSchemaForModuleCodes ??= [];

        using var scope = Services.CreateScope();
        var provisioning = scope.ServiceProvider.GetRequiredService<ICompanyProvisioningService>();
        var moduleRegistry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
        var provisioner = scope.ServiceProvider.GetRequiredService<ITenantSchemaProvisioner>();
        var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var company = await provisioning.CreateCompanyAsync(name, ownerUserId, coreDb);

        // Même câblage que Spectrometre.Host.Onboarding.CompanyOnboardingService : sans abonnement, aucun
        // module ne serait effectivement actif pour cette entreprise (échec fermé, voir
        // ModuleRegistry.IsActiveAsync) — Standard couvre les 8 modules Matching Emploi, pas GestionDuTemps.
        coreDb.TenantSubscriptions.Add(new TenantSubscription
        {
            CompanyId = company.Id,
            PlanCode = PlanCodes.Standard,
            Status = SubscriptionStatus.Active,
        });
        await coreDb.SaveChangesAsync();

        foreach (var module in TenantModules)
        {
            if (skipSchemaForModuleCodes.Contains(module.ModuleCode))
                continue;

            await using var db = await module.CreateDbContextAsync(Services, default);
            await provisioner.ApplyModuleSchemaAsync(db, "public", company.SchemaName);
        }

        // Ordre imposé par les dépendances déclarées aux manifestes (Compatibilite requiert ProfilEntreprise ;
        // PostesRecrutement requiert ProfilEntreprise ; Entretien requiert Compatibilite). ProfilCandidat
        // n'est PLUS activé ici pour le sujet Company (cycle correctif « modules personnels ») : Compatibilite
        // ne le liste plus comme dépendance (voir son manifeste) — l'activer artificiellement pour l'entreprise
        // n'a jamais servi à rien à l'exécution et faisait apparaître à tort ce module personnel dans son menu.
        await moduleRegistry.ActivateForCompanyAsync(company.Id, Spectrometre.Modules.ProfilEntreprise.ServiceCollectionExtensions.Manifest.Code, coreDb);
        await moduleRegistry.ActivateForCompanyAsync(company.Id, Spectrometre.Modules.Compatibilite.ServiceCollectionExtensions.Manifest.Code, coreDb);
        await moduleRegistry.ActivateForCompanyAsync(company.Id, Spectrometre.Modules.PostesRecrutement.ServiceCollectionExtensions.Manifest.Code, coreDb);
        await moduleRegistry.ActivateForCompanyAsync(company.Id, Spectrometre.Modules.Entretien.ServiceCollectionExtensions.Manifest.Code, coreDb);
        await moduleRegistry.ActivateForCompanyAsync(company.Id, Spectrometre.Modules.SuiviEvolutif.ServiceCollectionExtensions.Manifest.Code, coreDb);
        // Analytics n'a pas de schéma propre (voir TenantModules ci-dessus, volontairement absent) — rien à
        // provisionner, seulement l'activation.
        await moduleRegistry.ActivateForCompanyAsync(company.Id, Spectrometre.Modules.Analytics.ServiceCollectionExtensions.Manifest.Code, coreDb);

        return company;
    }

    public async Task DisposeAsync() => await Services.DisposeAsync();
}

[CollectionDefinition("Base de données partagée")]
public sealed class DatabaseCollection : ICollectionFixture<ServiceFixture>;
