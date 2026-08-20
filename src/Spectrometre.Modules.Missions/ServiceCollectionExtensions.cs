using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Missions.Data;
using Spectrometre.Modules.Missions.Services;

namespace Spectrometre.Modules.Missions;

public static class ServiceCollectionExtensions
{
    public static readonly ModuleManifest Manifest = new(
        Code: "ProfilParticulier",
        DisplayName: "Particulier",
        DisplayNameEn: "Individual",
        Version: "1.0.0",
        RequiredModuleCodes: []);

    public static IServiceCollection AddMissionsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' introuvable.");

        services.AddDbContextFactory<MissionsDbContext>(options =>
        {
            options.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", MissionsDbContext.SchemaName));
        });

        services.AddScoped<IParticulierProfileService, ParticulierProfileService>();
        services.AddScoped<IParticulierSubjectResolver, ParticulierSubjectResolver>();
        services.AddScoped<IMissionService, MissionService>();
        return services;
    }
}
