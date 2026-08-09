using Spectrometre.Core.Modules;

namespace Spectrometre.Modules.ProfilCandidat.Services;

/// <summary>
/// Implémentation réelle de <see cref="ICandidateSubjectResolver"/> (Core) — un simple relais vers
/// <see cref="ICandidateProfileService.GetOrCreateProfileIdAsync"/>, seul endroit qui sait résoudre un
/// <c>CandidateProfileId</c> à partir d'un <c>UserId</c> Identity. Enregistrée directement par
/// <c>AddProfilCandidatModule</c> (pas via une inversion de dépendance câblée depuis Host comme
/// <c>IProfileChangeRecorder</c>/<c>ICandidatureExistenceChecker</c>) : ProfilCandidat n'a aucune
/// dépendance de manifeste et Core ne référence aucun module, donc ProfilCandidat → Core ne crée jamais le
/// risque de dépendance croisée qui justifiait ce câblage ailleurs.
/// </summary>
public sealed class CandidateSubjectResolver(ICandidateProfileService candidateProfileService) : ICandidateSubjectResolver
{
    public Task<int> GetOrCreateCandidateProfileIdAsync(string userId, CancellationToken cancellationToken = default) =>
        candidateProfileService.GetOrCreateProfileIdAsync(userId, cancellationToken);

    public Task<int?> TryGetCandidateProfileIdAsync(string userId, CancellationToken cancellationToken = default) =>
        candidateProfileService.TryGetProfileIdAsync(userId, cancellationToken);
}
