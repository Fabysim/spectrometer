using Spectrometre.Modules.Missions.Entities;

namespace Spectrometre.Modules.Missions.Services;

public interface IMissionService
{
    Task<int?> PublierMissionAsync(string particulierUserId, PublierMissionInput input, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MissionResumeView>> GetMissionsDisponiblesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MissionJeuneView>> GetMesMissionsAsync(string jeuneUserId, CancellationToken cancellationToken = default);

    Task<bool> AccepterMissionAsync(string jeuneUserId, int missionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MissionAcceptationView>> GetDemandesEnAttentePourCoachAsync(string coachUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MissionAcceptationView>> GetDemandesEnAttentePourJeuneSuiviAsync(string coachUserId, string suiviUserId, CancellationToken cancellationToken = default);

    Task<bool> ValiderAcceptationAsync(string coachUserId, int missionAcceptationId, CancellationToken cancellationToken = default);

    Task<bool> RefuserAcceptationAsync(string coachUserId, int missionAcceptationId, CancellationToken cancellationToken = default);

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
    /// Annule une mission <c>Disponible</c> du particulier propriétaire → <c>Annulee</c>.
    /// Refuse si non propriétaire ou statut ≠ Disponible (ex. déjà en attente / attribuée).
    /// </summary>
    Task<bool> AnnulerMissionAsync(string particulierUserId, int missionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Signale un problème pendant une mission <c>Attribuee</c> : notifie le coach suiveur actif du jeune.
    /// <c>false</c> si non propriétaire, statut ≠ Attribuee, ou aucun coach actif.
    /// </summary>
    Task<bool> SignalerProblemeAsync(
        string particulierUserId,
        int missionId,
        string? message,
        CancellationToken cancellationToken = default);
}
