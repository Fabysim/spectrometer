using Spectrometre.Modules.Missions.Entities;

namespace Spectrometre.Modules.Missions.Services;

public interface IMissionService
{
    Task<int?> PublierMissionAsync(string particulierUserId, PublierMissionInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// File partagée : tout coach authentifié (profil coach ou liens de suivi), pas de rattachement
    /// coach↔particulier — les particuliers n'ont pas de coach dédié dans le modèle actuel.
    /// </summary>
    Task<IReadOnlyList<MissionDetailView>> GetMissionsEnAttenteModerationAsync(
        string coachUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Passe <c>EnAttenteModeration</c> → <c>Disponible</c>. Tout coach authentifié.</summary>
    Task<bool> ValiderPublicationAsync(string coachUserId, int missionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Passe <c>EnAttenteModeration</c> → <c>Annulee</c> avec motif obligatoire (pas de statut Refusee séparé).
    /// </summary>
    Task<bool> RefuserPublicationAsync(
        string coachUserId,
        int missionId,
        string motif,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MissionResumeView>> GetMissionsDisponiblesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MissionJeuneView>> GetMesMissionsAsync(string jeuneUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Accepte une mission <c>Disponible</c>. Refuse aussi si la charte n'est pas acceptée
    /// (<see cref="Spectrometre.Modules.JeunesPrestataires.Services.ICharteService"/>).
    /// </summary>
    Task<bool> AccepterMissionAsync(string jeuneUserId, int missionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MissionAcceptationView>> GetDemandesEnAttentePourCoachAsync(string coachUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MissionAcceptationView>> GetDemandesEnAttentePourJeuneSuiviAsync(string coachUserId, string suiviUserId, CancellationToken cancellationToken = default);

    Task<bool> ValiderAcceptationAsync(string coachUserId, int missionAcceptationId, CancellationToken cancellationToken = default);

    Task<bool> RefuserAcceptationAsync(string coachUserId, int missionAcceptationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Le jeune propriétaire retire sa candidature tant qu'elle est
    /// <see cref="MissionAcceptationStatut.EnAttenteValidationCoach"/>.
    /// Statut distinct <see cref="MissionAcceptationStatut.RetireeParJeune"/> (pas un refus coach).
    /// Mission → <c>Disponible</c>. <c>false</c> après validation coach, ou si ce n'est pas sa candidature.
    /// </summary>
    Task<bool> RetirerCandidatureAsync(string jeuneUserId, int missionAcceptationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Le jeune propriétaire marque une mission <c>Attribuee</c> comme <c>Terminee</c>.
    /// <c>false</c> si non propriétaire, acceptation non validée, ou statut mission ≠ Attribuee.
    /// </summary>
    Task<bool> MarquerTermineeAsync(string jeuneUserId, int missionAcceptationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Missions terminées du jeune suivi (lecture coach) — pour consulter les retours.
    /// Liste vide si coach non autorisé.
    /// </summary>
    Task<IReadOnlyList<MissionJeuneView>> GetMissionsTermineesPourJeuneSuiviAsync(
        string coachUserId,
        string suiviUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Missions <c>Attribuee</c> du jeune suivi (lecture coach) — même garde que
    /// <see cref="GetMissionsTermineesPourJeuneSuiviAsync"/>. Liste vide si coach non autorisé.
    /// </summary>
    Task<IReadOnlyList<MissionJeuneView>> GetMissionsEnCoursPourJeuneSuiviAsync(
        string coachUserId,
        string suiviUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MissionResumeView>> GetMesMissionsPublieesAsync(string particulierUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Annule une mission <c>Disponible</c> ou <c>EnAttenteModeration</c> du propriétaire → <c>Annulee</c>.
    /// </summary>
    Task<bool> AnnulerMissionAsync(string particulierUserId, int missionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Détail pour pré-remplir la modification. Null si non propriétaire ou statut
    /// ≠ Disponible / EnAttenteModeration.
    /// </summary>
    Task<MissionDetailView?> TryGetMissionPourModificationAsync(
        string particulierUserId,
        int missionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Met à jour une mission <c>Disponible</c> ou <c>EnAttenteModeration</c> du propriétaire.
    /// Ne change ni le statut ni <c>CreatedAt</c>.
    /// </summary>
    Task<bool> ModifierMissionAsync(
        string particulierUserId,
        int missionId,
        PublierMissionInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Signale un problème pendant une mission <c>Attribuee</c> : notifie le coach suiveur actif du jeune.
    /// <c>false</c> si non propriétaire, statut ≠ Attribuee, ou aucun coach actif.
    /// Signature inchangée — incident réel uniquement ; le contact simple est
    /// <see cref="DemanderContactParticulierAsync"/> (TypeCode distinct).
    /// </summary>
    Task<bool> SignalerProblemeAsync(
        string particulierUserId,
        int missionId,
        string? message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Demande de contact non urgente du particulier vers le coach (même canal que
    /// <see cref="SignalerProblemeAsync"/>, pas de fil de discussion). Mission <c>Attribuee</c>.
    /// </summary>
    Task<bool> DemanderContactParticulierAsync(
        string particulierUserId,
        int missionId,
        string? message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Demande de contact non urgente du jeune vers son coach référent — jamais un échange
    /// direct avec le particulier. Titulaire de l'acceptation, mission <c>Attribuee</c> ou <c>Terminee</c>.
    /// </summary>
    Task<bool> DemanderContactCoachAsync(
        string jeuneUserId,
        int missionAcceptationId,
        string? message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Signalement d'un problème par le jeune (même garde que <see cref="DemanderContactCoachAsync"/>).
    /// Méthode séparée de <see cref="SignalerProblemeAsync"/> : identifiant d'acceptation, pas de missionId.
    /// </summary>
    Task<bool> SignalerProblemeJeuneAsync(
        string jeuneUserId,
        int missionAcceptationId,
        string? message,
        CancellationToken cancellationToken = default);
}
