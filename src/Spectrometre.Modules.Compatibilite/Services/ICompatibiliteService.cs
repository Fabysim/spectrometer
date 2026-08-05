using Spectrometre.Modules.Compatibilite.Entities;

namespace Spectrometre.Modules.Compatibilite.Services;

public sealed record AxisScoreView(CompatibilityAxis Axis, int Score);

public sealed record CompatibiliteResultView(
    int ScoreGlobal,
    IReadOnlyList<AxisScoreView> ScoresParAxe,
    IReadOnlyList<string> PointsDeVigilance,
    DateTimeOffset CalculatedAt);

/// <summary>
/// Service public du module Moteur de Compatibilité. Ne lit jamais directement les DbContext de
/// Profil Candidat / Profil Entreprise — passe exclusivement par leurs services publics
/// (<c>ICandidateProfileService</c>, <c>ICompanyProfileService</c>).
/// </summary>
public interface ICompatibiliteService
{
    Task<CompatibiliteResultView> CalculerCompatibiliteAsync(int candidateProfileId, int companyProfileId, CancellationToken cancellationToken = default);

    Task<CompatibiliteResultView?> GetDernierResultatAsync(int candidateProfileId, int companyProfileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Point d'entrée à utiliser par toute page/API exposant un résultat de compatibilité à un utilisateur
    /// authentifié par simple identifiant de candidat dans l'URL (ex. <c>/compatibilite/resultat/{id}</c>) :
    /// applique la règle d'accès (le candidat lui-même, ou un gestionnaire d'une entreprise pour laquelle
    /// une candidature réelle existe pour ce candidat) et retourne <c>null</c> dans tous les autres cas —
    /// jamais une exception d'accès refusé, pour ne pas confirmer l'existence de la ressource à un tiers.
    /// Voir l'implémentation pour le détail de la résolution de l'entreprise concernée.
    /// </summary>
    Task<CompatibiliteResultView?> GetResultatAutorisePourUtilisateurAsync(int candidateProfileId, string requestingUserId, CancellationToken cancellationToken = default);
}
