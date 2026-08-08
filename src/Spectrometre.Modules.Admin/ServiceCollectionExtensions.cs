using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Admin.Data;
using Spectrometre.Modules.Admin.Services;

namespace Spectrometre.Modules.Admin;

/// <summary>
/// Contrairement à tous les autres modules, Admin n'a NI manifeste NI activation propre : ce n'est pas un
/// sujet du registre d'activation généralisé (voir <c>ModuleActivationSubjectType</c>) mais une zone
/// transverse protégée par un rôle ASP.NET Identity (<c>PlatformRoles.Admin</c>) — aucun module ne doit
/// jamais avoir besoin de tester si "Admin est actif", donc aucune ligne à ajouter à
/// <c>IModuleRegistry</c>/<c>Program.cs</c> au-delà de cet appel de DI et de l'assembly Razor additionnelle.
/// Possède depuis ce cycle un schéma fixe propre (<c>admin</c>, voir <see cref="AdminDbContext"/>) —
/// uniquement pour son propre journal d'audit, jamais pour dupliquer une donnée métier d'un autre module.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAdminModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' introuvable.");

        services.AddDbContextFactory<AdminDbContext>(options =>
        {
            options.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", AdminDbContext.SchemaName));
        });

        services.AddScoped<IAdminService, AdminService>();
        return services;
    }
}
