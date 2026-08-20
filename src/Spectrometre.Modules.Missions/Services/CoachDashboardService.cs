using Spectrometre.Core.JeunesPrestataires;
using Spectrometre.Modules.Coaching.Entities;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.JeunesPrestataires.Services;

namespace Spectrometre.Modules.Missions.Services;

/// <summary>
/// Agrège les compteurs coach pour le tableau de bord Host. Vit dans Missions car c'est le seul module
/// qui référence déjà Coaching + JeunesPrestataires sans créer de cycle (Host consomme l'interface).
/// </summary>
/// <remarks>
/// <para><b>Dossiers incomplets</b> — le document Bouchra ne définit pas la métrique. Interprétation
/// retenue pour ce cycle : jeune suivi actif, mineur, et consentement parental non encore validé
/// (<see cref="IConsentementParentalService.EstConsentementValideAsync"/> = false). Un majeur n'entre
/// jamais dans ce compteur (consentement parental hors périmètre).</para>
/// <para><b>Alertes</b> — invitations jeunes encore en attente avec <c>EstExpiree</c> (à relancer ou
/// annuler). Pas de carte « rendez-vous » : aucune notion RDV/planification coach↔jeune en base.</para>
/// </remarks>
public sealed class CoachDashboardService(
    ICoachingService coachingService,
    IMissionService missionService,
    IJeuneProfileService jeuneProfileService,
    IConsentementParentalService consentementParentalService,
    IJeunePrestataireInvitationQuery invitationQuery) : ICoachDashboardService
{
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

        return new CoachDashboardSynthese(
            JeunesSuivisActifs: actifs.Count,
            MissionsAValider: missions.Count,
            DossiersIncomplets: dossiersIncomplets,
            AlertesInvitationsExpirees: alertes);
    }
}
