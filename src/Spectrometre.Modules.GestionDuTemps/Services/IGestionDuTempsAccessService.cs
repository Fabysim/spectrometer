namespace Spectrometre.Modules.GestionDuTemps.Services;

/// <summary>
/// Vérifie l'accès EFFECTIF au module Gestion du temps pour l'utilisateur connecté — que ce soit en tant
/// que candidat (abonnement personnel, <c>ModuleActivationSubjectType.Candidate</c>) ou en tant que
/// gestionnaire d'une entreprise abonnée (<c>ModuleActivationSubjectType.Company</c>, n'importe laquelle de
/// celles qu'il gère). Point d'entrée unique utilisé à la fois par la page <c>/gestion-du-temps</c> et par
/// le lien de navigation (<c>MainLayout</c>), pour ne jamais faire diverger les deux vérifications.
/// </summary>
public interface IGestionDuTempsAccessService
{
    Task<bool> HasAccessAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Accès restreint au sujet Candidat uniquement (<c>ModuleActivationSubjectType.Candidate</c>) — jamais
    /// via une entreprise gérée. Utilisé pour tout ce qui dérive de Gestion du temps mais n'a de sens que
    /// pour une personne suivie individuellement (ex. Coaching côté « Mon coach ») : une entreprise n'a pas
    /// de coach, même si elle a activé Gestion du temps pour ses employés. Voir la remarque sur
    /// <see cref="HasAccessAsync"/> — même principe de point d'entrée unique, pour ne jamais faire diverger
    /// menu et route.
    /// </summary>
    Task<bool> HasCandidateAccessAsync(string userId, CancellationToken cancellationToken = default);
}
