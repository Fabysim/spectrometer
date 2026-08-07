namespace Spectrometre.Core.Modules;

/// <summary>
/// Abstraction d'inversion de dépendance : les pages de <c>Spectrometre.Modules.GestionDuTemps</c> doivent
/// pouvoir vérifier qu'un coach a un lien actif avec une personne suivie avant d'afficher ses données en
/// lecture seule, mais ne doivent JAMAIS référencer <c>Spectrometre.Modules.Coaching</c> : le manifeste
/// déclare la dépendance dans l'autre sens (Coaching dépend de GestionDuTemps + ProfilCoach), et un module
/// ne dépend jamais de ce qui dépend de lui — même recette que <c>ICandidatureExistenceChecker</c>
/// (Compatibilite/PostesRecrutement) et <c>IProfileChangeRecorder</c> (ProfilCandidat·ProfilEntreprise/SuiviEvolutif).
/// Cette interface vit dans le noyau ; l'implémentation réelle est fournie par Coaching et câblée depuis
/// <c>Spectrometre.Host</c> (le seul projet autorisé à connaître les deux modules à la fois).
/// </summary>
public interface ICoachingAccessChecker
{
    /// <summary>
    /// Retourne <paramref name="suiviUserId"/> TEL QUEL si un lien de coaching actif existe entre les deux
    /// comptes, <c>null</c> sinon (lien en attente/révoqué/refusé/absent, ou implémentation non câblée —
    /// voir <see cref="NoOpCoachingAccessChecker"/> — jamais distingué, jamais une exception). L'appelant
    /// doit utiliser la valeur RETOURNÉE, pas son paramètre, comme UserId à transmettre à
    /// <c>IGestionDuTempsService</c>.
    /// </summary>
    Task<string?> GetSuiviUserIdSiAutoriseAsync(string suiviUserId, string requestingCoachUserId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implémentation par défaut, enregistrée par <c>AddSpectrometreCore</c> — un filet de sécurité si
/// <c>Spectrometre.Host</c> ne branche pas l'implémentation réelle de Coaching (ou si ce module n'a pas de
/// sens pour un déploiement donné) : GestionDuTemps continue de fonctionner normalement pour ses propres
/// utilisateurs, simplement sans consultation possible par un coach. Refuse TOUJOURS (retourne null) —
/// jamais de faux positif silencieux. Voir <c>Spectrometre.Modules.Coaching.Services.CoachingService</c>
/// pour l'implémentation réelle, câblée par-dessus celle-ci depuis <c>Program.cs</c>.
/// </summary>
public sealed class NoOpCoachingAccessChecker : ICoachingAccessChecker
{
    public Task<string?> GetSuiviUserIdSiAutoriseAsync(string suiviUserId, string requestingCoachUserId, CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);
}
