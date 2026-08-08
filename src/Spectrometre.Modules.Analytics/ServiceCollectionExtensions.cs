using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Modules;
using Spectrometre.Modules.Analytics.Services;

namespace Spectrometre.Modules.Analytics;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Dépend à la fois de Compatibilite (les scores agrégés n'ont de sens que si le moteur existe) et de
    /// PostesRecrutement (le funnel de candidatures et les postes ouverts/fermés) — même schéma de
    /// dépendance double que <c>Spectrometre.Modules.Vivier</c>, pas un précédent nouveau : un tableau de
    /// bord sans l'un des deux n'a tout simplement aucune donnée de recrutement à agréger.
    /// </summary>
    public static readonly ModuleManifest Manifest = new(
        Code: "Analytics",
        DisplayName: "Analytics / Décideurs",
        DisplayNameEn: "Analytics / Decision-makers",
        Version: "1.0.0",
        RequiredModuleCodes: ["Compatibilite", "Recrutement"]);

    /// <summary>
    /// Aucun DbContext à enregistrer : ce module n'a pas de schéma propre, il lit exclusivement l'index
    /// partagé du noyau (<c>IRecruitmentIndexService</c>, déjà enregistré par <c>AddSpectrometreCore</c>) —
    /// voir le résumé final pour cette décision (pas de nouvelle table, tout est dérivé de l'index).
    /// </summary>
    public static IServiceCollection AddAnalyticsModule(this IServiceCollection services)
    {
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        return services;
    }
}
