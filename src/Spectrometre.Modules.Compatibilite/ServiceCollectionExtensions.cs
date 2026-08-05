using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Compatibilite.Data;
using Spectrometre.Modules.Compatibilite.Services;

namespace Spectrometre.Modules.Compatibilite;

public static class ServiceCollectionExtensions
{
    public static readonly ModuleManifest Manifest = new(
        Code: "Compatibilite",
        DisplayName: "Moteur de Compatibilité",
        Version: "1.0.0",
        RequiredModuleCodes: ["ProfilCandidat", "ProfilEntreprise"]);

    /// <summary>À appeler après <c>AddProfilCandidatModule</c> et <c>AddProfilEntrepriseModule</c> (dépendances du manifeste).</summary>
    public static IServiceCollection AddCompatibiliteModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' introuvable.");

        // Voir le commentaire équivalent dans Spectrometre.Modules.ProfilEntreprise : DbContextFactory
        // requis pour un DbContext tenant-scopé utilisé depuis Blazor Server.
        services.AddDbContextFactory<CompatibiliteDbContext>(options =>
        {
            options.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Compatibilite", "public"));
        });

        services.AddScoped<ICompatibiliteService, CompatibiliteService>();

        return services;
    }
}
