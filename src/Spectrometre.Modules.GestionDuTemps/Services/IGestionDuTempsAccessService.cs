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
}
