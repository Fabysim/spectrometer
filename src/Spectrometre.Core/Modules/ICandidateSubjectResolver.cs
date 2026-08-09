namespace Spectrometre.Core.Modules;

/// <summary>
/// Résout l'identifiant candidat (le <c>SubjectId</c> à utiliser avec
/// <see cref="ModuleActivationSubjectType.Candidate"/>) à partir de l'utilisateur Identity connecté.
/// Inversion de dépendance, même principe que <c>ICandidatureExistenceChecker</c>/<c>IProfileChangeRecorder</c> :
/// le noyau définit le contrat, <c>Spectrometre.Modules.ProfilCandidat</c> l'implémente (il est seul à
/// connaître <c>CandidateProfileId</c>) — un module comme GestionDuTemps peut ainsi vérifier l'activation
/// « candidat » sans obtenir de référence de projet vers ProfilCandidat.
/// </summary>
public interface ICandidateSubjectResolver
{
    Task<int> GetOrCreateCandidateProfileIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Profil candidat s'il existe déjà — <c>null</c> sinon, sans le créer.</summary>
    Task<int?> TryGetCandidateProfileIdAsync(string userId, CancellationToken cancellationToken = default);
}
