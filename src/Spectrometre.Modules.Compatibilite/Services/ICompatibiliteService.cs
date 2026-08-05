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
}
