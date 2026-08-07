using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Invitations;
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

        // Une SEULE source de vérité pour DbContextOptions<CoreDbContext> : la factory (Singleton). Appeler
        // AUSSI AddDbContext<CoreDbContext> enregistrerait une deuxième fois DbContextOptions<CoreDbContext>
        // (Scoped, cette fois) sous un mécanisme différent (IDbContextOptionsConfiguration) — ce qui a deux
        // conséquences constatées : une dépendance captive (Singleton IDbContextFactory dépendant d'un
        // service Scoped, détectée par la validation de graphe de DI activée en Development) ET une
        // résolution cassée pour les outils de conception EF Core (`dotnet ef migrations add` échoue en
        // essayant de résoudre CoreDbContext depuis le fournisseur racine). D'où l'enregistrement scoped de
        // CoreDbContext ci-dessous via un simple délégué sur la factory, plutôt que via AddDbContext.
        services.AddDbContextFactory<CoreDbContext>(configureCoreDbContext);
        // CoreDbContext reste injectable directement en Scoped (pages Razor, AddEntityFrameworkStores
        // d'Identity juste en dessous, etc.) — une instance par circuit, construite via la factory ci-dessus.
        // Les consommateurs qui doivent éviter tout partage d'instance entre appels concurrents (ex.
        // IProfileChangeRecorder, invoqué depuis des mutations qui peuvent se chevaucher — deux cases
        // cochées coup sur coup — où un CoreDbContext partagé planterait avec "A second operation was
        // started on this context instance…") utilisent directement IDbContextFactory<CoreDbContext>.
        services.AddScoped<CoreDbContext>(sp => sp.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContext());

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
        services.AddScoped<IInvitationService, InvitationService>();

        // Filet de sécurité : voir NoOpProfileChangeRecorder. Program.cs branche l'implémentation réelle
        // de SuiviEvolutif PAR-DESSUS cet enregistrement (la dernière inscription gagne à la résolution).
        services.AddScoped<IProfileChangeRecorder, NoOpProfileChangeRecorder>();

        return services;
    }
}
