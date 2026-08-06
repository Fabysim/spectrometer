using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Modules;
using Spectrometre.Modules.Vivier.Services;

namespace Spectrometre.Modules.Vivier;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Dépend à la fois de Compatibilite (le score n'a de sens que si le moteur existe) et de
    /// PostesRecrutement (le Vivier n'est qu'un filtre sur les candidatures déjà reçues par ce module —
    /// voir la contrainte de confidentialité dans <see cref="Services.VivierService"/>) : les deux sont
    /// des dépendances DURES au manifeste, contrairement à l'intégration "molle" (vérifiée à l'exécution)
    /// entre PostesRecrutement et Compatibilite — ici, un Vivier sans l'un des deux n'a tout simplement
    /// aucune donnée à afficher, ça n'a pas de sens de l'activer seul.
    /// </summary>
    public static readonly ModuleManifest Manifest = new(
        Code: "Vivier",
        DisplayName: "Vivier de candidats",
        DisplayNameEn: "Talent Pool",
        Version: "1.0.0",
        RequiredModuleCodes: ["Compatibilite", "PostesRecrutement"]);

    /// <summary>
    /// Aucun DbContext à enregistrer : ce module n'a pas de schéma propre, il lit exclusivement l'index
    /// partagé du noyau (<c>IRecruitmentIndexService</c>, déjà enregistré par <c>AddSpectrometreCore</c>).
    /// </summary>
    public static IServiceCollection AddVivierModule(this IServiceCollection services)
    {
        services.AddScoped<IVivierService, VivierService>();
        return services;
    }
}
