using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Tenancy;

namespace Spectrometre.Core;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Enregistre le noyau : identité, tenants/entreprises, registre de modules. À appeler une seule fois
    /// depuis <c>Program.cs</c>, avant les <c>AddXxxModule()</c> de chaque module.
    /// </summary>
    public static IServiceCollection AddSpectrometreCore(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' introuvable.");

        services.AddDbContext<CoreDbContext>(options =>
        {
            options.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
            // Table d'historique des migrations dédiée : par défaut EF Core la place dans "public" pour
            // TOUS les DbContext (HasDefaultSchema ne s'applique pas à __EFMigrationsHistory), ce qui les
            // fait tous partager la même table malgré des schémas différents. Chaque module fixe la sienne.
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "core"));
        });

        services.AddIdentityCore<ApplicationUser>(o => o.SignIn.RequireConfirmedAccount = true)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<CoreDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddScoped<ITenantContext, TenantContext>();
        services.AddSingleton<ITenantSchemaNameGenerator, TenantSchemaNameGenerator>();
        services.AddScoped<ICompanyProvisioningService, CompanyProvisioningService>();
        services.AddScoped<ITenantSchemaProvisioner, TenantSchemaProvisioner>();
        services.AddSingleton<IModuleRegistry, ModuleRegistry>();

        return services;
    }
}
