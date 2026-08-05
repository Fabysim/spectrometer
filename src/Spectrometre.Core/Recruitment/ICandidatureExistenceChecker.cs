namespace Spectrometre.Core.Recruitment;

/// <summary>
/// Abstraction d'inversion de dépendance : <c>Spectrometre.Modules.Compatibilite</c> a besoin de savoir
/// si une candidature réelle existe entre un candidat et une entreprise (pour restreindre l'accès aux
/// résultats de compatibilité — voir <c>ResultatCompatibilite.razor</c>), mais ne doit JAMAIS référencer
/// <c>Spectrometre.Modules.PostesRecrutement</c> : le manifeste déclare la dépendance dans l'autre sens
/// (Vivier dépend de Compatibilite + PostesRecrutement), et un module ne doit jamais dépendre de ce qui
/// dépend de lui. Cette interface vit dans le noyau, consommée par Compatibilite via injection de
/// dépendance ; l'implémentation réelle est fournie par PostesRecrutement et câblée depuis
/// <c>Spectrometre.Host</c> (le seul projet autorisé à connaître les deux modules à la fois).
/// </summary>
public interface ICandidatureExistenceChecker
{
    /// <summary>
    /// Vrai si ce candidat a réellement postulé à au moins un poste de cette entreprise. Comportement par
    /// défaut si le module PostesRecrutement n'est pas activé pour cette entreprise (ou si l'implémentation
    /// n'est pas câblée) : documenté par l'implémentation, mais doit toujours être le choix le plus sûr
    /// (aucune candidature ne peut être prouvée ⇒ ne jamais accorder l'accès sur cette seule base).
    /// </summary>
    Task<bool> ExisteCandidatureReelleAsync(int candidateProfileId, int companyId, CancellationToken cancellationToken = default);
}
