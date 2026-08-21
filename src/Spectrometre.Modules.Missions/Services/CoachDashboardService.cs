using Spectrometre.Core.JeunesPrestataires;
using Spectrometre.Core.Notifications;
using Spectrometre.Modules.Coaching.Entities;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.JeunesPrestataires.Services;

namespace Spectrometre.Modules.Missions.Services;

/// <summary>
/// Agrège les compteurs et deep-links coach pour le tableau de bord Host. Vit dans Missions car c'est
/// le seul module qui référence déjà Coaching + JeunesPrestataires sans créer de cycle (Host consomme
/// l'interface).
/// </summary>
/// <remarks>
/// <para><b>Dossiers incomplets</b> — le document Bouchra ne définit pas la métrique. Interprétation
/// retenue pour ce cycle : jeune suivi actif, mineur, et consentement parental non encore validé
/// (<see cref="IConsentementParentalService.EstConsentementValideAsync"/> = false). Un majeur n'entre
/// jamais dans ce compteur (consentement parental hors périmètre).</para>
/// <para><b>Alertes</b> — invitations jeunes encore en attente avec <c>EstExpiree</c> (à relancer ou
/// annuler). <b>Signalements / demandes de contact</b> — notifications non lues
/// <c>Missions.ProblemeSignale</c> et <c>Missions.DemandeContact</c> (messagerie légère,
/// jeune ou particulier → coach). Pas de carte « rendez-vous » : aucune notion RDV/planification
/// coach↔jeune en base.</para>
/// <para><b>Actions rapides omises</b> — voir <see cref="CoachDashboardActionsRapides"/> (Proposer une
/// mission, Demander une précision).</para>
/// </remarks>
public sealed class CoachDashboardService(
    ICoachingService coachingService,
    IMissionService missionService,
    IJeuneProfileService jeuneProfileService,
    IConsentementParentalService consentementParentalService,
    IJeunePrestataireInvitationQuery invitationQuery,
    IGuideEntrevueService guideEntrevueService,
    IObjectifsCoachingService objectifsCoachingService,
    INotificationService notificationService) : ICoachDashboardService
{
    /// <summary>Identiques aux TypeCode écrits par <c>MissionService.EnvoyerNotificationCoachAsync</c>.</summary>
    private static readonly HashSet<string> TypeCodesSignalementOuContact = new(StringComparer.Ordinal)
    {
        "Missions.ProblemeSignale",
        "Missions.DemandeContact",
    };

    public async Task<CoachDashboardSynthese> GetSyntheseAsync(string coachUserId, CancellationToken cancellationToken = default)
    {
        var liens = await coachingService.GetLiensPourCoachAsync(coachUserId, cancellationToken);
        var actifs = liens.Where(l => l.Statut == LienCoachingStatut.Actif).ToList();

        var missions = await missionService.GetDemandesEnAttentePourCoachAsync(coachUserId, cancellationToken);

        var dossiersIncomplets = 0;
        foreach (var lien in actifs)
        {
            var jeune = await jeuneProfileService.TryGetByUserIdAsync(lien.SuiviUserId, cancellationToken);
            if (jeune is null)
                continue;

            if (!jeuneProfileService.EstMineur(jeune.DateNaissance))
                continue;

            if (!await consentementParentalService.EstConsentementValideAsync(jeune.Id, cancellationToken))
                dossiersIncomplets++;
        }

        var invitations = await invitationQuery.GetInvitationsEnvoyeesEnAttenteAsync(coachUserId, cancellationToken);
        var alertes = invitations.Count(i => i.EstExpiree);

        var nonLues = await notificationService.GetNonLuesAsync(coachUserId, cancellationToken);
        var signalements = nonLues.Count(n => TypeCodesSignalementOuContact.Contains(n.TypeCode));

        return new CoachDashboardSynthese(
            JeunesSuivisActifs: actifs.Count,
            MissionsAValider: missions.Count,
            DossiersIncomplets: dossiersIncomplets,
            AlertesInvitationsExpirees: alertes,
            SignalementsEtDemandesNonLus: signalements);
    }

    public async Task<CoachDashboardActionsRapides> GetActionsRapidesAsync(
        string coachUserId,
        CancellationToken cancellationToken = default)
    {
        var liens = await coachingService.GetLiensPourCoachAsync(coachUserId, cancellationToken);
        var actifs = liens
            .Where(l => l.Statut == LienCoachingStatut.Actif)
            .OrderBy(l => l.CreatedAt)
            .ToList();

        var validerMissionHref = await ResolveValiderMissionHrefAsync(coachUserId, cancellationToken);
        var (guideHref, guideContinuer) = await ResolveGuideEntrevueAsync(coachUserId, actifs, cancellationToken);

        string? relancerHref = null;
        var invitations = await invitationQuery.GetInvitationsEnvoyeesEnAttenteAsync(coachUserId, cancellationToken);
        if (invitations.Any(i => i.EstExpiree))
            relancerHref = "/coach/suivis#invitations-jeunes";

        string? cloturerHref = null;
        var lienObjectifs = await objectifsCoachingService.TryGetPremierLienIdAvecObjectifsOuvertsAsync(
            coachUserId, cancellationToken);
        if (lienObjectifs is int lid)
            cloturerHref = $"/coach/objectifs/{lid}";

        return new CoachDashboardActionsRapides(
            ValiderMissionHref: validerMissionHref,
            GuideEntrevueHref: guideHref,
            GuideEntrevueEstContinuer: guideContinuer,
            RelancerInvitationHref: relancerHref,
            CloturerObjectifsHref: cloturerHref);
    }

    /// <summary>
    /// Première demande en attente = acceptation la plus ancienne (<see cref="IMissionService.GetDemandesEnAttentePourCoachAsync"/>
    /// déjà triée par <c>AccepteeLe</c>). Résout le <c>SuiviUserId</c> via le profil jeune.
    /// </summary>
    private async Task<string?> ResolveValiderMissionHrefAsync(string coachUserId, CancellationToken cancellationToken)
    {
        var missions = await missionService.GetDemandesEnAttentePourCoachAsync(coachUserId, cancellationToken);
        var premiere = missions.FirstOrDefault();
        if (premiere is null)
            return null;

        var jeune = await jeuneProfileService.TryGetByIdAsync(premiere.JeuneProfileId, cancellationToken);
        if (jeune is null)
            return null;

        return $"/coach/suivis/{jeune.UserId}/missions";
    }

    /// <summary>
    /// <c>GuideEntrevueView.Id == null</c> = jamais persisté (GetOrCreate ne crée pas en base).
    /// Préférer le premier jeune (CreatedAt) sans guide ; sinon Continuer sur le premier suivi actif.
    /// </summary>
    private async Task<(string? Href, bool EstContinuer)> ResolveGuideEntrevueAsync(
        string coachUserId,
        IReadOnlyList<LienCoachingView> actifsOrdonnes,
        CancellationToken cancellationToken)
    {
        if (actifsOrdonnes.Count == 0)
            return (null, false);

        LienCoachingView? premierSansGuide = null;
        foreach (var lien in actifsOrdonnes)
        {
            var jeune = await jeuneProfileService.TryGetByUserIdAsync(lien.SuiviUserId, cancellationToken);
            if (jeune is null)
                continue;

            var guide = await guideEntrevueService.GetOrCreateAsync(coachUserId, jeune.Id, cancellationToken);
            if (guide is null)
                continue;

            // Jamais sauvegardé : Id null (entité absente) — distinct d'un guide créé avec champs vides (Id > 0).
            if (guide.Id is null)
            {
                premierSansGuide = lien;
                break;
            }
        }

        if (premierSansGuide is not null)
            return ($"/coach/suivis/{premierSansGuide.SuiviUserId}/guide-entrevue", false);

        var premier = actifsOrdonnes[0];
        return ($"/coach/suivis/{premier.SuiviUserId}/guide-entrevue", true);
    }
}

