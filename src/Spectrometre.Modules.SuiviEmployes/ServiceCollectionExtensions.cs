using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.SuiviEmployes.Data;
using Spectrometre.Modules.SuiviEmployes.Services;

namespace Spectrometre.Modules.SuiviEmployes;

/// <summary>
/// Module Suivi &amp; évaluation des employés — DbContext tenant + services métier.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static readonly ModuleManifest Manifest = new(
        Code: "SuiviEmployes",
        DisplayName: "Suivi & évaluation des employés",
        DisplayNameEn: "Employee follow-up & evaluation",
        Version: "1.0.0",
        RequiredModuleCodes: ["ProfilEntreprise"]);

    public static IServiceCollection AddSuiviEmployesModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' introuvable.");

        services.AddDbContextFactory<SuiviEmployesDbContext>(options =>
        {
            options.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory_SuiviEmployes", "public"));
        });

        services.AddScoped<IAnalyseEmployeIaService, ReplicateAnalyseEmployeIaService>();
        services.AddScoped<ISuiviEmployesService, SuiviEmployesService>();

        return services;
    }
}
