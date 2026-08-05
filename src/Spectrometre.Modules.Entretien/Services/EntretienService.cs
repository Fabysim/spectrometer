using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Compatibility;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Compatibilite.Entities;
using Spectrometre.Modules.Compatibilite.Services;
using Spectrometre.Modules.Entretien.Data;
using Spectrometre.Modules.Entretien.Entities;

namespace Spectrometre.Modules.Entretien.Services;

/// <summary>Voir le commentaire équivalent sur <c>CompanyProfileService</c> : DbContext tenant-scopé, instancié via la factory à chaque opération, schéma affecté après coup depuis <see cref="ITenantContext"/>.</summary>
public sealed class EntretienService(
    IDbContextFactory<EntretienDbContext> dbFactory,
    ITenantContext tenantContext,
    ICompatibiliteService compatibiliteService) : IEntretienService
{
    private async Task<EntretienDbContext> CreateDbAsync(CancellationToken cancellationToken)
    {
        var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        db.TenantSchema = tenantContext.SchemaName;
        return db;
    }

    public async Task<GrilleEntretienView?> GenererGrilleAsync(int candidateProfileId, string requestingUserId, CancellationToken cancellationToken = default)
    {
        // Seul point d'entrée pour obtenir le résultat sous-jacent : délègue tout le contrôle d'accès à
        // Compatibilite (voir IEntretienService). Si null, on relaie null tel quel — jamais de tentative
        // de génération "partielle" ou de contournement.
        var resultat = await compatibiliteService.GetResultatAutorisePourUtilisateurAsync(candidateProfileId, requestingUserId, cancellationToken);
        if (resultat is null)
            return null;

        // GetResultatAutorisePourUtilisateurAsync a déjà positionné TenantContext sur la BONNE entreprise
        // (celle pour laquelle l'accès a été validé) avant de retourner un résultat non nul — voir son
        // commentaire dans CompatibiliteService : c'est cette entreprise dont on lit les gabarits ici.
        await using var db = await CreateDbAsync(cancellationToken);

        var seuil = (await db.InterviewGenerationSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken))
            ?.SeuilAxeFaiblePercent ?? 60;
        var templates = await db.QuestionTemplates.AsNoTracking().OrderBy(t => t.DisplayOrder).ToListAsync(cancellationToken);

        var groupes = new List<QuestionGroupeView>();

        foreach (var axisScore in resultat.ScoresParAxe)
        {
            if (axisScore.Score >= seuil)
                continue; // axe déjà élevé : pas de bruit, aucune question générée pour lui.

            var parametres = new Dictionary<string, string>
            {
                ["axe"] = CompatibilityAxisLabels.Label(axisScore.Axis),
                ["score"] = axisScore.Score.ToString(),
            };
            if (axisScore.Axis == CompatibilityAxis.Organisationnelle)
            {
                parametres["rythmeCandidat"] = LabelRythme(resultat.RythmeCandidat);
                parametres["rythmeEntreprise"] = LabelRythme(resultat.RythmeEntreprise);
            }

            var pourCandidat = RendreTous(templates, QuestionTemplateType.Axe, axisScore.Axis, QuestionSens.EntrepriseVersCandidat, parametres);
            var pourEntreprise = RendreTous(templates, QuestionTemplateType.Axe, axisScore.Axis, QuestionSens.CandidatVersEntreprise, parametres);
            if (pourCandidat.Count == 0 && pourEntreprise.Count == 0)
                continue;

            groupes.Add(new QuestionGroupeView(
                $"Axe {CompatibilityAxisLabels.Label(axisScore.Axis)} ({axisScore.Score}%)",
                axisScore.Score,
                pourCandidat,
                pourEntreprise));
        }

        foreach (var tag in resultat.PointsVigilanceTagsPartages)
        {
            var parametres = new Dictionary<string, string> { ["tag"] = tag };

            var pourCandidat = RendreTous(templates, QuestionTemplateType.PointVigilance, null, QuestionSens.EntrepriseVersCandidat, parametres);
            var pourEntreprise = RendreTous(templates, QuestionTemplateType.PointVigilance, null, QuestionSens.CandidatVersEntreprise, parametres);
            if (pourCandidat.Count == 0 && pourEntreprise.Count == 0)
                continue;

            groupes.Add(new QuestionGroupeView($"Point de vigilance : {tag}", null, pourCandidat, pourEntreprise));
        }

        return new GrilleEntretienView(candidateProfileId, resultat.ScoreGlobal, groupes);
    }

    private static List<string> RendreTous(
        IReadOnlyList<QuestionTemplate> templates,
        QuestionTemplateType type,
        CompatibilityAxis? axis,
        QuestionSens sens,
        IReadOnlyDictionary<string, string> parametres) =>
        templates
            .Where(t => t.Type == type && t.Axis == axis && t.Sens == sens)
            .Select(t => Rendre(t.Gabarit, parametres))
            .ToList();

    /// <summary>Substitution <c>{nom}</c> volontairement simple — voir le commentaire sur <c>QuestionTemplate</c> : la donnée doit rester éditable sans compétence de développement.</summary>
    private static string Rendre(string gabarit, IReadOnlyDictionary<string, string> parametres)
    {
        var texte = gabarit;
        foreach (var (cle, valeur) in parametres)
            texte = texte.Replace("{" + cle + "}", valeur);
        return texte;
    }

    private static string LabelRythme(int? rythme) =>
        rythme is int r && CompatibilityVocabulary.RythmeTravailLabels.TryGetValue(r, out var label)
            ? label
            : "non renseigné";
}
