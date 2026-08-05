using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Compatibilite;
using Spectrometre.Modules.Compatibilite.Data;
using Spectrometre.Modules.ProfilCandidat;
using Spectrometre.Modules.ProfilCandidat.Data;
using Spectrometre.Modules.ProfilEntreprise;
using Spectrometre.Modules.ProfilEntreprise.Data;
using Microsoft.EntityFrameworkCore;
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
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddProfilCandidatModule(config);
        services.AddProfilEntrepriseModule(config);
        services.AddCompatibiliteModule(config);

        Services = services.BuildServiceProvider();

        // Les migrations de ProfilCandidat (schéma fixe) et le schéma "gabarit" public de
        // ProfilEntreprise/Compatibilite (tenant-scopés) doivent déjà être en place — idempotent.
        await using var candidatDb = await Services.GetRequiredService<IDbContextFactory<ProfilCandidatDbContext>>().CreateDbContextAsync();
        await candidatDb.Database.MigrateAsync();

        await using var entrepriseDb = await Services.GetRequiredService<IDbContextFactory<ProfilEntrepriseDbContext>>().CreateDbContextAsync();
        await entrepriseDb.Database.MigrateAsync();

        await using var compatibiliteDb = await Services.GetRequiredService<IDbContextFactory<CompatibiliteDbContext>>().CreateDbContextAsync();
        await compatibiliteDb.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await Services.DisposeAsync();
}

[CollectionDefinition("Base de données partagée")]
public sealed class DatabaseCollection : ICollectionFixture<ServiceFixture>;
