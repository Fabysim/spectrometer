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
    /// Ce manifeste EST maintenant enregistré dans <c>IModuleRegistry</c> (<c>moduleRegistry.Register</c>
    /// dans Program.cs), pour les DEUX types de sujet — <c>ModuleActivationSubjectType.Company</c> ET
    /// <c>Candidate</c> — depuis que le registre a cessé d'être couplé à la seule notion d'entreprise (voir
    /// <c>ModuleActivationSubjectType</c>). Contrairement au domaine Matching Emploi, il n'est PAS activé
    /// par défaut pour toute nouvelle entreprise (absent de la boucle dans
    /// <c>CompanyOnboardingService.CreateCompanyAsync</c>) : c'est un module vendu indépendamment (voir le
    /// plan <c>PlanCodes.StandardPlusTemps</c>), pas groupé avec le reste. Toujours absent de
    /// <c>TenantSchemaModuleCatalog</c> (aucun schéma tenant propre, voir <c>AddGestionDuTempsModule</c>).
    /// L'accès effectif (page + lien de navigation) passe par <see cref="Services.IGestionDuTempsAccessService"/>.
    /// </remarks>
    public static readonly ModuleManifest Manifest = new(
        Code: "GestionDuTemps",
        DisplayName: "Gestion du temps",
        DisplayNameEn: "Time Management",
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
        services.AddScoped<IGestionDuTempsAccessService, GestionDuTempsAccessService>();

        // AddHttpClient() est idempotent (ajoute IHttpClientFactory s'il n'est pas déjà enregistré) — sûr à
        // appeler ici même si Host ne l'a pas fait ailleurs, pour que ce module reste autonome.
        services.AddHttpClient();
        // Implémentation réelle par défaut (appel Replicate/Claude) ; les tests substituent cette
        // inscription par une factice (voir FakeAiSynthesisService), jamais de vrai appel réseau en test.
        services.AddScoped<IAiSynthesisService, ReplicateAiSynthesisService>();

        return services;
    }
}
