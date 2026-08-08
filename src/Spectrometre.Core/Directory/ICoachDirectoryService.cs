namespace Spectrometre.Core.Directory;

/// <summary>Métadonnée structurelle d'un profil coach — jamais son contenu métier, voir la remarque sur <see cref="ICoachDirectoryService"/>.</summary>
public sealed record CoachDirectoryEntry(int CoachProfileId, string UserId, string NomAffiche, bool VisibleDansAnnuaire, DateTimeOffset CreatedAt);

/// <summary>Équivalent de <see cref="ICandidateDirectoryService"/> côté coach — même raisonnement d'inversion de dépendance, implémentation réelle enregistrée directement par <c>AddProfilCoachModule</c>.</summary>
public interface ICoachDirectoryService
{
    Task<IReadOnlyList<CoachDirectoryEntry>> GetAllAsync(CancellationToken cancellationToken = default);
}

/// <summary>Filet de sécurité — voir <see cref="NoOpCandidateDirectoryService"/>.</summary>
public sealed class NoOpCoachDirectoryService : ICoachDirectoryService
{
    public Task<IReadOnlyList<CoachDirectoryEntry>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CoachDirectoryEntry>>([]);
}
