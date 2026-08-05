using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Data;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Recruitment;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Compatibilite;
using Spectrometre.Modules.Compatibilite.Data;
using Spectrometre.Modules.Entretien;
using Spectrometre.Modules.Entretien.Data;
using Spectrometre.Modules.PostesRecrutement;
using Spectrometre.Modules.PostesRecrutement.Data;
using Spectrometre.Modules.PostesRecrutement.Services;
using Spectrometre.Modules.ProfilCandidat;
using Spectrometre.Modules.ProfilCandidat.Data;
using Spectrometre.Modules.ProfilEntreprise;
using Spectrometre.Modules.ProfilEntreprise.Data;
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

        services.AddDbContext<CoreDbContext>(options =>
        {
            options.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
            options.UseNpgsql(ConnectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "core"));
        });
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

        // Même câblage que Spectrometre.Host.Program : l'implémentation réelle est fournie par
        // PostesRecrutement mais enregistrée ici (pas depuis Compatibilite).
        services.AddScoped<ICandidatureExistenceChecker, CandidatureExistenceChecker>();

        Services = services.BuildServiceProvider();

        using (var scope = Services.CreateScope())
        {
            var moduleRegistry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
            moduleRegistry.Register(Spectrometre.Modules.ProfilCandidat.ServiceCollectionExtensions.Manifest);
            moduleRegistry.Register(Spectrometre.Modules.ProfilEntreprise.ServiceCollectionExtensions.Manifest);
            moduleRegistry.Register(Spectrometre.Modules.Compatibilite.ServiceCollectionExtensions.Manifest);
            moduleRegistry.Register(Spectrometre.Modules.PostesRecrutement.ServiceCollectionExtensions.Manifest);
            moduleRegistry.Register(Spectrometre.Modules.Entretien.ServiceCollectionExtensions.Manifest);

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
    }

    /// <summary>
    /// Crée une entreprise de test complète : schéma provisionné, propriétaire lié (<c>UserCompanyLink</c>),
    /// et modules ProfilEntreprise + PostesRecrutement activés — le strict nécessaire pour que
    /// <c>CandidatureExistenceChecker</c> puisse réellement trouver une candidature dans ce tenant.
    /// </summary>
    public async Task<Company> CreateCompanyAsync(string name, string ownerUserId)
    {
        using var scope = Services.CreateScope();
        var provisioning = scope.ServiceProvider.GetRequiredService<ICompanyProvisioningService>();
        var moduleRegistry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
        var provisioner = scope.ServiceProvider.GetRequiredService<ITenantSchemaProvisioner>();
        var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var company = await provisioning.CreateCompanyAsync(name, ownerUserId, coreDb);

        await using (var entrepriseDb = await Services.GetRequiredService<IDbContextFactory<ProfilEntrepriseDbContext>>().CreateDbContextAsync())
            await provisioner.ApplyModuleSchemaAsync(entrepriseDb, "public", company.SchemaName);

        await using (var compatibiliteDb = await Services.GetRequiredService<IDbContextFactory<CompatibiliteDbContext>>().CreateDbContextAsync())
            await provisioner.ApplyModuleSchemaAsync(compatibiliteDb, "public", company.SchemaName);

        await using (var postesDb = await Services.GetRequiredService<IDbContextFactory<PostesRecrutementDbContext>>().CreateDbContextAsync())
            await provisioner.ApplyModuleSchemaAsync(postesDb, "public", company.SchemaName);

        await using (var entretienDb = await Services.GetRequiredService<IDbContextFactory<EntretienDbContext>>().CreateDbContextAsync())
            await provisioner.ApplyModuleSchemaAsync(entretienDb, "public", company.SchemaName);

        // Ordre imposé par les dépendances déclarées aux manifestes (Compatibilite requiert ProfilCandidat
        // + ProfilEntreprise ; PostesRecrutement requiert ProfilEntreprise ; Entretien requiert Compatibilite).
        await moduleRegistry.ActivateForCompanyAsync(company.Id, Spectrometre.Modules.ProfilCandidat.ServiceCollectionExtensions.Manifest.Code, coreDb);
        await moduleRegistry.ActivateForCompanyAsync(company.Id, Spectrometre.Modules.ProfilEntreprise.ServiceCollectionExtensions.Manifest.Code, coreDb);
        await moduleRegistry.ActivateForCompanyAsync(company.Id, Spectrometre.Modules.Compatibilite.ServiceCollectionExtensions.Manifest.Code, coreDb);
        await moduleRegistry.ActivateForCompanyAsync(company.Id, Spectrometre.Modules.PostesRecrutement.ServiceCollectionExtensions.Manifest.Code, coreDb);
        await moduleRegistry.ActivateForCompanyAsync(company.Id, Spectrometre.Modules.Entretien.ServiceCollectionExtensions.Manifest.Code, coreDb);

        return company;
    }

    public async Task DisposeAsync() => await Services.DisposeAsync();
}

[CollectionDefinition("Base de données partagée")]
public sealed class DatabaseCollection : ICollectionFixture<ServiceFixture>;
