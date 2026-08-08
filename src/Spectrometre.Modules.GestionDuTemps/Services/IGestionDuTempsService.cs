using Spectrometre.Modules.GestionDuTemps.Entities;

namespace Spectrometre.Modules.GestionDuTemps.Services;

public sealed record CycleView(int Id, int NumeroCycle, string Statut, DateTimeOffset DemarreLe, DateTimeOffset? ClotureLe);

/// <summary>Synthèse du cycle actif — <see cref="RecommandationsJson"/>/<see cref="AlertesJson"/> de l'entité déjà désérialisés pour la page.</summary>
public sealed record SyntheseView(
    string ProfilType, int IndiceEquilibre, int NiveauMaturite,
    string? ProfilTexte, string? IndiceCommentaire, string? MaturiteCommentaire,
    IReadOnlyList<RecommandationIa> Recommandations, IReadOnlyList<string> Alertes,
    bool GenereeParIa, DateTimeOffset CalculatedAt,
    /// <summary>Diagnostic non persisté — pourquoi le repli local a été utilisé (clé API, erreur Replicate, profil manquant, JSON invalide).</summary>
    string? AvertissementIa = null);

public sealed record TypeDeTempsView(int Id, string Cle, string Libelle, TimeOnly HeureDebut, TimeOnly HeureFin, string RecurrenceJours, int OrdreAffichage, int? CompanyId);

public sealed record ActiviteView(int Id, int TypeDeTempsId, string TypeCle, string TypeLibelle, string TypeCouleur, string Nom, DateOnly DateActivite, TimeOnly HeureDebut, int DureeMinutes, int? CompanyId);

/// <summary>Carte Kanban : statut (3 colonnes) + minuteur — voir <c>KanbanTimer</c> pour <see cref="TempsReelMs"/>/<see cref="EnDepassement"/>.</summary>
public sealed record KanbanCarteView(
    int ActiviteId,
    int TypeDeTempsId,
    string Nom,
    string TypeLibelle,
    string TypeCouleur,
    int DureeMinutes,
    int? CompanyId,
    string Statut,
    long TempsReelMs,
    bool EnDepassement,
    DateTimeOffset UpdatedAt,
    DateTimeOffset ActiviteCreatedAt);

/// <summary>
/// Point d'entrée public du module Gestion du temps. Toutes les méthodes prennent <c>userId</c> en
/// paramètre explicite (résolu par la page depuis <c>AuthenticationState</c>, même pattern que
/// <c>ICandidateProfileService.GetOrCreateProfileIdAsync</c>) plutôt qu'un tenant ambiant : ce module n'a
/// pas de notion d'entreprise active, son scope d'autorisation est simplement "cet utilisateur".
/// </summary>
public interface IGestionDuTempsService
{
    /// <summary>Cycle actif s'il existe déjà — lecture pure, ne crée rien (Suivi coach / Coaching anamnèse).</summary>
    Task<CycleView?> GetCycleActifAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Cycle actif de l'utilisateur, créé paresseusement (avec les 6 catégories par défaut) au premier écriture ou accès propriétaire explicite.</summary>
    Task<CycleView> GetOrCreateCycleActifAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clôture le cycle actif et en démarre un nouveau : les types de temps sont RECOPIÉS vers le nouveau
    /// cycle, les activités restent attachées au cycle clôturé (jamais reportées) — voir <see cref="Entities.Cycle"/>.
    /// </summary>
    Task<CycleView> ClotureEtDemarrerNouveauCycleAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Types de temps du cycle ACTIF uniquement — lecture pure (liste vide s'il n'y a pas encore de cycle).</summary>
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

    /// <summary>Met à jour un rappel. <paramref name="typeDeTempsId"/> optionnel — si fourni, doit appartenir au cycle actif (édition calendrier Organisation).</summary>
    Task UpdateActiviteAsync(string userId, int activiteId, string nom, DateOnly dateActivite, TimeOnly heureDebut, int dureeMinutes, int? companyId, int? typeDeTempsId = null, CancellationToken cancellationToken = default);

    Task DeleteActiviteAsync(string userId, int activiteId, CancellationToken cancellationToken = default);

    /// <summary>Toutes les cartes Kanban du cycle ACTIF, triées À faire → En cours → Terminé.</summary>
    Task<IReadOnlyList<KanbanCarteView>> GetKanbanAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Démarre (ou reprend) le minuteur — passe en "En cours".</summary>
    Task MarquerDebutAsync(string userId, int activiteId, CancellationToken cancellationToken = default);

    /// <summary>Met en pause : accumule le temps écoulé, repasse en "À faire".</summary>
    Task MarquerPauseAsync(string userId, int activiteId, CancellationToken cancellationToken = default);

    /// <summary>Finalise le minuteur (accumule l'écoulé si en cours) et passe en "Terminé".</summary>
    Task MarquerTermineAsync(string userId, int activiteId, CancellationToken cancellationToken = default);

    // ── Profil psychosocial / réflexion consciente / synthèse (cycle ACTIF) ────
    //
    // ProfilPsychosocial/ReflexionConsciente sont exposés directement en entités (pas de vue dédiée,
    // exception délibérée à la convention du reste de cette interface) : ce sont déjà de purs sacs de champs
    // sans logique ni relation à cacher (comme dans mvp, où GetProfilAsync/SaveProfilAsync manipulent
    // directement GdtProfilPsychosocial) — dupliquer une quarantaine de champs dans un record séparé
    // n'aurait ajouté aucune sécurité, seulement un risque de désynchronisation. L'implémentation ignore
    // TOUJOURS Id/CycleId/UserId fournis par l'appelant et les résout elle-même côté serveur.

    /// <summary>Profil psychosocial du cycle ACTIF, ou <c>null</c> s'il n'a jamais été rempli.</summary>
    Task<ProfilPsychosocial?> GetProfilPsychosocialAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Crée ou met à jour le profil psychosocial du cycle ACTIF. <c>profil.Id</c>/<c>CycleId</c>/<c>UserId</c> sont ignorés et résolus côté serveur.</summary>
    Task SaveProfilPsychosocialAsync(string userId, ProfilPsychosocial profil, CancellationToken cancellationToken = default);

    /// <summary>Réflexion consciente du cycle ACTIF, ou <c>null</c> si elle n'a jamais été remplie.</summary>
    Task<ReflexionConsciente?> GetReflexionConscienteAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Crée ou met à jour la réflexion consciente du cycle ACTIF. <c>reflexion.Id</c>/<c>CycleId</c>/<c>UserId</c> sont ignorés et résolus côté serveur.</summary>
    Task SaveReflexionConscienteAsync(string userId, ReflexionConsciente reflexion, CancellationToken cancellationToken = default);

    /// <summary>Synthèse déjà calculée pour le cycle ACTIF, ou <c>null</c> si jamais générée.</summary>
    Task<SyntheseView?> GetSyntheseAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Génère la synthèse du cycle actif via Replicate (Claude). Si <paramref name="forcerRegeneration"/>
    /// est <c>false</c> et que le hash profil/réflexion est inchangé, retourne le cache. Ne lève jamais si
    /// l'IA est indisponible : retombe sur un texte généré localement (voir <see cref="SyntheseView.GenereeParIa"/>).
    /// </summary>
    Task<SyntheseView> GenererSyntheseAsync(string userId, bool forcerRegeneration = false, CancellationToken cancellationToken = default);
}
