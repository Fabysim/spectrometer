using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.ProfilCoach.Data;
using Spectrometre.Modules.ProfilCoach.Services;

namespace Spectrometre.Modules.ProfilCoach;

public static class ServiceCollectionExtensions
{
    public static readonly ModuleManifest Manifest = new(
        Code: "ProfilCoach",
        DisplayName: "Profil Coach",
        DisplayNameEn: "Coach Profile",
        Version: "1.0.0",
        RequiredModuleCodes: []);

    /// <summary>Enregistre le module Profil Coach. À appeler depuis <c>Program.cs</c>, après <c>AddSpectrometreCore</c>.</summary>
    public static IServiceCollection AddProfilCoachModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' introuvable.");

        services.AddDbContextFactory<ProfilCoachDbContext>(options =>
        {
            options.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", ProfilCoachDbContext.SchemaName));
        });

        services.AddScoped<ICoachProfileService, CoachProfileService>();
        services.AddScoped<ICoachSubjectResolver, CoachSubjectResolver>();

        return services;
    }
}
