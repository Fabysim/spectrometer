using Spectrometre.Core.Recruitment;
using Spectrometre.Core.Tenancy;

namespace Spectrometre.Modules.Analytics.Services;

/// <summary>Voir <see cref="IAnalyticsService"/> pour le principe (pur agrégateur sur l'index partagé, scopé à l'entreprise active).</summary>
public sealed class AnalyticsService(IRecruitmentIndexService recruitmentIndex, ITenantContext tenantContext) : IAnalyticsService
{
    private const int TopPointsDeVigilanceCount = 5;

    /// <summary>
    /// Ordre métier du funnel (pas alphabétique) — les valeurs correspondent aux chaînes produites par
    /// <c>PostesRecrutement.Entities.CandidatureStatut.ToString()</c>. Couplage assumé aux mêmes chaînes que
    /// <c>CandidatureIndexEntry.Statut</c> (voir son commentaire : le noyau ne référence aucun type de
    /// module, donc le statut y est déjà une chaîne) — un statut inconnu de cette liste (aucun aujourd'hui)
    /// serait simplement absent du funnel plutôt que de faire échouer l'agrégation.
    /// </summary>
    private static readonly (string Statut, string Libelle)[] OrdreFunnel =
    [
        ("Recue", "Reçues"),
        ("EnRevue", "En revue"),
        ("Entretien", "Entretien"),
        ("Rejetee", "Rejetées"),
        ("Embauchee", "Embauchées"),
    ];

    private int ActiveCompanyId =>
        tenantContext.ActiveCompanyId ?? throw new InvalidOperationException("Aucune entreprise active — le tableau de bord Analytics nécessite un tenant sélectionné.");

    public async Task<AnalyticsDashboardView> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var companyId = ActiveCompanyId;

        var postes = await recruitmentIndex.GetPostesPourEntrepriseAsync(companyId, cancellationToken);
        var candidatures = await recruitmentIndex.GetCandidaturesPourEntrepriseAsync(companyId, cancellationToken);

        var postesOuverts = postes.Count(p => p.Statut == "Ouvert");
        var postesFermes = postes.Count - postesOuverts;

        var comptesParStatut = candidatures
            .GroupBy(c => c.Statut)
            .ToDictionary(g => g.Key, g => g.Count());
        var funnel = OrdreFunnel
            .Select(o => new FunnelStatutView(o.Statut, o.Libelle, comptesParStatut.GetValueOrDefault(o.Statut)))
            .ToList();

        var scoresGlobaux = candidatures.Where(c => c.ScoreCompatibilite is not null).Select(c => c.ScoreCompatibilite!.Value).ToList();
        int? scoreMoyenGlobal = scoresGlobaux.Count > 0 ? (int)Math.Round(scoresGlobaux.Average()) : null;

        var moyennesParAxe = new List<AxisMoyenneView>
        {
            MoyenneAxe("Technique", candidatures.Select(c => c.AxisScores.Technique)),
            MoyenneAxe("Comportementale", candidatures.Select(c => c.AxisScores.Comportementale)),
            MoyenneAxe("Culturelle", candidatures.Select(c => c.AxisScores.Culturelle)),
            MoyenneAxe("Organisationnelle", candidatures.Select(c => c.AxisScores.Organisationnelle)),
            MoyenneAxe("Motivationnelle", candidatures.Select(c => c.AxisScores.Motivationnelle)),
        };

        var topPointsDeVigilance = candidatures
            .SelectMany(c => c.PointsVigilanceTags)
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .Select(g => new TagFrequenceView(g.Key, g.Count()))
            .OrderByDescending(t => t.Nombre)
            .ThenBy(t => t.Tag, StringComparer.OrdinalIgnoreCase)
            .Take(TopPointsDeVigilanceCount)
            .ToList();

        var candidatsUniques = candidatures.Select(c => c.CandidateProfileId).Distinct().Count();
        var candidatsAvecGrilleComplete = candidatures
            .GroupBy(c => c.CandidateProfileId)
            .Count(g => g.Any(c => c.GrilleCandidatComplete));
        double? tauxCompletion = candidatsUniques > 0 ? 100.0 * candidatsAvecGrilleComplete / candidatsUniques : null;

        return new AnalyticsDashboardView(
            postesOuverts, postesFermes, candidatures.Count, funnel, scoreMoyenGlobal, moyennesParAxe,
            topPointsDeVigilance, candidatsUniques, candidatsAvecGrilleComplete, tauxCompletion);
    }

    /// <summary>Moyenne ignorant les candidatures sans score sur cet axe (Compatibilite inactif ou pas encore calculé) — <c>null</c> si aucune n'en a.</summary>
    private static AxisMoyenneView MoyenneAxe(string label, IEnumerable<int?> scores)
    {
        var valeurs = scores.Where(s => s is not null).Select(s => s!.Value).ToList();
        return new AxisMoyenneView(label, valeurs.Count > 0 ? Math.Round(valeurs.Average(), 1) : null);
    }
}
