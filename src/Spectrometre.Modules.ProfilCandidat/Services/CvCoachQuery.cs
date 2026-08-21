using Spectrometre.Core.Modules;

namespace Spectrometre.Modules.ProfilCandidat.Services;

/// <summary>
/// Lecture du CV d'un jeune par son coach suiveur — sans copie des données.
/// Vit dans ProfilCandidat (CvView et ICandidateProfileService y appartiennent) et s'autorise
/// via <see cref="ICoachingAccessChecker"/> (Core) : ProfilCandidat ne référence pas Coaching,
/// même inversion que GestionDuTemps. Pas dans Core (évite d'y dupliquer CvView) ni dans
/// Missions (évite une dépendance Missions → ProfilCandidat pour un lien d'accompagnement).
/// </summary>
public interface ICvCoachQuery
{
    /// <summary>
    /// CV du jeune suivi, ou <c>null</c> si le coach n'est pas autorisé
    /// (<see cref="ICoachingAccessChecker.GetSuiviUserIdSiAutoriseAsync"/>).
    /// Si le jeune n'a pas encore de profil candidat, retourne un <see cref="CvView"/> vide
    /// (aucune création de ligne — <see cref="ICandidateProfileService.TryGetProfileIdAsync"/>).
    /// </summary>
    Task<CvView?> TryGetPourCoachAsync(
        string coachUserId,
        string suiviUserId,
        CancellationToken cancellationToken = default);
}

public sealed class CvCoachQuery(
    ICoachingAccessChecker coachingAccess,
    ICandidateProfileService candidateProfileService) : ICvCoachQuery
{
    public async Task<CvView?> TryGetPourCoachAsync(
        string coachUserId,
        string suiviUserId,
        CancellationToken cancellationToken = default)
    {
        var autorise = await coachingAccess.GetSuiviUserIdSiAutoriseAsync(
            suiviUserId, coachUserId, cancellationToken);
        if (autorise is null)
            return null;

        var profileId = await candidateProfileService.TryGetProfileIdAsync(autorise, cancellationToken);
        if (profileId is null)
            return new CvView(null, [], null, [], null, null, [], null);

        return await candidateProfileService.GetCvAsync(profileId.Value, cancellationToken);
    }
}
