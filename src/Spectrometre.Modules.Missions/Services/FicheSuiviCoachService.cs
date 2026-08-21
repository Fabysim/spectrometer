using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Identity;
using Spectrometre.Core.JeunesPrestataires;
using Spectrometre.Modules.Coaching.Entities;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.JeunesPrestataires.Entities;
using Spectrometre.Modules.JeunesPrestataires.Services;

namespace Spectrometre.Modules.Missions.Services;

/// <summary>
/// Fiche de suivi coach — agrégation lecture seule (identité, consentement, missions, grille, guide).
/// Placée dans Missions comme <see cref="MesProgresService"/> et <see cref="CoachDashboardService"/> :
/// c'est le module qui référence déjà Coaching + JeunesPrestataires + missions, sans cycle
/// JeunesPrestataires → Missions.
/// </summary>
public enum FicheSuiviConsentementStatut
{
    MajeurNonRequis = 0,
    MineurValide = 1,
    MineurEnAttente = 2,
}

public sealed record FicheSuiviCoachView(
    string SuiviUserId,
    string Nom,
    string Prenoms,
    DateOnly DateNaissance,
    int Age,
    bool EstMineur,
    FicheSuiviConsentementStatut ConsentementStatut,
    int MissionsTerminees,
    int MissionsEnCours,
    double? GrilleDerniereMoyenne,
    DateTimeOffset? GrilleDerniereEvaluationLe,
    bool GuideEntrevueRempli,
    int? LienCoachingId,
    ProfilAccompagnement ProfilAccompagnement,
    IReadOnlyList<RetourParticulierCoachItem> RetoursParticuliersRecents,
    ConsentementParentalCoachView? ContactParental,
    string? Email = null,
    string? Telephone = null);

public interface IFicheSuiviCoachService
{
    /// <summary>
    /// Vue agrégée pour le coach suiveur. <c>null</c> si non autorisé ou profil jeune introuvable.
    /// </summary>
    Task<FicheSuiviCoachView?> GetAsync(
        string coachUserId,
        string suiviUserId,
        CancellationToken cancellationToken = default);
}

public sealed class FicheSuiviCoachService(
    ICoachingService coachingService,
    IJeuneProfileService jeuneProfileService,
    IConsentementParentalService consentementParentalService,
    IMissionService missionService,
    IGrilleObservationService grilleObservationService,
    IGuideEntrevueService guideEntrevueService,
    IRetoursParticuliersCoachQuery retoursParticuliersQuery,
    UserManager<ApplicationUser> userManager) : IFicheSuiviCoachService
{
    private const int NbRetoursResumes = 3;

    public async Task<FicheSuiviCoachView?> GetAsync(
        string coachUserId,
        string suiviUserId,
        CancellationToken cancellationToken = default)
    {
        var autorise = await coachingService.GetSuiviUserIdSiAutoriseAsync(suiviUserId, coachUserId, cancellationToken);
        if (autorise is null)
            return null;

        var jeune = await jeuneProfileService.TryGetByUserIdAsync(suiviUserId, cancellationToken);
        if (jeune is null)
            return null;

        var estMineur = jeuneProfileService.EstMineur(jeune.DateNaissance);
        var consentementValide = await consentementParentalService.EstConsentementValideAsync(jeune.Id, cancellationToken);
        var consentementStatut = !estMineur
            ? FicheSuiviConsentementStatut.MajeurNonRequis
            : consentementValide
                ? FicheSuiviConsentementStatut.MineurValide
                : FicheSuiviConsentementStatut.MineurEnAttente;

        var terminees = await missionService.GetMissionsTermineesPourJeuneSuiviAsync(
            coachUserId, suiviUserId, cancellationToken);
        var enCours = await missionService.GetMissionsEnCoursPourJeuneSuiviAsync(
            coachUserId, suiviUserId, cancellationToken);

        var historique = await grilleObservationService.GetHistoriqueAsync(
            coachUserId, jeune.Id, cancellationToken);
        double? derniereMoyenne = null;
        DateTimeOffset? derniereLe = null;
        if (historique.Count > 0)
        {
            derniereMoyenne = historique[0].MoyenneScore;
            derniereLe = historique[0].EvalueeLe;
        }

        var guide = await guideEntrevueService.GetOrCreateAsync(coachUserId, jeune.Id, cancellationToken);
        var guideRempli = guide?.Id is not null;

        var liens = await coachingService.GetLiensPourCoachAsync(coachUserId, cancellationToken);
        var lienId = liens
            .FirstOrDefault(l => l.SuiviUserId == suiviUserId && l.Statut == LienCoachingStatut.Actif)
            ?.Id;

        var retours = await retoursParticuliersQuery.GetHistoriquePourCoachAsync(
            coachUserId, jeune.Id, cancellationToken);

        var contactParental = consentementStatut == FicheSuiviConsentementStatut.MineurValide
            ? await consentementParentalService.TryGetPourCoachAsync(coachUserId, jeune.Id, cancellationToken)
            : null;

        var compte = await userManager.Users.AsNoTracking()
            .Where(u => u.Id == suiviUserId)
            .Select(u => new { u.Email, u.PhoneNumber })
            .FirstOrDefaultAsync(cancellationToken);

        return new FicheSuiviCoachView(
            suiviUserId,
            jeune.Nom,
            jeune.Prenoms,
            jeune.DateNaissance,
            jeuneProfileService.CalculerAge(jeune.DateNaissance),
            estMineur,
            consentementStatut,
            terminees.Count,
            enCours.Count,
            derniereMoyenne,
            derniereLe,
            guideRempli,
            lienId,
            jeune.ProfilAccompagnement,
            retours.Take(NbRetoursResumes).ToList(),
            contactParental,
            compte?.Email,
            compte?.PhoneNumber);
    }
}
