namespace Spectrometre.Modules.Analytics.Services;

/// <summary>Une étape du funnel de candidatures, dans l'ordre métier (pas l'ordre alphabétique ni celui d'arrivée des données).</summary>
public sealed record FunnelStatutView(string Statut, string Libelle, int Nombre);

/// <summary>Moyenne d'un axe de compatibilité — <c>null</c> tant qu'aucune candidature de l'entreprise n'a de score calculé sur cet axe.</summary>
public sealed record AxisMoyenneView(string Axe, double? Moyenne);

public sealed record TagFrequenceView(string Tag, int Nombre);

/// <summary>
/// Vue agrégée du tableau de bord Analytics pour l'entreprise active. Toutes les valeurs sont déjà
/// scopées à cette entreprise — voir <see cref="IAnalyticsService.GetDashboardAsync"/>.
/// </summary>
public sealed record AnalyticsDashboardView(
    int PostesOuverts,
    int PostesFermes,
    int TotalCandidatures,
    IReadOnlyList<FunnelStatutView> Funnel,
    int? ScoreMoyenGlobal,
    IReadOnlyList<AxisMoyenneView> MoyennesParAxe,
    IReadOnlyList<TagFrequenceView> TopPointsDeVigilance,
    int CandidatsUniques,
    int CandidatsAvecGrilleComplete,
    double? TauxCompletionGrilleH);

/// <summary>
/// Point d'entrée public du module Analytics — pur agrégateur en lecture sur l'index partagé de
/// recrutement (<c>Spectrometre.Core.Recruitment.IRecruitmentIndexService</c>), jamais un accès direct aux
/// schémas tenant des autres modules. Scopé à l'entreprise active (<c>ITenantContext.ActiveCompanyId</c>),
/// même modèle d'autorisation que Vivier/PostesRecrutement/ProfilEntreprise — n'expose jamais d'agrégat
/// toutes-entreprises-confondues.
/// </summary>
public interface IAnalyticsService
{
    Task<AnalyticsDashboardView> GetDashboardAsync(CancellationToken cancellationToken = default);
}
