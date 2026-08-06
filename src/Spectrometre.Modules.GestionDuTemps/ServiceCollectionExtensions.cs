using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.GestionDuTemps.Data;
using Spectrometre.Modules.GestionDuTemps.Services;

namespace Spectrometre.Modules.GestionDuTemps;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Aucune dépendance à un autre module (ni ProfilCandidat, ni ProfilEntreprise, ni le reste du domaine
    /// Matching Emploi) : la Gestion du temps est indépendante, utilisable par n'importe quel utilisateur
    /// authentifié. Le rattachement optionnel à une entreprise (voir <c>TypeDeTemps.CompanyId</c>) passe
    /// exclusivement par le noyau (<c>Company</c>/<c>UserCompanyLink</c>), jamais par une référence à
    /// ProfilEntreprise ou un autre module métier.
    /// </summary>
    /// <remarks>
    /// Ce manifeste n'est PAS enregistré dans <c>IModuleRegistry</c> (pas de
    /// <c>moduleRegistry.Register(Manifest)</c> dans Program.cs, pas d'entrée dans
    /// <c>CompanyOnboardingService</c>/<c>TenantSchemaModuleCatalog</c>) : ce registre est intrinsèquement
    /// scopé par entreprise (toutes ses méthodes prennent un <c>companyId</c> — voir
    /// <c>IModuleRegistry.IsActiveAsync</c>), alors que la Gestion du temps n'a aucune notion d'entreprise à
    /// activer/désactiver. Le module est simplement câblé dans Program.cs et disponible pour tout
    /// utilisateur authentifié, comme l'authentification elle-même — gardé ici pour la cohérence structurelle
    /// avec les autres modules (et au cas où un futur cycle voudrait un jour un gating par entreprise).
    /// </remarks>
    public static readonly ModuleManifest Manifest = new(
        Code: "GestionDuTemps",
        DisplayName: "Gestion du temps",
        Version: "1.0.0",
        RequiredModuleCodes: []);

    public static IServiceCollection AddGestionDuTempsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' introuvable.");

        // AddDbContextFactory (jamais AddDbContext) : même raison que partout ailleurs en Blazor Server —
        // une instance UNIQUE partagée pour tout le circuit serait utilisée concurremment par deux
        // gestionnaires d'évènements qui se chevauchent (ex. cocher "Fait" sur deux rappels coup sur coup).
        services.AddDbContextFactory<GestionDuTempsDbContext>(options =>
        {
            options.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", GestionDuTempsDbContext.SchemaName));
        });

        services.AddScoped<IGestionDuTempsService, GestionDuTempsService>();

        return services;
    }
}
