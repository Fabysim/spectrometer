using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.ProfilCandidat.Data;
using Spectrometre.Modules.ProfilCandidat.Services;

namespace Spectrometre.Modules.ProfilCandidat;

public static class ServiceCollectionExtensions
{
    public static readonly ModuleManifest Manifest = new(
        Code: "ProfilCandidat",
        DisplayName: "Profil Candidat",
        Version: "1.0.0",
        RequiredModuleCodes: []);

    /// <summary>Enregistre le module Profil Candidat. À appeler depuis <c>Program.cs</c>, après <c>AddSpectrometreCore</c>.</summary>
    public static IServiceCollection AddProfilCandidatModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' introuvable.");

        services.AddDbContext<ProfilCandidatDbContext>(options =>
        {
            options.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", ProfilCandidatDbContext.SchemaName));
        });

        services.AddScoped<ICandidateProfileService, CandidateProfileService>();

        // L'inscription au registre se fait explicitement dans Program.cs (moduleRegistry.Register(Manifest))
        // une fois l'IServiceCollection construite — impossible de résoudre IModuleRegistry ici.
        return services;
    }
}
