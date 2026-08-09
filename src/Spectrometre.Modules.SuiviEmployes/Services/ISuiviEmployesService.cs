namespace Spectrometre.Modules.SuiviEmployes.Services;

public interface ISuiviEmployesService
{
    Task<EmployeContexte?> GetContexteAsync(int userCompanyLinkId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EmployeRattachementOption>> ListRattachementsEmployeAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<ProfilProfessionnelPageData?> GetProfilProfessionnelAsync(
        int userCompanyLinkId,
        CancellationToken cancellationToken = default);

    Task ValiderProfilInitialAsync(
        int userCompanyLinkId,
        IReadOnlyList<ScoreSaisieDto> scores,
        CancellationToken cancellationToken = default);

    Task<int> GetNextDaySequenceAsync(
        int userCompanyLinkId,
        DateOnly evaluationDate,
        CancellationToken cancellationToken = default);

    Task SaveScoresBlocAsync(
        int userCompanyLinkId,
        DateOnly evaluationDate,
        int daySequence,
        IReadOnlyList<ScoreSaisieDto> scores,
        CancellationToken cancellationToken = default);

    Task CloturerBlocAsync(
        int userCompanyLinkId,
        DateOnly evaluationDate,
        int daySequence,
        CancellationToken cancellationToken = default);

    Task<EvaluationObjectifsView> GetOrCreateEvaluationObjectifsCouranteAsync(
        int userCompanyLinkId,
        string? evaluateurUserId,
        CancellationToken cancellationToken = default);

    Task SaveObjectifsAsync(
        int userCompanyLinkId,
        IReadOnlyList<ObjectifSaisieDto> objectifs,
        bool archiver,
        string? evaluateurUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvaluationObjectifsView>> GetArchivesObjectifsAsync(
        int userCompanyLinkId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PointCourbe>> GetEvolutionNotesObjectifsAsync(
        int userCompanyLinkId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SerieCritereCourbe>> GetEvolutionCriteresAsync(
        int userCompanyLinkId,
        CancellationToken cancellationToken = default);

    Task<AnalyseIaEmployeView?> GetAnalyseIaAsync(
        int userCompanyLinkId,
        CancellationToken cancellationToken = default);

    Task<AnalyseIaEmployeView> GenererAnalyseEmployeAsync(
        int userCompanyLinkId,
        bool forcerRegeneration = false,
        CancellationToken cancellationToken = default);
}
