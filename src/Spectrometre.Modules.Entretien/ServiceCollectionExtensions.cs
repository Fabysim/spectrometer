using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Entretien.Data;
using Spectrometre.Modules.Entretien.Services;

namespace Spectrometre.Modules.Entretien;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Dépendance manifeste UNIQUE : Compatibilite (un résultat de compatibilité est le seul intrant dont
    /// ce module a besoin). Pas de dépendance vers PostesRecrutement ni Vivier — l'intégration UI se fait
    /// par simple lien de navigation depuis ces modules, jamais par référence de service croisée.
    /// </summary>
    public static readonly ModuleManifest Manifest = new(
        Code: "Entretien",
        DisplayName: "Préparation d'entretien",
        DisplayNameEn: "Interview Preparation",
        Version: "1.0.0",
        RequiredModuleCodes: ["Compatibilite"]);

    public static IServiceCollection AddEntretienModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' introuvable.");

        // Tenant-scopé (voir le commentaire sur EntretienDbContext) : IDbContextFactory, pas AddDbContext —
        // même raison que ProfilEntreprise/Compatibilite/PostesRecrutement (Blazor Server + schéma variable).
        services.AddDbContextFactory<EntretienDbContext>(options =>
        {
            options.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Entretien", "public"));
        });

        // Catalogue partagé (public) — séparé du DbContext tenant pour ne pas recopier les tables
        // de questions dans chaque schéma co_* via ITenantSchemaProvisioner.
        services.AddDbContextFactory<EntretienCatalogDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_EntretienCatalog", "public"));
        });

        services.AddScoped<IEntretienService, EntretienService>();
        services.AddScoped<IBibliothequeQuestionsService, BibliothequeQuestionsService>();

        return services;
    }
}
