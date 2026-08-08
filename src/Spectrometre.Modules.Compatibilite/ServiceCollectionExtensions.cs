using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Compatibility;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Compatibilite.Data;
using Spectrometre.Modules.Compatibilite.Services;

namespace Spectrometre.Modules.Compatibilite;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// <c>RequiredModuleCodes</c> ne liste que ProfilEntreprise — ProfilCandidat en a été retiré (écart
    /// volontaire par rapport à la déclaration d'origine, antérieure à la généralisation du registre par
    /// sujet). Compatibilite lit les données candidat via <c>ICandidateProfileService</c> (scopé par
    /// <c>CandidateProfileId</c>, jamais par une activation de module côté entreprise) — elle n'a donc jamais
    /// eu besoin que « Profil Candidat » soit marqué actif POUR L'ENTREPRISE elle-même. Cette dépendance ne
    /// faisait que forcer une ligne <c>ModuleActivation</c> artificielle sur le sujet Company, ce qui faisait
    /// apparaître à tort « Profil Candidat » (module personnel, scopé UserId, sans écran entreprise) dans le
    /// menu et le tableau de bord d'une entreprise ayant activé Recrutement. Retirée sans impact sur le
    /// fonctionnement réel de Compatibilite/Vivier/Entretien/Analytics, qui ne consultent jamais cet
    /// indicateur à l'exécution.
    /// </summary>
    public static readonly ModuleManifest Manifest = new(
        Code: "Compatibilite",
        DisplayName: "Moteur de Compatibilité",
        DisplayNameEn: "Compatibility Engine",
        Version: "1.0.0",
        RequiredModuleCodes: ["ProfilEntreprise"]);

    /// <summary>À appeler après <c>AddProfilCandidatModule</c> et <c>AddProfilEntrepriseModule</c> (ICandidateProfileService/données ProfilEntreprise consommées par le service, pas des dépendances de manifeste).</summary>
    public static IServiceCollection AddCompatibiliteModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' introuvable.");

        // Voir le commentaire équivalent dans Spectrometre.Modules.ProfilEntreprise : DbContextFactory
        // requis pour un DbContext tenant-scopé utilisé depuis Blazor Server.
        services.AddDbContextFactory<CompatibiliteDbContext>(options =>
        {
            options.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Compatibilite", "public"));
        });

        // Une seule instance scoped exposée sous les deux contrats (ICompatibiliteService +
        // ICompatibiliteScoreService Core pour ProfilEntreprise sans ProjectReference circulaire).
        services.AddScoped<CompatibiliteService>();
        services.AddScoped<ICompatibiliteService>(sp => sp.GetRequiredService<CompatibiliteService>());
        services.AddScoped<ICompatibiliteScoreService>(sp => sp.GetRequiredService<CompatibiliteService>());

        return services;
    }
}
