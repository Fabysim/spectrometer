namespace Spectrometre.Modules.GestionDuTemps.Services;

public sealed record CycleView(int Id, int NumeroCycle, string Statut, DateTimeOffset DemarreLe, DateTimeOffset? ClotureLe);

public sealed record TypeDeTempsView(int Id, string Cle, string Libelle, TimeOnly HeureDebut, TimeOnly HeureFin, string RecurrenceJours, int OrdreAffichage, int? CompanyId);

public sealed record ActiviteView(int Id, int TypeDeTempsId, string TypeLibelle, string TypeCouleur, string Nom, DateOnly DateActivite, TimeOnly HeureDebut, int DureeMinutes, int? CompanyId);

/// <summary>Carte Kanban : statut (3 colonnes) + minuteur — voir <c>KanbanTimer</c> pour <see cref="TempsReelMs"/>/<see cref="EnDepassement"/>.</summary>
public sealed record KanbanCarteView(int ActiviteId, string Nom, string TypeLibelle, string TypeCouleur, int DureeMinutes, int? CompanyId, string Statut, long TempsReelMs, bool EnDepassement);

/// <summary>
/// Point d'entrée public du module Gestion du temps. Toutes les méthodes prennent <c>userId</c> en
/// paramètre explicite (résolu par la page depuis <c>AuthenticationState</c>, même pattern que
/// <c>ICandidateProfileService.GetOrCreateProfileIdAsync</c>) plutôt qu'un tenant ambiant : ce module n'a
/// pas de notion d'entreprise active, son scope d'autorisation est simplement "cet utilisateur".
/// </summary>
public interface IGestionDuTempsService
{
    /// <summary>Cycle actif de l'utilisateur, créé paresseusement (avec les 6 catégories par défaut) au tout premier accès.</summary>
    Task<CycleView> GetOrCreateCycleActifAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clôture le cycle actif et en démarre un nouveau : les types de temps sont RECOPIÉS vers le nouveau
    /// cycle, les activités restent attachées au cycle clôturé (jamais reportées) — voir <see cref="Entities.Cycle"/>.
    /// </summary>
    Task<CycleView> ClotureEtDemarrerNouveauCycleAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Types de temps du cycle ACTIF uniquement — ceux d'un cycle clôturé restent en base mais ne sont plus modifiables.</summary>
    Task<IReadOnlyList<TypeDeTempsView>> GetTypesDeTempsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Crée (id == null) ou modifie un type de temps DU CYCLE ACTIF. Si <paramref name="companyId"/> est
    /// renseigné, doit correspondre à une entreprise que l'utilisateur gère réellement (<c>UserCompanyLink</c>) —
    /// sinon <see cref="InvalidOperationException"/>.
    /// </summary>
    Task UpsertTypeDeTempsAsync(string userId, int? id, string cle, string libelle, TimeOnly heureDebut, TimeOnly heureFin, string recurrenceJours, int ordreAffichage, int? companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rappels du cycle ACTIF. <paramref name="companyId"/> filtre sur une entreprise précise ;
    /// <paramref name="personnelUniquement"/> filtre sur les rappels sans entreprise ; les deux <c>false</c>/<c>null</c>
    /// retournent tout — le filtre est un confort d'affichage, jamais une restriction d'accès (toujours les
    /// rappels de l'utilisateur connecté, quel que soit le filtre).
    /// </summary>
    Task<IReadOnlyList<ActiviteView>> GetActivitesAsync(string userId, int? companyId, bool personnelUniquement, CancellationToken cancellationToken = default);

    /// <summary>Crée un rappel dans le cycle ACTIF (avec son statut Kanban initial "À faire").</summary>
    Task<int> CreateActiviteAsync(string userId, int typeDeTempsId, string nom, DateOnly dateActivite, TimeOnly heureDebut, int dureeMinutes, int? companyId, CancellationToken cancellationToken = default);

    Task UpdateActiviteAsync(string userId, int activiteId, string nom, DateOnly dateActivite, TimeOnly heureDebut, int dureeMinutes, int? companyId, CancellationToken cancellationToken = default);

    Task DeleteActiviteAsync(string userId, int activiteId, CancellationToken cancellationToken = default);

    /// <summary>Toutes les cartes Kanban du cycle ACTIF, triées À faire → En cours → Terminé.</summary>
    Task<IReadOnlyList<KanbanCarteView>> GetKanbanAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Démarre (ou reprend) le minuteur — passe en "En cours".</summary>
    Task MarquerDebutAsync(string userId, int activiteId, CancellationToken cancellationToken = default);

    /// <summary>Met en pause : accumule le temps écoulé, repasse en "À faire".</summary>
    Task MarquerPauseAsync(string userId, int activiteId, CancellationToken cancellationToken = default);

    /// <summary>Finalise le minuteur (accumule l'écoulé si en cours) et passe en "Terminé".</summary>
    Task MarquerTermineAsync(string userId, int activiteId, CancellationToken cancellationToken = default);
}
