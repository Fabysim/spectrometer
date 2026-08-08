namespace Spectrometre.Core.Directory;

/// <summary>Vue d'ensemble d'un lien de coaching pour le support — statut et dates uniquement, jamais l'anamnèse ni aucun contenu de suivi. <see cref="Statut"/> est le nom en toutes lettres de <c>LienCoachingStatut</c> (le noyau ne peut pas référencer ce type, un module).</summary>
public sealed record CoachingLinkSummary(int Id, string SuiviUserId, string CoachUserId, string Statut, DateTimeOffset CreatedAt, DateTimeOffset? AccepteLe);

/// <summary>Équivalent de <see cref="ICandidateDirectoryService"/> pour les liens de coaching — implémentation réelle enregistrée directement par <c>AddCoachingModule</c>.</summary>
public interface ICoachingLinkOverviewService
{
    Task<IReadOnlyList<CoachingLinkSummary>> GetAllAsync(CancellationToken cancellationToken = default);
}

/// <summary>Filet de sécurité — voir <see cref="NoOpCandidateDirectoryService"/>.</summary>
public sealed class NoOpCoachingLinkOverviewService : ICoachingLinkOverviewService
{
    public Task<IReadOnlyList<CoachingLinkSummary>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CoachingLinkSummary>>([]);
}
