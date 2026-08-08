using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Compatibility;
using Spectrometre.Core.Data;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.ProfilEntreprise.Data;
using Spectrometre.Modules.ProfilEntreprise.Entities;
using Spectrometre.Modules.ProfilEntreprise.Services;
using Spectrometre.Modules.Recrutement.Data;
using Spectrometre.Modules.Recrutement.Entities;

namespace Spectrometre.Modules.Recrutement.Services;

/// <summary>
/// Persiste guides / analyses dans <see cref="RecrutementDbContext"/>.
/// Lit poste / candidature / critères via <see cref="ProfilEntrepriseDbContext"/>
/// (évite une dépendance circulaire DI avec <see cref="IPosteService"/> qui injecte
/// <c>IRecrutementEntretienCleanup</c>).
/// Le score tags passe par <see cref="ICompatibiliteScoreService"/> (Core) — même pattern que
/// <c>PosteService</c>, sans cycle DI vers <c>IPosteService</c>.
/// </summary>
public sealed class RecrutementEntretienService(
    IDbContextFactory<RecrutementDbContext> dbFactory,
    IDbContextFactory<ProfilEntrepriseDbContext> profilDbFactory,
    ITenantContext tenantContext,
    CoreDbContext coreDb,
    IModuleRegistry moduleRegistry,
    ICompanyProfileService companyProfileService,
    ICompatibiliteScoreService compatibiliteScoreService,
    IAnalysePosteIaService analysePosteIa) : IRecrutementEntretienService
{
    private Task<RecrutementDbContext> CreateAmbientDbAsync(CancellationToken ct) =>
        CreateDbForSchemaAsync(tenantContext.SchemaName, ct);

    private async Task<RecrutementDbContext> CreateDbForSchemaAsync(string schema, CancellationToken ct)
    {
        var db = await dbFactory.CreateDbContextAsync(ct);
        db.TenantSchema = schema;
        return db;
    }

    private async Task<ProfilEntrepriseDbContext> CreateAmbientProfilDbAsync(CancellationToken ct)
    {
        var db = await profilDbFactory.CreateDbContextAsync(ct);
        db.TenantSchema = tenantContext.SchemaName;
        return db;
    }

    public async Task<GuideDeuxiemeEntrevue?> GetGuideDeuxiemeEntrevueAsync(int posteId, CancellationToken cancellationToken = default)
    {
        await using var profilDb = await CreateAmbientProfilDbAsync(cancellationToken);
        var posteExists = await profilDb.Postes.AsNoTracking().AnyAsync(p => p.Id == posteId, cancellationToken);
        if (!posteExists)
            return null;

        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var guide = await db.GuidesDeuxiemeEntrevue.AsNoTracking()
            .FirstOrDefaultAsync(g => g.PosteId == posteId, cancellationToken);

        return guide ?? new GuideDeuxiemeEntrevue { PosteId = posteId };
    }

    public async Task SaveGuideDeuxiemeEntrevueAsync(int posteId, GuideDeuxiemeEntrevue guide, CancellationToken cancellationToken = default)
    {
        await using var profilDb = await CreateAmbientProfilDbAsync(cancellationToken);
        var posteExists = await profilDb.Postes.AsNoTracking().AnyAsync(p => p.Id == posteId, cancellationToken);
        if (!posteExists)
            return;

        static string? Normalize(string? value)
        {
            var trimmed = value?.Trim();
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }

        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var existing = await db.GuidesDeuxiemeEntrevue
            .FirstOrDefaultAsync(g => g.PosteId == posteId, cancellationToken);

        if (existing is null)
        {
            db.GuidesDeuxiemeEntrevue.Add(new GuideDeuxiemeEntrevue
            {
                PosteId = posteId,
                MissionLivrables = Normalize(guide.MissionLivrables),
                SituationQuantitative = Normalize(guide.SituationQuantitative),
                SituationQualitative = Normalize(guide.SituationQualitative),
                Objectifs = Normalize(guide.Objectifs),
                Suivi = Normalize(guide.Suivi),
                Echeances = Normalize(guide.Echeances),
                AutoriteResponsabilite = Normalize(guide.AutoriteResponsabilite),
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        }
        else
        {
            existing.MissionLivrables = Normalize(guide.MissionLivrables);
            existing.SituationQuantitative = Normalize(guide.SituationQuantitative);
            existing.SituationQualitative = Normalize(guide.SituationQualitative);
            existing.Objectifs = Normalize(guide.Objectifs);
            existing.Suivi = Normalize(guide.Suivi);
            existing.Echeances = Normalize(guide.Echeances);
            existing.AutoriteResponsabilite = Normalize(guide.AutoriteResponsabilite);
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AnalyseIaView?> GetAnalyseIaAsync(int candidatureId, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var entity = await db.AnalysesIaPoste.AsNoTracking()
            .FirstOrDefaultAsync(a => a.CandidatureId == candidatureId, cancellationToken);
        return entity is null ? null : ToAnalyseIaView(entity);
    }

    public async Task<AnalyseIaView> GenererAnalyseIaAsync(int candidatureId, bool forcerRegeneration = false, CancellationToken cancellationToken = default)
    {
        await using var profilDb = await CreateAmbientProfilDbAsync(cancellationToken);
        var candidature = await profilDb.Candidatures.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == candidatureId, cancellationToken);
        if (candidature is null)
        {
            return new AnalyseIaView(
                AnalyseTexte: CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr"
                    ? "Candidature introuvable."
                    : "Application not found.",
                GenereeLe: DateTimeOffset.UtcNow,
                GenereeParIa: false);
        }

        var poste = await profilDb.Postes.AsNoTracking().FirstOrDefaultAsync(p => p.Id == candidature.PosteId, cancellationToken);
        if (poste is null)
        {
            return new AnalyseIaView(
                AnalyseTexte: CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr"
                    ? "Poste introuvable."
                    : "Job not found.",
                GenereeLe: DateTimeOffset.UtcNow,
                GenereeParIa: false);
        }

        var criteres = await profilDb.CriteresEvaluation.AsNoTracking()
            .Where(c => c.PosteId == poste.Id)
            .OrderBy(c => c.OrdreAffichage)
            .ThenBy(c => c.Categorie)
            .ThenBy(c => c.Libelle)
            .ToListAsync(cancellationToken);

        var finals = await profilDb.EvaluationsCriteresCandidature.AsNoTracking()
            .Where(e => e.CandidatureId == candidatureId && e.NiveauFinal != null)
            .ToDictionaryAsync(e => e.CritereId, e => e.NiveauFinal!.Value, cancellationToken);

        // Même pattern que PosteService.GetCandidatureAsync / UpsertCandidatureIndexAsync :
        // ICompatibiliteScoreService (Core) — pas IPosteService (cycle DI).
        CompatibiliteScoresSnapshot? scoresCompatibilite = null;
        var companyId = tenantContext.ActiveCompanyId
            ?? throw new InvalidOperationException("Aucune entreprise active — cette opération nécessite un tenant sélectionné.");
        var compatibiliteActif = await moduleRegistry.IsActiveAsync(companyId, "Compatibilite", coreDb, cancellationToken);
        if (compatibiliteActif)
        {
            var companyProfileId = await companyProfileService.GetOrCreateProfileIdAsync(cancellationToken);
            scoresCompatibilite = await compatibiliteScoreService.CalculerScoresAsync(
                candidature.CandidateProfileId, companyProfileId, cancellationToken);
        }

        var english = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "fr";
        var snapshotHash = CalculerHashAnalyse(poste, candidature, criteres, finals, scoresCompatibilite, english);

        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var existante = await db.AnalysesIaPoste
            .FirstOrDefaultAsync(a => a.CandidatureId == candidatureId, cancellationToken);

        if (!forcerRegeneration
            && existante is not null
            && existante.HashSnapshot == snapshotHash
            && !string.IsNullOrWhiteSpace(existante.AnalyseTexte))
        {
            return ToAnalyseIaView(existante);
        }

        string texte;
        var genereeParIa = false;
        string? avertissement = null;

        try
        {
            var systemPrompt = BuildAnalyseSystemPrompt(english);
            var userPrompt = BuildAnalyseUserPrompt(poste, candidature, criteres, finals, scoresCompatibilite, english);
            var (output, error) = await analysePosteIa.GenererTexteAsync(systemPrompt, userPrompt, cancellationToken);

            if (error is not null || string.IsNullOrWhiteSpace(output))
            {
                texte = BuildAnalyseFallback(poste, candidature, criteres, finals, scoresCompatibilite, english);
                avertissement = error ?? (english ? "Empty AI response." : "Réponse IA vide.");
            }
            else
            {
                texte = output.Trim();
                genereeParIa = true;
            }
        }
        catch (Exception ex)
        {
            texte = BuildAnalyseFallback(poste, candidature, criteres, finals, scoresCompatibilite, english);
            avertissement = ex.Message;
        }

        if (existante is null)
        {
            existante = new AnalyseIaPoste
            {
                PosteId = poste.Id,
                CandidatureId = candidatureId,
            };
            db.AnalysesIaPoste.Add(existante);
        }

        existante.AnalyseTexte = texte;
        existante.GenereeParIa = genereeParIa;
        existante.HashSnapshot = snapshotHash;
        existante.GenereeLe = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return ToAnalyseIaView(existante, avertissement);
    }

    public async Task DeleteDonneesEntretienPourPosteAsync(int posteId, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAmbientDbAsync(cancellationToken);

        var analyses = await db.AnalysesIaPoste
            .Where(a => a.PosteId == posteId)
            .ToListAsync(cancellationToken);
        db.AnalysesIaPoste.RemoveRange(analyses);

        var guides = await db.GuidesDeuxiemeEntrevue
            .Where(g => g.PosteId == posteId)
            .ToListAsync(cancellationToken);
        db.GuidesDeuxiemeEntrevue.RemoveRange(guides);

        if (analyses.Count > 0 || guides.Count > 0)
            await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteDonneesEntretienPourCandidatureAsync(int candidatureId, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var analyses = await db.AnalysesIaPoste
            .Where(a => a.CandidatureId == candidatureId)
            .ToListAsync(cancellationToken);
        if (analyses.Count == 0)
            return;

        db.AnalysesIaPoste.RemoveRange(analyses);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static AnalyseIaView ToAnalyseIaView(AnalyseIaPoste entity, string? avertissement = null) =>
        new(entity.AnalyseTexte, entity.GenereeLe, entity.GenereeParIa, avertissement);

    private static string CalculerHashAnalyse(
        Poste poste,
        Candidature candidature,
        IReadOnlyList<CritereEvaluation> criteres,
        IReadOnlyDictionary<int, NiveauEvaluation> finals,
        CompatibiliteScoresSnapshot? scoresCompatibilite,
        bool english)
    {
        var sb = new StringBuilder();
        sb.Append(poste.Id).Append('|').Append(poste.Titre).Append('|').Append(poste.Description ?? "")
            .Append('|').Append(candidature.Id).Append('|').Append(candidature.Statut)
            .Append('|').Append(FormatScoresForHash(scoresCompatibilite))
            .Append('|').Append(english ? "en" : "fr");
        foreach (var c in criteres)
        {
            sb.Append(';').Append(c.Id).Append(':').Append(c.Categorie).Append(':').Append(c.Libelle)
                .Append(':').Append((int)c.NiveauRequis);
            if (finals.TryGetValue(c.Id, out var niveauFinal))
                sb.Append('=').Append((int)niveauFinal);
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes);
    }

    private static string FormatScoresForHash(CompatibiliteScoresSnapshot? scores)
    {
        if (scores is null)
            return "-";

        var vigilance = scores.PointsVigilanceTags.Count == 0
            ? ""
            : string.Join(',', scores.PointsVigilanceTags.OrderBy(t => t, StringComparer.Ordinal));
        return string.Join(';',
            scores.ScoreGlobal.ToString(CultureInfo.InvariantCulture),
            scores.Technique?.ToString(CultureInfo.InvariantCulture) ?? "",
            scores.Comportementale?.ToString(CultureInfo.InvariantCulture) ?? "",
            scores.Culturelle?.ToString(CultureInfo.InvariantCulture) ?? "",
            scores.Organisationnelle?.ToString(CultureInfo.InvariantCulture) ?? "",
            scores.Motivationnelle?.ToString(CultureInfo.InvariantCulture) ?? "",
            vigilance);
    }

    private static string BuildAnalyseSystemPrompt(bool english) => english
        ? """
You are an HR assistant. Write a concise compatibility analysis between a job posting and a candidate application.
Reply in English, plain text with short paragraphs (no JSON). Be factual, professional, and actionable.
Base your analysis on BOTH the tag-based compatibility score (declared cultural/behavioural fit) AND the job criteria grid levels (skills assessed by the employer). If these two signals diverge significantly, call out that divergence explicitly in the analysis instead of silently favouring only one of them.
"""
        : """
Tu es un assistant RH. Rédige une analyse concise de compatibilité entre un poste et une candidature.
Réponds en français, texte libre en paragraphes courts (pas de JSON). Sois factuel, professionnel et actionnable.
Base ton analyse à la fois sur le score de compatibilité par tags (adéquation culturelle/comportementale déclarée) ET sur les niveaux de grille de critères (compétences vérifiées par l'entreprise) ; si les deux divergent significativement, signale explicitement cette divergence dans l'analyse plutôt que de la lisser silencieusement en faveur d'un seul signal.
""";

    private static string BuildAnalyseUserPrompt(
        Poste poste,
        Candidature candidature,
        IReadOnlyList<CritereEvaluation> criteres,
        IReadOnlyDictionary<int, NiveauEvaluation> finals,
        CompatibiliteScoresSnapshot? scoresCompatibilite,
        bool english)
    {
        var sb = new StringBuilder();
        if (english)
        {
            sb.AppendLine($"Job title: {poste.Titre}");
            sb.AppendLine($"Job description: {poste.Description ?? "(none)"}");
            sb.AppendLine($"Candidate profile id: {candidature.CandidateProfileId}");
            sb.AppendLine($"Application status: {candidature.Statut}");
            AppendCompatibilityScores(sb, scoresCompatibilite, english: true);
            sb.AppendLine("Required criteria (category / label / required / final):");
        }
        else
        {
            sb.AppendLine($"Titre du poste : {poste.Titre}");
            sb.AppendLine($"Description : {poste.Description ?? "(aucune)"}");
            sb.AppendLine($"Profil candidat id : {candidature.CandidateProfileId}");
            sb.AppendLine($"Statut candidature : {candidature.Statut}");
            AppendCompatibilityScores(sb, scoresCompatibilite, english: false);
            sb.AppendLine("Critères (catégorie / libellé / requis / final) :");
        }

        if (criteres.Count == 0)
        {
            sb.AppendLine(english ? "(no criteria defined)" : "(aucun critère défini)");
        }
        else
        {
            foreach (var c in criteres)
            {
                var finalLabel = finals.TryGetValue(c.Id, out var nf)
                    ? NiveauEvaluationLabels.Label(nf, english)
                    : (english ? "not evaluated" : "non évalué");
                sb.AppendLine($"- {c.Categorie} / {c.Libelle} / {NiveauEvaluationLabels.Label(c.NiveauRequis, english)} / {finalLabel}");
            }
        }

        sb.AppendLine(english
            ? "Produce: strengths, gaps vs required levels, how tag-compatibility relates to the grid (including any divergence), and 2-4 interview recommendations."
            : "Produis : points forts, écarts vs niveaux requis, lien entre compatibilité par tags et grille (y compris toute divergence), et 2 à 4 recommandations d'entretien.");
        return sb.ToString();
    }

    private static void AppendCompatibilityScores(StringBuilder sb, CompatibiliteScoresSnapshot? scores, bool english)
    {
        if (scores is null)
        {
            sb.AppendLine(english
                ? "Compatibility score: n/a"
                : "Score de compatibilité : n/d");
            return;
        }

        if (english)
        {
            sb.AppendLine($"Compatibility score (tags): {scores.ScoreGlobal}%");
            sb.AppendLine(
                $"Axis scores — Technical: {FormatAxis(scores.Technique, english: true)}, Behavioural: {FormatAxis(scores.Comportementale, english: true)}, Cultural: {FormatAxis(scores.Culturelle, english: true)}, Organisational: {FormatAxis(scores.Organisationnelle, english: true)}, Motivational: {FormatAxis(scores.Motivationnelle, english: true)}");
            if (scores.PointsVigilanceTags.Count > 0)
                sb.AppendLine($"Shared vigilance tags: {string.Join(", ", scores.PointsVigilanceTags)}");
        }
        else
        {
            sb.AppendLine($"Score de compatibilité (tags) : {scores.ScoreGlobal}%");
            sb.AppendLine(
                $"Scores par axe — Technique : {FormatAxis(scores.Technique, english: false)}, Comportementale : {FormatAxis(scores.Comportementale, english: false)}, Culturelle : {FormatAxis(scores.Culturelle, english: false)}, Organisationnelle : {FormatAxis(scores.Organisationnelle, english: false)}, Motivationnelle : {FormatAxis(scores.Motivationnelle, english: false)}");
            if (scores.PointsVigilanceTags.Count > 0)
                sb.AppendLine($"Points de vigilance partagés : {string.Join(", ", scores.PointsVigilanceTags)}");
        }
    }

    private static string FormatAxis(int? score, bool english) =>
        score is int s ? $"{s}%" : (english ? "n/a" : "n/d");

    private static string BuildAnalyseFallback(
        Poste poste,
        Candidature candidature,
        IReadOnlyList<CritereEvaluation> criteres,
        IReadOnlyDictionary<int, NiveauEvaluation> finals,
        CompatibiliteScoresSnapshot? scoresCompatibilite,
        bool english)
    {
        var sb = new StringBuilder();
        if (english)
        {
            sb.AppendLine($"Local analysis for « {poste.Titre} » (candidate #{candidature.CandidateProfileId}).");
            if (scoresCompatibilite is { } scores)
            {
                sb.AppendLine($"Compatibility score available: {scores.ScoreGlobal}%.");
                sb.AppendLine(
                    $"Axis scores — Technical: {FormatAxis(scores.Technique, english: true)}, Behavioural: {FormatAxis(scores.Comportementale, english: true)}, Cultural: {FormatAxis(scores.Culturelle, english: true)}, Organisational: {FormatAxis(scores.Organisationnelle, english: true)}, Motivational: {FormatAxis(scores.Motivationnelle, english: true)}.");
            }
            else
            {
                sb.AppendLine("No compatibility score available for this application.");
            }

            sb.AppendLine(criteres.Count == 0
                ? "No skill criteria defined on this job — complete the job profile, then regenerate."
                : $"{criteres.Count} criterion(a) defined; {finals.Count} with a final evaluation level.");
            sb.AppendLine("AI generation was unavailable — this summary was produced locally without an external model.");
        }
        else
        {
            sb.AppendLine($"Analyse locale pour « {poste.Titre} » (candidat #{candidature.CandidateProfileId}).");
            if (scoresCompatibilite is { } scores)
            {
                sb.AppendLine($"Score de compatibilité disponible : {scores.ScoreGlobal}%.");
                sb.AppendLine(
                    $"Scores par axe — Technique : {FormatAxis(scores.Technique, english: false)}, Comportementale : {FormatAxis(scores.Comportementale, english: false)}, Culturelle : {FormatAxis(scores.Culturelle, english: false)}, Organisationnelle : {FormatAxis(scores.Organisationnelle, english: false)}, Motivationnelle : {FormatAxis(scores.Motivationnelle, english: false)}.");
            }
            else
            {
                sb.AppendLine("Aucun score de compatibilité disponible pour cette candidature.");
            }

            sb.AppendLine(criteres.Count == 0
                ? "Aucun critère de compétence défini sur ce poste — complétez le profil du poste puis régénérez."
                : $"{criteres.Count} critère(s) défini(s) ; {finals.Count} avec un niveau final renseigné.");
            sb.AppendLine("La génération IA était indisponible — ce résumé a été produit localement sans modèle externe.");
        }

        return sb.ToString().Trim();
    }
}
