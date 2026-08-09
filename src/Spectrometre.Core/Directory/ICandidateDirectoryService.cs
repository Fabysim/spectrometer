namespace Spectrometre.Core.Directory;

/// <summary>Métadonnée structurelle d'un profil candidat — jamais son contenu métier (réponses, CV...), voir la remarque sur <see cref="ICandidateDirectoryService"/>.</summary>
public sealed record CandidateDirectoryEntry(int CandidateProfileId, string UserId, DateTimeOffset CreatedAt);

/// <summary>
/// Énumère tous les profils candidats existants, sans exposer leur contenu métier — le noyau ne peut pas
/// référencer <c>Spectrometre.Modules.ProfilCandidat.Entities.CandidateProfile</c> (un type de module),
/// d'où cette abstraction. Même principe d'inversion de dépendance que <c>ICoachingAccessChecker</c> :
/// définie ici, NoOp par défaut (<see cref="NoOpCandidateDirectoryService"/>), implémentation réelle
/// enregistrée directement par <c>AddProfilCandidatModule</c> (pas de conflit circulaire à résoudre depuis
/// Host ici, contrairement à <c>ICoachingAccessChecker</c>/<c>IProfileChangeRecorder</c> — ProfilCandidat
/// n'a besoin que de ses PROPRES données pour répondre).
/// </summary>
public interface ICandidateDirectoryService
{
    Task<IReadOnlyList<CandidateDirectoryEntry>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Compte les profils. Si <paramref name="matchingUserIds"/> et <paramref name="matchingProfileIds"/>
    /// sont tous deux <c>null</c>, aucun filtre. Sinon : UserId dans la liste OU Id dans la liste
    /// (listes vides = aucun résultat). Pas de recherche texte locale — les candidats n'ont pas de nom affiché.
    /// </summary>
    Task<int> CountAsync(
        IReadOnlyCollection<string>? matchingUserIds = null,
        IReadOnlyCollection<int>? matchingProfileIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>Page ordonnée par <c>CreatedAt</c> croissant puis Id. Même filtre que <see cref="CountAsync"/>.</summary>
    Task<IReadOnlyList<CandidateDirectoryEntry>> GetPageAsync(
        int skip,
        int take,
        IReadOnlyCollection<string>? matchingUserIds = null,
        IReadOnlyCollection<int>? matchingProfileIds = null,
        CancellationToken cancellationToken = default);
}

/// <summary>Filet de sécurité : voir la remarque sur <see cref="ICandidateDirectoryService"/> — tant que ProfilCandidat n'est pas enregistré (ex. tests ne le chargeant pas), la zone Admin voit une liste vide plutôt que de lever.</summary>
public sealed class NoOpCandidateDirectoryService : ICandidateDirectoryService
{
    public Task<IReadOnlyList<CandidateDirectoryEntry>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CandidateDirectoryEntry>>([]);

    public Task<int> CountAsync(
        IReadOnlyCollection<string>? matchingUserIds = null,
        IReadOnlyCollection<int>? matchingProfileIds = null,
        CancellationToken cancellationToken = default) => Task.FromResult(0);

    public Task<IReadOnlyList<CandidateDirectoryEntry>> GetPageAsync(
        int skip,
        int take,
        IReadOnlyCollection<string>? matchingUserIds = null,
        IReadOnlyCollection<int>? matchingProfileIds = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CandidateDirectoryEntry>>([]);
}
