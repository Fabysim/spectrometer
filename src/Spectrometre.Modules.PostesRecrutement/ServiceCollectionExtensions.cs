using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.PostesRecrutement.Data;
using Spectrometre.Modules.PostesRecrutement.Services;

namespace Spectrometre.Modules.PostesRecrutement;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Ne dépend que de ProfilEntreprise au manifeste (un poste appartient à une entreprise). L'intégration
    /// avec Compatibilite est volontairement absente d'ici : elle est vérifiée à l'exécution via
    /// <c>IModuleRegistry.IsActiveAsync</c> dans <c>PosteService</c>, pas via une dépendance d'activation —
    /// le module reste utilisable même si Compatibilite n'est pas activé pour un tenant donné.
    /// </summary>
    public static readonly ModuleManifest Manifest = new(
        Code: "PostesRecrutement",
        DisplayName: "Postes & Recrutement",
        DisplayNameEn: "Job Postings & Recruitment",
        Version: "1.0.0",
        RequiredModuleCodes: ["ProfilEntreprise"]);

    public static IServiceCollection AddPostesRecrutementModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' introuvable.");

        services.AddDbContextFactory<PostesRecrutementDbContext>(options =>
        {
            options.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_PostesRecrutement", "public"));
        });

        services.AddScoped<IPosteService, PosteService>();

        return services;
    }
}
