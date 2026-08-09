namespace Spectrometre.Core.Directory;

/// <summary>Vue d'ensemble d'un lien de coaching pour le support — statut et dates uniquement, jamais l'anamnèse ni aucun contenu de suivi. <see cref="Statut"/> est le nom en toutes lettres de <c>LienCoachingStatut</c> (le noyau ne peut pas référencer ce type, un module).</summary>
public sealed record CoachingLinkSummary(int Id, string SuiviUserId, string CoachUserId, string Statut, DateTimeOffset CreatedAt, DateTimeOffset? AccepteLe);

/// <summary>Équivalent de <see cref="ICandidateDirectoryService"/> pour les liens de coaching — implémentation réelle enregistrée directement par <c>AddCoachingModule</c>.</summary>
public interface ICoachingLinkOverviewService
{
    Task<IReadOnlyList<CoachingLinkSummary>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Compte les liens. Si <paramref name="recherche"/> est <c>null</c>, aucun filtre.
    /// Sinon : SuiviUserId/CoachUserId dans <paramref name="matchingUserIds"/> OU Statut contient le terme.
    /// </summary>
    Task<int> CountAsync(
        string? recherche = null,
        IReadOnlyCollection<string>? matchingUserIds = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CoachingLinkSummary>> GetPageAsync(
        int skip,
        int take,
        string? recherche = null,
        IReadOnlyCollection<string>? matchingUserIds = null,
        CancellationToken cancellationToken = default);
}

/// <summary>Filet de sécurité — voir <see cref="NoOpCandidateDirectoryService"/>.</summary>
public sealed class NoOpCoachingLinkOverviewService : ICoachingLinkOverviewService
{
    public Task<IReadOnlyList<CoachingLinkSummary>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CoachingLinkSummary>>([]);

    public Task<int> CountAsync(
        string? recherche = null,
        IReadOnlyCollection<string>? matchingUserIds = null,
        CancellationToken cancellationToken = default) => Task.FromResult(0);

    public Task<IReadOnlyList<CoachingLinkSummary>> GetPageAsync(
        int skip,
        int take,
        string? recherche = null,
        IReadOnlyCollection<string>? matchingUserIds = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CoachingLinkSummary>>([]);
}
