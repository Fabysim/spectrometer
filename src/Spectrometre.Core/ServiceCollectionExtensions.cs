using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Recruitment;
using Spectrometre.Core.Suivi;
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

        Action<DbContextOptionsBuilder> configureCoreDbContext = options =>
        {
            options.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
            // Table d'historique des migrations dédiée : par défaut EF Core la place dans "public" pour
            // TOUS les DbContext (HasDefaultSchema ne s'applique pas à __EFMigrationsHistory), ce qui les
            // fait tous partager la même table malgré des schémas différents. Chaque module fixe la sienne.
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "core"));
        };

        services.AddDbContext<CoreDbContext>(configureCoreDbContext);
        // En PLUS de l'injection directe ci-dessus (utilisée partout ailleurs — pages Razor, etc., où un
        // seul CoreDbContext par circuit suffit) : une factory pour les consommateurs qui doivent créer une
        // instance fraîche à chaque appel plutôt que de partager celle du circuit — ex. IProfileChangeRecorder,
        // appelé depuis des mutations qui peuvent se chevaucher (deux cases cochées coup sur coup), où un
        // CoreDbContext scoped partagé planterait ("A second operation was started on this context
        // instance…"), même raison que pour tous les DbContext tenant-scopés de la solution.
        services.AddDbContextFactory<CoreDbContext>(configureCoreDbContext);

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
        services.AddScoped<IRecruitmentIndexService, RecruitmentIndexService>();

        // Filet de sécurité : voir NoOpProfileChangeRecorder. Program.cs branche l'implémentation réelle
        // de SuiviEvolutif PAR-DESSUS cet enregistrement (la dernière inscription gagne à la résolution).
        services.AddScoped<IProfileChangeRecorder, NoOpProfileChangeRecorder>();

        return services;
    }
}
