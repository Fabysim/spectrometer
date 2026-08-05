using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Data;
using Spectrometre.Core.Recruitment;
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
    ICompanyProfileService companyProfileService,
    ICompanyProvisioningService companyProvisioningService,
    ICandidatureExistenceChecker candidatureExistenceChecker,
    CoreDbContext coreDb) : ICompatibiliteService
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

        var candidateTechnique = candidateCriteria?.TechniqueTags ?? [];
        var candidateComportementale = candidateCriteria?.ComportementaleTags ?? [];
        var candidateCulturelle = candidateCriteria?.CulturelleTags ?? [];
        var candidateMotivationnelle = candidateCriteria?.MotivationnelleTags ?? [];
        var candidateVigilance = candidateCriteria?.PointsVigilanceTags ?? [];

        var companyTechnique = companyCriteria?.TechniqueTags ?? [];
        var companyComportementale = companyCriteria?.ComportementaleTags ?? [];
        var companyCulturelle = companyCriteria?.CulturelleTags ?? [];
        var companyMotivationnelle = companyCriteria?.MotivationnelleTags ?? [];
        var companyVigilance = companyCriteria?.PointsVigilanceTags ?? [];

        var scores = new Dictionary<CompatibilityAxis, int>
        {
            [CompatibilityAxis.Technique] = StructuredCriteriaScorer.TagOverlapScore(candidateTechnique, companyTechnique),
            [CompatibilityAxis.Comportementale] = StructuredCriteriaScorer.TagOverlapScore(candidateComportementale, companyComportementale),
            [CompatibilityAxis.Culturelle] = StructuredCriteriaScorer.TagOverlapScore(candidateCulturelle, companyCulturelle),
            [CompatibilityAxis.Organisationnelle] = StructuredCriteriaScorer.ScaleScore(candidateCriteria?.RythmeTravail, companyCriteria?.RythmeTravail),
            [CompatibilityAxis.Motivationnelle] = StructuredCriteriaScorer.TagOverlapScore(candidateMotivationnelle, companyMotivationnelle),
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

        // Un tag de vigilance signalé à la fois par l'entreprise et par le candidat est un vrai risque
        // partagé (ex. « Rythme intense ») — plus fiable que l'ancienne détection par mots-clés sur texte libre.
        foreach (var tag in StructuredCriteriaScorer.SharedVigilanceTags(candidateVigilance, companyVigilance))
            vigilancePoints.Add($"Point de vigilance partagé : {tag} (signalé par l'entreprise et par le candidat).");

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
        foreach (var point in vigilancePoints.Distinct())
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

    /// <summary>
    /// Voir le commentaire sur <see cref="ICompatibiliteService.GetResultatAutorisePourUtilisateurAsync"/>.
    /// </summary>
    /// <remarks>
    /// Cas 1 (le candidat consulte son propre résultat) : un résultat de compatibilité est toujours relatif
    /// à UNE entreprise, or cette page n'a jamais reçu d'identifiant d'entreprise dans son URL — seule
    /// l'entreprise déjà active dans ce circuit (<see cref="ITenantContext.ActiveCompanyId"/>) peut servir
    /// de contexte. Si aucune entreprise n'est active, on retourne <c>null</c> : ce n'est pas un refus
    /// d'accès (le candidat garde le droit de voir SES résultats), simplement l'absence de contexte
    /// permettant d'en calculer un — documenté ici plutôt que de fabriquer un résultat arbitraire.
    /// <para/>
    /// Cas 2 (un gestionnaire d'entreprise consulte le résultat d'un candidat) : on cherche parmi TOUTES
    /// les entreprises que l'utilisateur gère (pas seulement celle déjà active) celle, s'il en existe une,
    /// pour laquelle une candidature réelle existe pour ce candidat — un gestionnaire de plusieurs
    /// entreprises ne doit pas être refusé à tort simplement parce que la mauvaise entreprise était active
    /// au moment de la requête.
    /// </remarks>
    public async Task<CompatibiliteResultView?> GetResultatAutorisePourUtilisateurAsync(int candidateProfileId, string requestingUserId, CancellationToken cancellationToken = default)
    {
        var ownCandidateProfileId = await candidateProfileService.GetOrCreateProfileIdAsync(requestingUserId, cancellationToken);

        if (ownCandidateProfileId == candidateProfileId)
        {
            if (tenantContext.ActiveCompanyId is null)
                return null;

            var ownCompanyProfileId = await companyProfileService.GetOrCreateProfileIdAsync(cancellationToken);
            return await CalculerCompatibiliteAsync(candidateProfileId, ownCompanyProfileId, cancellationToken);
        }

        var companies = await companyProvisioningService.GetCompaniesForUserAsync(requestingUserId, coreDb, cancellationToken);
        foreach (var company in companies)
        {
            if (!await candidatureExistenceChecker.ExisteCandidatureReelleAsync(candidateProfileId, company.Id, cancellationToken))
                continue;

            tenantContext.SetActiveCompany(company.Id, company.SchemaName);
            var companyProfileId = await companyProfileService.GetOrCreateProfileIdAsync(cancellationToken);
            return await CalculerCompatibiliteAsync(candidateProfileId, companyProfileId, cancellationToken);
        }

        return null;
    }

    private static CompatibiliteResultView ToView(CompatibilityResult result, Dictionary<CompatibilityAxis, int> scores) =>
        new(
            result.ScoreGlobal,
            scores.Select(kv => new AxisScoreView(kv.Key, kv.Value)).ToList(),
            result.VigilancePoints.Select(v => v.Text).ToList(),
            result.CalculatedAt);
}
