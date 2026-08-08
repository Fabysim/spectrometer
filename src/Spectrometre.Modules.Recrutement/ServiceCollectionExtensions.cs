using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Recruitment;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Recrutement.Data;
using Spectrometre.Modules.Recrutement.Services;

namespace Spectrometre.Modules.Recrutement;

/// <summary>
/// Module Recrutement (ex-PostesRecrutement) : guides 2ème entrevue et analyses IA.
/// Les postes / candidatures / critères vivent dans ProfilEntreprise.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static readonly ModuleManifest Manifest = new(
        Code: "Recrutement",
        DisplayName: "Recrutement",
        DisplayNameEn: "Recruitment",
        Version: "1.0.0",
        RequiredModuleCodes: ["ProfilEntreprise"]);

    public static IServiceCollection AddRecrutementModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' introuvable.");

        // Conservé : __EFMigrationsHistory_PostesRecrutement pour ne pas casser les historiques locaux existants.
        services.AddDbContextFactory<RecrutementDbContext>(options =>
        {
            options.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_PostesRecrutement", "public"));
        });

        services.AddScoped<IAnalysePosteIaService, ReplicateAnalysePosteIaService>();
        services.AddScoped<IAnalysePdfService, AnalysePdfService>();
        services.AddScoped<RecrutementEntretienService>();
        services.AddScoped<IRecrutementEntretienService>(sp => sp.GetRequiredService<RecrutementEntretienService>());
        services.AddScoped<IRecrutementEntretienCleanup>(sp => sp.GetRequiredService<RecrutementEntretienService>());

        return services;
    }
}
