using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.ProfilEntreprise.Data;
using Spectrometre.Modules.ProfilEntreprise.Services;

namespace Spectrometre.Modules.ProfilEntreprise;

public static class ServiceCollectionExtensions
{
    public static readonly ModuleManifest Manifest = new(
        Code: "ProfilEntreprise",
        DisplayName: "Profil Entreprise",
        Version: "1.0.0",
        RequiredModuleCodes: []);

    public static IServiceCollection AddProfilEntrepriseModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' introuvable.");

        // AddDbContextFactory (pas AddDbContext) : en Blazor Server, un DbContext scoped vit pour tout le
        // circuit (pas par page), donc son modèle EF (construit avec le schéma tenant courant à la première
        // utilisation) resterait figé même après un changement d'entreprise active dans la même session.
        // Chaque opération doit donc créer une instance fraîche via la factory, à jour du schéma courant.
        services.AddDbContextFactory<ProfilEntrepriseDbContext>(options =>
        {
            options.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
            // Tenant-scopé : ces migrations ne servent qu'au schéma "gabarit" (public) en design-time /
            // dev local — le provisioning par tenant passe par ITenantSchemaProvisioner, pas Database.Migrate().
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_ProfilEntreprise", "public"));
        });

        services.AddScoped<ICompanyProfileService, CompanyProfileService>();

        return services;
    }
}
