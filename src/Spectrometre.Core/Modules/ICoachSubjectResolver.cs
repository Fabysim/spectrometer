namespace Spectrometre.Core.Modules;

/// <summary>
/// Résout l'identifiant coach (le <c>SubjectId</c> à utiliser avec
/// <see cref="ModuleActivationSubjectType.Coach"/>) à partir de l'utilisateur Identity connecté.
/// Même inversion de dépendance que <see cref="ICandidateSubjectResolver"/> : le noyau définit le contrat,
/// <c>Spectrometre.Modules.ProfilCoach</c> l'implémente (il est seul à connaître <c>CoachProfileId</c>) —
/// le module Coaching peut ainsi vérifier l'activation « coach » sans référence de projet vers ProfilCoach
/// pour ce seul besoin (il en a néanmoins une, directe, pour consommer <c>ICoachProfileService</c> —
/// voir sa remarque).
/// </summary>
public interface ICoachSubjectResolver
{
    Task<int> GetOrCreateCoachProfileIdAsync(string userId, CancellationToken cancellationToken = default);
}
