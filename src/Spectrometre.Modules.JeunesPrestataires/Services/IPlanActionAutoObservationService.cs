namespace Spectrometre.Modules.JeunesPrestataires.Services;

public sealed record PlanActionAutoObservationView(
    int? Id,
    int JeuneProfileId,
    string? ObjectifPrincipal,
    string? PremiereAction,
    string? ResponsableSuivi,
    DateOnly? Echeance,
    string? IndicateurReussite,
    DateTimeOffset? UpdatedAt)
{
    public bool EstRempli =>
        !string.IsNullOrWhiteSpace(ObjectifPrincipal)
        || !string.IsNullOrWhiteSpace(PremiereAction)
        || !string.IsNullOrWhiteSpace(ResponsableSuivi)
        || Echeance.HasValue
        || !string.IsNullOrWhiteSpace(IndicateurReussite);
}

public sealed record PlanActionAutoObservationInput(
    string? ObjectifPrincipal,
    string? PremiereAction,
    string? ResponsableSuivi,
    DateOnly? Echeance,
    string? IndicateurReussite);

public interface IPlanActionAutoObservationService
{
    /// <summary>
    /// Coach suiveur uniquement (<c>GetSuiviUserIdSiAutoriseAsync</c>). Vue vide non persistée
    /// s'il n'existe pas encore de plan (comme <c>IGuideEntrevueService.GetOrCreateAsync</c>).
    /// </summary>
    Task<PlanActionAutoObservationView?> GetOrCreateAsync(
        string coachUserId,
        int jeuneProfileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upsert. Pas de notification jeune (voir <c>IAutoObservationService.ValiderSyntheseAsync</c>).
    /// </summary>
    Task<bool> SaveAsync(
        string coachUserId,
        int jeuneProfileId,
        PlanActionAutoObservationInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lecture jeune (son profil) ou coach autorisé. <c>null</c> si non autorisé, ou — pour le jeune —
    /// si le plan n'est pas encore rempli.
    /// </summary>
    Task<PlanActionAutoObservationView?> GetLectureAsync(
        string requestingUserId,
        int jeuneProfileId,
        CancellationToken cancellationToken = default);
}
