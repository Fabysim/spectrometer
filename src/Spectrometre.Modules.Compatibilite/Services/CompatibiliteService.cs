using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Compatibilite.Data;
using Spectrometre.Modules.Compatibilite.Entities;
using Spectrometre.Modules.ProfilCandidat.Services;
using Spectrometre.Modules.ProfilEntreprise.Services;

namespace Spectrometre.Modules.Compatibilite.Services;

/// <summary>Voir le commentaire équivalent sur <c>CompanyProfileService</c> : DbContext tenant-scopé, donc instancié via la factory à chaque opération plutôt qu'injecté au constructeur, schéma affecté après coup depuis <see cref="ITenantContext"/>.</summary>
public sealed class CompatibiliteService(
    IDbContextFactory<CompatibiliteDbContext> dbFactory,
    ITenantContext tenantContext,
    ICandidateProfileService candidateProfileService,
    ICompanyProfileService companyProfileService) : ICompatibiliteService
{
    private const int SeuilVigilance = 50;

    private async Task<CompatibiliteDbContext> CreateDbAsync(CancellationToken cancellationToken)
    {
        var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        db.TenantSchema = tenantContext.SchemaName;
        return db;
    }

    public async Task<CompatibiliteResultView> CalculerCompatibiliteAsync(int candidateProfileId, int companyProfileId, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbAsync(cancellationToken);

        var candidateCriteria = await candidateProfileService.GetCompatibilityCriteriaAsync(candidateProfileId, cancellationToken);
        var companyCriteria = await companyProfileService.GetCompatibilityCriteriaAsync(companyProfileId, cancellationToken);

        var scores = new Dictionary<CompatibilityAxis, int>
        {
            [CompatibilityAxis.Technique] = TextSimilarityScorer.Score(candidateCriteria?.TechniqueText, companyCriteria?.TechniqueText),
            [CompatibilityAxis.Comportementale] = TextSimilarityScorer.Score(candidateCriteria?.ComportementaleText, companyCriteria?.ComportementaleText),
            [CompatibilityAxis.Culturelle] = TextSimilarityScorer.Score(candidateCriteria?.CulturelleText, companyCriteria?.CulturelleText),
            [CompatibilityAxis.Organisationnelle] = TextSimilarityScorer.Score(candidateCriteria?.OrganisationnelleText, companyCriteria?.OrganisationnelleText),
            [CompatibilityAxis.Motivationnelle] = TextSimilarityScorer.Score(candidateCriteria?.MotivationnelleText, companyCriteria?.MotivationnelleText),
        };

        var weights = await db.CompatibilityWeightSettings.AsNoTracking().ToListAsync(cancellationToken);
        var totalWeight = weights.Sum(w => w.WeightPercent);
        var scoreGlobal = totalWeight == 0
            ? 0
            : (int)Math.Round(weights.Sum(w => scores[w.Axis] * w.WeightPercent) / totalWeight);

        var vigilancePoints = new List<string>();
        foreach (var (axis, score) in scores)
        {
            if (score < SeuilVigilance)
                vigilancePoints.Add($"Axe {CompatibilityAxisLabels.Label(axis)} : score faible ({score}%), à approfondir en entretien.");
        }

        vigilancePoints.AddRange(VigilanceDetector.Detect(companyCriteria?.PointsVigilanceText, candidateCriteria?.PointsVigilanceText));
        foreach (var axisText in new[]
                 {
                     (companyCriteria?.TechniqueText, candidateCriteria?.PointsVigilanceText),
                     (companyCriteria?.OrganisationnelleText, candidateCriteria?.PointsVigilanceText),
                     (companyCriteria?.ComportementaleText, candidateCriteria?.PointsVigilanceText),
                 })
        {
            vigilancePoints.AddRange(VigilanceDetector.Detect(axisText.Item1, axisText.Item2));
        }
        vigilancePoints = vigilancePoints.Distinct().ToList();

        var result = new CompatibilityResult
        {
            CandidateProfileId = candidateProfileId,
            CompanyProfileId = companyProfileId,
            ScoreTechnique = scores[CompatibilityAxis.Technique],
            ScoreComportementale = scores[CompatibilityAxis.Comportementale],
            ScoreCulturelle = scores[CompatibilityAxis.Culturelle],
            ScoreOrganisationnelle = scores[CompatibilityAxis.Organisationnelle],
            ScoreMotivationnelle = scores[CompatibilityAxis.Motivationnelle],
            ScoreGlobal = scoreGlobal,
            CalculatedAt = DateTimeOffset.UtcNow,
        };
        foreach (var point in vigilancePoints)
            result.VigilancePoints.Add(new CompatibilityVigilancePoint { Text = point });

        db.CompatibilityResults.Add(result);
        await db.SaveChangesAsync(cancellationToken);

        return ToView(result, scores);
    }

    public async Task<CompatibiliteResultView?> GetDernierResultatAsync(int candidateProfileId, int companyProfileId, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbAsync(cancellationToken);

        var result = await db.CompatibilityResults
            .AsNoTracking()
            .Include(r => r.VigilancePoints)
            .Where(r => r.CandidateProfileId == candidateProfileId && r.CompanyProfileId == companyProfileId)
            .OrderByDescending(r => r.CalculatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (result is null)
            return null;

        var scores = new Dictionary<CompatibilityAxis, int>
        {
            [CompatibilityAxis.Technique] = result.ScoreTechnique,
            [CompatibilityAxis.Comportementale] = result.ScoreComportementale,
            [CompatibilityAxis.Culturelle] = result.ScoreCulturelle,
            [CompatibilityAxis.Organisationnelle] = result.ScoreOrganisationnelle,
            [CompatibilityAxis.Motivationnelle] = result.ScoreMotivationnelle,
        };

        return ToView(result, scores);
    }

    private static CompatibiliteResultView ToView(CompatibilityResult result, Dictionary<CompatibilityAxis, int> scores) =>
        new(
            result.ScoreGlobal,
            scores.Select(kv => new AxisScoreView(kv.Key, kv.Value)).ToList(),
            result.VigilancePoints.Select(v => v.Text).ToList(),
            result.CalculatedAt);
}
