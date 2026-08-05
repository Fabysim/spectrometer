using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.SuiviEvolutif.Data;
using Spectrometre.Modules.SuiviEvolutif.Services;

namespace Spectrometre.Modules.SuiviEvolutif;

public static class ServiceCollectionExtensions
{
    public static readonly ModuleManifest Manifest = new(
        Code: "SuiviEvolutif",
        DisplayName: "Suivi évolutif",
        Version: "1.0.0",
        RequiredModuleCodes: ["ProfilCandidat", "ProfilEntreprise"]);

    public static IServiceCollection AddSuiviEvolutifModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' introuvable.");

        // Schéma fixe (voir SuiviEvolutifCandidatDbContext) : IDbContextFactory quand même, pour éviter
        // tout usage concurrent d'un même DbContext partagé par circuit Blazor Server (même raison que
        // ProfilCandidatDbContext).
        services.AddDbContextFactory<SuiviEvolutifCandidatDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", SuiviEvolutifCandidatDbContext.SchemaName));
        });

        // Tenant-scopé (voir SuiviEvolutifEntrepriseDbContext).
        services.AddDbContextFactory<SuiviEvolutifEntrepriseDbContext>(options =>
        {
            options.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_SuiviEvolutifEntreprise", "public"));
        });

        services.AddScoped<ISuiviEvolutifService, SuiviEvolutifService>();

        // L'enregistrement de IProfileChangeRecorder (implémentation réelle, par-dessus le no-op de Core)
        // se fait depuis Program.cs, pas ici — voir le commentaire sur ProfileChangeRecorder.

        return services;
    }
}
