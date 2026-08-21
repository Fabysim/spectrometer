namespace Spectrometre.Modules.Missions.Services;

/// <summary>Compteurs de synthèse du tableau de bord coach (document Bouchra — cartes de synthèse).</summary>
public sealed record CoachDashboardSynthese(
    int JeunesSuivisActifs,
    int MissionsAValider,
    int DossiersIncomplets,
    int AlertesInvitationsExpirees,
    /// <summary>
    /// Notifications non lues <c>Missions.ProblemeSignale</c> ou <c>Missions.DemandeContact</c>
    /// (mêmes TypeCode que <c>MissionService</c> — messagerie légère).
    /// </summary>
    int SignalementsEtDemandesNonLus);

/// <summary>
/// Deep-links des actions rapides du tableau de bord coach. Chaque href est <c>null</c> si l'action
/// n'a pas de cible concrète (masquée côté UI — jamais un lien générique vers « rien »).
/// </summary>
/// <remarks>
/// <para><b>Actions omises (document Bouchra, volontairement hors cycle)</b> — même schéma que la carte
/// « rendez-vous » absente de <see cref="CoachDashboardSynthese"/> :</para>
/// <list type="bullet">
/// <item>
/// <b>Proposer une mission</b> — dans l'architecture actuelle, seul un Particulier publie une mission
/// (<c>IMissionService.PublierMissionAsync</c>). Un coach n'a pas ce rôle ; pas de mécanisme inventé ici.
/// </item>
/// <item>
/// <b>Demander une précision</b> — il n'existe toujours pas de messagerie libre <c>coach→jeune</c>
/// (pas de fil de discussion, pas d'envoi depuis le coach). Ce qui existe depuis le cycle
/// « messagerie légère » : notifications in-app <c>jeune→coach</c> ou <c>particulier→coach</c>
/// uniquement (<c>Missions.ProblemeSignale</c>, <c>Missions.DemandeContact</c>), consultables
/// dans la cloche. L'action Bouchra « Demander une précision » (coach qui interroge le jeune)
/// n'est donc toujours pas inventée ici.
/// </item>
/// </list>
/// </remarks>
public sealed record CoachDashboardActionsRapides(
    /// <summary>Ex. <c>/coach/suivis/{userId}/missions</c> — première acceptation en attente (plus ancienne).</summary>
    string? ValiderMissionHref,
    /// <summary>Ex. <c>/coach/suivis/{userId}/guide-entrevue</c>.</summary>
    string? GuideEntrevueHref,
    /// <summary>
    /// <c>true</c> = tous les jeunes suivis ont déjà un guide persisté → libellé « Continuer » ;
    /// <c>false</c> = au moins un guide jamais sauvegardé (<c>GuideEntrevueView.Id == null</c>) → « Préparer ».
    /// Ignoré si <see cref="GuideEntrevueHref"/> est null.
    /// </summary>
    bool GuideEntrevueEstContinuer,
    /// <summary>Ex. <c>/coach/suivis#invitations-jeunes</c> si au moins une invitation expirée.</summary>
    string? RelancerInvitationHref,
    /// <summary>Ex. <c>/coach/objectifs/{lienId}</c> — premier lien avec objectifs ouverts (Atteinte ≠ Oui).</summary>
    string? CloturerObjectifsHref);

public interface ICoachDashboardService
{
    Task<CoachDashboardSynthese> GetSyntheseAsync(string coachUserId, CancellationToken cancellationToken = default);

    Task<CoachDashboardActionsRapides> GetActionsRapidesAsync(string coachUserId, CancellationToken cancellationToken = default);
}
