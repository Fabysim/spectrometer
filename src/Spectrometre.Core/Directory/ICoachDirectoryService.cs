namespace Spectrometre.Core.Directory;

/// <summary>Métadonnée structurelle d'un profil coach — jamais son contenu métier, voir la remarque sur <see cref="ICoachDirectoryService"/>.</summary>
public sealed record CoachDirectoryEntry(int CoachProfileId, string UserId, string NomAffiche, bool VisibleDansAnnuaire, DateTimeOffset CreatedAt);

/// <summary>Équivalent de <see cref="ICandidateDirectoryService"/> côté coach — même raisonnement d'inversion de dépendance, implémentation réelle enregistrée directement par <c>AddProfilCoachModule</c>.</summary>
public interface ICoachDirectoryService
{
    Task<IReadOnlyList<CoachDirectoryEntry>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Compte les profils. Si <paramref name="recherche"/>, <paramref name="matchingUserIds"/> et
    /// <paramref name="matchingProfileIds"/> sont tous <c>null</c>, aucun filtre. Sinon :
    /// NomAffiche contient le terme (insensible à la casse) OU UserId dans matchingUserIds OU Id dans matchingProfileIds.
    /// </summary>
    Task<int> CountAsync(
        string? recherche = null,
        IReadOnlyCollection<string>? matchingUserIds = null,
        IReadOnlyCollection<int>? matchingProfileIds = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CoachDirectoryEntry>> GetPageAsync(
        int skip,
        int take,
        string? recherche = null,
        IReadOnlyCollection<string>? matchingUserIds = null,
        IReadOnlyCollection<int>? matchingProfileIds = null,
        CancellationToken cancellationToken = default);
}

/// <summary>Filet de sécurité — voir <see cref="NoOpCandidateDirectoryService"/>.</summary>
public sealed class NoOpCoachDirectoryService : ICoachDirectoryService
{
    public Task<IReadOnlyList<CoachDirectoryEntry>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CoachDirectoryEntry>>([]);

    public Task<int> CountAsync(
        string? recherche = null,
        IReadOnlyCollection<string>? matchingUserIds = null,
        IReadOnlyCollection<int>? matchingProfileIds = null,
        CancellationToken cancellationToken = default) => Task.FromResult(0);

    public Task<IReadOnlyList<CoachDirectoryEntry>> GetPageAsync(
        int skip,
        int take,
        string? recherche = null,
        IReadOnlyCollection<string>? matchingUserIds = null,
        IReadOnlyCollection<int>? matchingProfileIds = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CoachDirectoryEntry>>([]);
}
