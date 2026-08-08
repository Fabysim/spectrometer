using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Data;
using Spectrometre.Core.Invitations;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Recruitment;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Compatibilite.Entities;
using Spectrometre.Modules.Compatibilite.Services;
using Spectrometre.Modules.PostesRecrutement.Data;
using Spectrometre.Modules.PostesRecrutement.Entities;
using Spectrometre.Modules.ProfilCandidat.Services;
using Spectrometre.Modules.ProfilEntreprise.Services;

namespace Spectrometre.Modules.PostesRecrutement.Services;

/// <summary>
/// DbContext tenant-scopé instancié via <c>IDbContextFactory</c> (voir le commentaire détaillé sur
/// <c>CompanyProfileService</c>) : soit avec le schéma du tenant ambiant (gestion de ses propres postes),
/// soit avec le schéma explicite d'une autre entreprise (candidature d'un candidat, une opération qui
/// traverse plusieurs tenants et ne peut donc pas s'appuyer sur <see cref="ITenantContext"/>).
/// </summary>
/// <remarks>
/// Écrit dans <see cref="IRecruitmentIndexService"/> (schéma <c>core</c>) à chaque poste créé/modifié/
/// supprimé, candidature créée ou statut changé — c'est cet index, pas une traversée des schémas tenant,
/// que lit désormais <see cref="GetPostesOuvertsAsync"/> (voir le commentaire sur cette méthode pour l'historique).
/// </remarks>
public sealed class PosteService(
    IDbContextFactory<PostesRecrutementDbContext> dbFactory,
    ITenantContext tenantContext,
    CoreDbContext coreDb,
    IModuleRegistry moduleRegistry,
    ICompanyProfileService companyProfileService,
    ICandidateProfileService candidateProfileService,
    ICompatibiliteService compatibiliteService,
    IRecruitmentIndexService recruitmentIndex,
    IAnalysePosteIaService analysePosteIa,
    IInvitationService invitationService) : IPosteService
{
    private Task<PostesRecrutementDbContext> CreateAmbientDbAsync(CancellationToken ct) =>
        CreateDbForSchemaAsync(tenantContext.SchemaName, ct);

    private async Task<PostesRecrutementDbContext> CreateDbForSchemaAsync(string schema, CancellationToken ct)
    {
        var db = await dbFactory.CreateDbContextAsync(ct);
        db.TenantSchema = schema;
        return db;
    }

    private async Task<Company> GetActiveCompanyAsync(CancellationToken ct)
    {
        var companyId = tenantContext.ActiveCompanyId
            ?? throw new InvalidOperationException("Aucune entreprise active — cette opération nécessite un tenant sélectionné.");
        return await coreDb.Companies.AsNoTracking().FirstAsync(c => c.Id == companyId, ct);
    }

    public async Task<int> CreatePosteAsync(
        string titre,
        string? description,
        string? departement,
        string? tachesDescription = null,
        string? salaire = null,
        string? avantages = null,
        DateTimeOffset? dateCloture = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var poste = new Poste
        {
            Titre = titre.Trim(),
            Description = NormalizeOptional(description),
            Departement = NormalizeOptional(departement),
            TachesDescription = NormalizeOptional(tachesDescription),
            Salaire = NormalizeOptional(salaire),
            Avantages = NormalizeOptional(avantages),
            DateCloture = dateCloture,
        };
        db.Postes.Add(poste);
        await db.SaveChangesAsync(cancellationToken);

        var company = await GetActiveCompanyAsync(cancellationToken);
        await recruitmentIndex.UpsertPosteAsync(company.Id, company.Name, poste.Id, poste.Titre, poste.Description, poste.Departement, poste.Statut.ToString(), cancellationToken);

        return poste.Id;
    }

    public async Task<IReadOnlyList<PosteView>> GetPostesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAmbientDbAsync(cancellationToken);
        return await db.Postes
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PosteView(
                p.Id, p.Titre, p.Description, p.Departement, p.Statut, p.CreatedAt,
                p.TachesDescription, p.Salaire, p.Avantages, p.DateCloture))
            .ToListAsync(cancellationToken);
    }

    public async Task UpdatePosteAsync(
        int posteId,
        string titre,
        string? description,
        string? departement,
        string? tachesDescription = null,
        string? salaire = null,
        string? avantages = null,
        DateTimeOffset? dateCloture = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var poste = await db.Postes.FirstOrDefaultAsync(p => p.Id == posteId, cancellationToken);
        if (poste is null) return;

        poste.Titre = titre.Trim();
        poste.Description = NormalizeOptional(description);
        poste.Departement = NormalizeOptional(departement);
        poste.TachesDescription = NormalizeOptional(tachesDescription);
        poste.Salaire = NormalizeOptional(salaire);
        poste.Avantages = NormalizeOptional(avantages);
        poste.DateCloture = dateCloture;
        await db.SaveChangesAsync(cancellationToken);

        var company = await GetActiveCompanyAsync(cancellationToken);
        await recruitmentIndex.UpsertPosteAsync(company.Id, company.Name, poste.Id, poste.Titre, poste.Description, poste.Departement, poste.Statut.ToString(), cancellationToken);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public async Task SetPosteStatutAsync(int posteId, PosteStatut statut, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var poste = await db.Postes.FirstOrDefaultAsync(p => p.Id == posteId, cancellationToken);
        if (poste is null) return;

        poste.Statut = statut;
        await db.SaveChangesAsync(cancellationToken);

        var company = await GetActiveCompanyAsync(cancellationToken);
        await recruitmentIndex.UpsertPosteAsync(company.Id, company.Name, poste.Id, poste.Titre, poste.Description, poste.Departement, poste.Statut.ToString(), cancellationToken);
    }

    public async Task DeletePosteAsync(int posteId, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var poste = await db.Postes.FirstOrDefaultAsync(p => p.Id == posteId, cancellationToken);
        if (poste is null) return;

        // Pas de FK Candidature/Critere → Poste dans le modèle : suppression manuelle des dépendances.
        var candidatureIds = await db.Candidatures
            .Where(c => c.PosteId == posteId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        if (candidatureIds.Count > 0)
        {
            var analyses = await db.AnalysesIaPoste
                .Where(a => a.PosteId == posteId || candidatureIds.Contains(a.CandidatureId))
                .ToListAsync(cancellationToken);
            db.AnalysesIaPoste.RemoveRange(analyses);

            var evaluations = await db.EvaluationsCriteresCandidature
                .Where(e => candidatureIds.Contains(e.CandidatureId))
                .ToListAsync(cancellationToken);
            db.EvaluationsCriteresCandidature.RemoveRange(evaluations);

            var candidatures = await db.Candidatures
                .Where(c => c.PosteId == posteId)
                .ToListAsync(cancellationToken);
            db.Candidatures.RemoveRange(candidatures);
        }

        var criteres = await db.CriteresEvaluation
            .Where(c => c.PosteId == posteId)
            .ToListAsync(cancellationToken);
        if (criteres.Count > 0)
        {
            var critereIds = criteres.Select(c => c.Id).ToList();
            var evaluationsCritere = await db.EvaluationsCriteresCandidature
                .Where(e => critereIds.Contains(e.CritereId))
                .ToListAsync(cancellationToken);
            db.EvaluationsCriteresCandidature.RemoveRange(evaluationsCritere);
            db.CriteresEvaluation.RemoveRange(criteres);
        }

        var guides = await db.GuidesDeuxiemeEntrevue
            .Where(g => g.PosteId == posteId)
            .ToListAsync(cancellationToken);
        db.GuidesDeuxiemeEntrevue.RemoveRange(guides);

        db.Postes.Remove(poste);
        await db.SaveChangesAsync(cancellationToken);

        var company = await GetActiveCompanyAsync(cancellationToken);
        await recruitmentIndex.RemovePosteAsync(company.Id, posteId, cancellationToken);

        var emetteurIds = await coreDb.UserCompanyLinks.AsNoTracking()
            .Where(l => l.CompanyId == company.Id)
            .Select(l => l.UserId)
            .ToListAsync(cancellationToken);

        var invitations = await coreDb.Invitations
            .Where(i => i.Type == InvitationType.CandidaturePoste
                && i.ContextId == posteId
                && i.Statut == InvitationStatus.EnAttente
                && emetteurIds.Contains(i.EmetteurUserId))
            .ToListAsync(cancellationToken);
        foreach (var invitation in invitations)
            invitation.Statut = InvitationStatus.Revoquee;
        if (invitations.Count > 0)
            await coreDb.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CandidatureView>> GetCandidaturesAsync(int posteId, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var candidatures = await db.Candidatures
            .Where(c => c.PosteId == posteId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var poste = await db.Postes.AsNoTracking().FirstOrDefaultAsync(p => p.Id == posteId, cancellationToken);
        var company = await GetActiveCompanyAsync(cancellationToken);

        // Intégration légère avec le Moteur de Compatibilité : uniquement si le module est actif pour ce
        // tenant, jamais de dépendance dure — le module reste utilisable sans Compatibilite activé.
        var compatibiliteActif = await moduleRegistry.IsActiveAsync(company.Id, "Compatibilite", coreDb, cancellationToken);
        int? companyProfileId = compatibiliteActif ? await companyProfileService.GetOrCreateProfileIdAsync(cancellationToken) : null;

        var result = new List<CandidatureView>();
        foreach (var c in candidatures)
        {
            CompatibiliteResultView? calcul = null;
            if (compatibiliteActif && companyProfileId is int cpid)
                calcul = await compatibiliteService.CalculerCompatibiliteAsync(c.CandidateProfileId, cpid, cancellationToken);

            result.Add(new CandidatureView(c.Id, c.PosteId, c.CandidateProfileId, c.Statut, c.CreatedAt, calcul?.ScoreGlobal, c.EstPreselectionne));

            // Recalcul de compatibilité = un des deux évènements qui doivent tenir l'index à jour (voir
            // IRecruitmentIndexService) : dès que ce score est (re)connu, on le reporte dans l'index lu
            // par le Vivier et le tableau de bord Analytics, sans attendre un futur changement de statut.
            if (poste is not null)
                await UpsertCandidatureIndexAsync(c, poste.Titre, company.Id, calcul, cancellationToken);
        }

        return result;
    }

    public async Task<CandidatureView?> GetCandidatureAsync(int candidatureId, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var candidature = await db.Candidatures.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == candidatureId, cancellationToken);
        if (candidature is null)
            return null;

        var poste = await db.Postes.AsNoTracking().FirstOrDefaultAsync(p => p.Id == candidature.PosteId, cancellationToken);
        var company = await GetActiveCompanyAsync(cancellationToken);

        var compatibiliteActif = await moduleRegistry.IsActiveAsync(company.Id, "Compatibilite", coreDb, cancellationToken);
        CompatibiliteResultView? calcul = null;
        if (compatibiliteActif)
        {
            var companyProfileId = await companyProfileService.GetOrCreateProfileIdAsync(cancellationToken);
            calcul = await compatibiliteService.CalculerCompatibiliteAsync(candidature.CandidateProfileId, companyProfileId, cancellationToken);
        }

        if (poste is not null)
            await UpsertCandidatureIndexAsync(candidature, poste.Titre, company.Id, calcul, cancellationToken);

        return new CandidatureView(
            candidature.Id, candidature.PosteId, candidature.CandidateProfileId,
            candidature.Statut, candidature.CreatedAt, calcul?.ScoreGlobal, candidature.EstPreselectionne);
    }

    public async Task SetCandidatureStatutAsync(int candidatureId, CandidatureStatut statut, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var candidature = await db.Candidatures.FirstOrDefaultAsync(c => c.Id == candidatureId, cancellationToken);
        if (candidature is null) return;

        candidature.Statut = statut;
        candidature.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var poste = await db.Postes.AsNoTracking().FirstOrDefaultAsync(p => p.Id == candidature.PosteId, cancellationToken);
        if (poste is null) return;

        var company = await GetActiveCompanyAsync(cancellationToken);
        // Le tenant ambiant est bien celui de cette candidature (opération côté entreprise) : sûr d'appeler
        // le Moteur de Compatibilité ici, à la différence de PostulerAsync qui traverse un AUTRE tenant.
        await UpsertCandidatureIndexAsync(candidature, poste.Titre, company.Id, precomputed: null, cancellationToken);
    }

    public async Task SetPreselectionAsync(int candidatureId, bool preselectionne, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var candidature = await db.Candidatures.FirstOrDefaultAsync(c => c.Id == candidatureId, cancellationToken);
        if (candidature is null)
            return;

        candidature.EstPreselectionne = preselectionne;
        candidature.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CritereEvaluationView>> GetCriteresAsync(int posteId, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAmbientDbAsync(cancellationToken);
        return await db.CriteresEvaluation
            .AsNoTracking()
            .Where(c => c.PosteId == posteId)
            .OrderBy(c => c.OrdreAffichage)
            .ThenBy(c => c.Categorie)
            .ThenBy(c => c.Libelle)
            .Select(c => new CritereEvaluationView(c.Id, c.PosteId, c.Categorie, c.Libelle, c.NiveauRequis, c.OrdreAffichage))
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertCritereAsync(int posteId, int? id, string categorie, string libelle, int niveauRequis, int ordreAffichage, CancellationToken cancellationToken = default)
    {
        categorie = (categorie ?? string.Empty).Trim();
        libelle = (libelle ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(categorie) || string.IsNullOrWhiteSpace(libelle))
            return;

        var niveau = (NiveauEvaluation)Math.Clamp(niveauRequis, 0, 4);

        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var posteExists = await db.Postes.AsNoTracking().AnyAsync(p => p.Id == posteId, cancellationToken);
        if (!posteExists)
            return;

        if (id is int existingId)
        {
            var critere = await db.CriteresEvaluation.FirstOrDefaultAsync(c => c.Id == existingId && c.PosteId == posteId, cancellationToken);
            if (critere is null)
                return;

            critere.Categorie = categorie;
            critere.Libelle = libelle;
            critere.NiveauRequis = niveau;
            critere.OrdreAffichage = ordreAffichage;
        }
        else
        {
            db.CriteresEvaluation.Add(new CritereEvaluation
            {
                PosteId = posteId,
                Categorie = categorie,
                Libelle = libelle,
                NiveauRequis = niveau,
                OrdreAffichage = ordreAffichage,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteCritereAsync(int critereId, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var critere = await db.CriteresEvaluation.FirstOrDefaultAsync(c => c.Id == critereId, cancellationToken);
        if (critere is null)
            return;

        db.CriteresEvaluation.Remove(critere);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EvaluationCritereView>> GetEvaluationCriteresAsync(int candidatureId, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var candidature = await db.Candidatures.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == candidatureId, cancellationToken);
        if (candidature is null)
            return [];

        var criteres = await db.CriteresEvaluation.AsNoTracking()
            .Where(c => c.PosteId == candidature.PosteId)
            .OrderBy(c => c.OrdreAffichage)
            .ThenBy(c => c.Categorie)
            .ThenBy(c => c.Libelle)
            .ToListAsync(cancellationToken);

        var finals = await db.EvaluationsCriteresCandidature.AsNoTracking()
            .Where(e => e.CandidatureId == candidatureId)
            .ToDictionaryAsync(e => e.CritereId, e => e.NiveauFinal, cancellationToken);

        return criteres
            .Select(c => new EvaluationCritereView(
                c.Id,
                c.Categorie,
                c.Libelle,
                c.NiveauRequis,
                finals.TryGetValue(c.Id, out var niveau) ? niveau : null,
                c.OrdreAffichage))
            .ToList();
    }

    public async Task SetNiveauFinalAsync(int candidatureId, int critereId, int niveauFinal, CancellationToken cancellationToken = default)
    {
        var niveau = (NiveauEvaluation)Math.Clamp(niveauFinal, 0, 4);

        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var candidature = await db.Candidatures.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == candidatureId, cancellationToken);
        if (candidature is null)
            return;

        var critereAppartientAuPoste = await db.CriteresEvaluation.AsNoTracking()
            .AnyAsync(c => c.Id == critereId && c.PosteId == candidature.PosteId, cancellationToken);
        if (!critereAppartientAuPoste)
            return;

        var existing = await db.EvaluationsCriteresCandidature
            .FirstOrDefaultAsync(e => e.CandidatureId == candidatureId && e.CritereId == critereId, cancellationToken);

        if (existing is null)
        {
            db.EvaluationsCriteresCandidature.Add(new EvaluationCritereCandidature
            {
                CandidatureId = candidatureId,
                CritereId = critereId,
                NiveauFinal = niveau,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        }
        else
        {
            existing.NiveauFinal = niveau;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<GuideDeuxiemeEntrevue?> GetGuideDeuxiemeEntrevueAsync(int posteId, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var posteExists = await db.Postes.AsNoTracking().AnyAsync(p => p.Id == posteId, cancellationToken);
        if (!posteExists)
            return null;

        var guide = await db.GuidesDeuxiemeEntrevue.AsNoTracking()
            .FirstOrDefaultAsync(g => g.PosteId == posteId, cancellationToken);

        return guide ?? new GuideDeuxiemeEntrevue { PosteId = posteId };
    }

    public async Task SaveGuideDeuxiemeEntrevueAsync(int posteId, GuideDeuxiemeEntrevue guide, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var posteExists = await db.Postes.AsNoTracking().AnyAsync(p => p.Id == posteId, cancellationToken);
        if (!posteExists)
            return;

        static string? Normalize(string? value)
        {
            var trimmed = value?.Trim();
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }

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
        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var candidature = await db.Candidatures.AsNoTracking()
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

        var poste = await db.Postes.AsNoTracking().FirstOrDefaultAsync(p => p.Id == candidature.PosteId, cancellationToken);
        if (poste is null)
        {
            return new AnalyseIaView(
                AnalyseTexte: CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr"
                    ? "Poste introuvable."
                    : "Job not found.",
                GenereeLe: DateTimeOffset.UtcNow,
                GenereeParIa: false);
        }

        var criteres = await db.CriteresEvaluation.AsNoTracking()
            .Where(c => c.PosteId == poste.Id)
            .OrderBy(c => c.OrdreAffichage)
            .ThenBy(c => c.Categorie)
            .ThenBy(c => c.Libelle)
            .ToListAsync(cancellationToken);

        var finals = await db.EvaluationsCriteresCandidature.AsNoTracking()
            .Where(e => e.CandidatureId == candidatureId)
            .ToDictionaryAsync(e => e.CritereId, e => e.NiveauFinal, cancellationToken);

        var candidatureView = await GetCandidatureAsync(candidatureId, cancellationToken);
        var score = candidatureView?.ScoreCompatibilite;
        var english = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName != "fr";
        var snapshotHash = CalculerHashAnalyse(poste, candidature, criteres, finals, score, english);

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
            var userPrompt = BuildAnalyseUserPrompt(poste, candidature, criteres, finals, score, english);
            var (output, error) = await analysePosteIa.GenererTexteAsync(systemPrompt, userPrompt, cancellationToken);

            if (error is not null || string.IsNullOrWhiteSpace(output))
            {
                texte = BuildAnalyseFallback(poste, candidature, criteres, finals, score, english);
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
            // Filet de sécurité : IAnalysePosteIaService ne doit pas lever, mais on ne remonte jamais
            // d'exception jusqu'à l'UI (même pattern que GenererSyntheseAsync GDT).
            texte = BuildAnalyseFallback(poste, candidature, criteres, finals, score, english);
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

    private static AnalyseIaView ToAnalyseIaView(AnalyseIaPoste entity, string? avertissement = null) =>
        new(entity.AnalyseTexte, entity.GenereeLe, entity.GenereeParIa, avertissement);

    private static string CalculerHashAnalyse(
        Poste poste,
        Candidature candidature,
        IReadOnlyList<CritereEvaluation> criteres,
        IReadOnlyDictionary<int, NiveauEvaluation> finals,
        int? scoreCompatibilite,
        bool english)
    {
        var sb = new StringBuilder();
        sb.Append(poste.Id).Append('|').Append(poste.Titre).Append('|').Append(poste.Description ?? "")
            .Append('|').Append(candidature.Id).Append('|').Append(candidature.Statut)
            .Append('|').Append(scoreCompatibilite?.ToString() ?? "-")
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

    private static string BuildAnalyseSystemPrompt(bool english) => english
        ? """
You are an HR assistant. Write a concise compatibility analysis between a job posting and a candidate application.
Reply in English, plain text with short paragraphs (no JSON). Be factual, professional, and actionable.
"""
        : """
Tu es un assistant RH. Rédige une analyse concise de compatibilité entre un poste et une candidature.
Réponds en français, texte libre en paragraphes courts (pas de JSON). Sois factuel, professionnel et actionnable.
""";

    private static string BuildAnalyseUserPrompt(
        Poste poste,
        Candidature candidature,
        IReadOnlyList<CritereEvaluation> criteres,
        IReadOnlyDictionary<int, NiveauEvaluation> finals,
        int? scoreCompatibilite,
        bool english)
    {
        var sb = new StringBuilder();
        if (english)
        {
            sb.AppendLine($"Job title: {poste.Titre}");
            sb.AppendLine($"Job description: {poste.Description ?? "(none)"}");
            sb.AppendLine($"Candidate profile id: {candidature.CandidateProfileId}");
            sb.AppendLine($"Application status: {candidature.Statut}");
            sb.AppendLine($"Compatibility score: {(scoreCompatibilite is int s ? $"{s}%" : "n/a")}");
            sb.AppendLine("Required criteria (category / label / required / final):");
        }
        else
        {
            sb.AppendLine($"Titre du poste : {poste.Titre}");
            sb.AppendLine($"Description : {poste.Description ?? "(aucune)"}");
            sb.AppendLine($"Profil candidat id : {candidature.CandidateProfileId}");
            sb.AppendLine($"Statut candidature : {candidature.Statut}");
            sb.AppendLine($"Score de compatibilité : {(scoreCompatibilite is int s ? $"{s}%" : "n/d")}");
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
            ? "Produce: strengths, gaps vs required levels, and 2-4 interview recommendations."
            : "Produis : points forts, écarts vs niveaux requis, et 2 à 4 recommandations d'entretien.");
        return sb.ToString();
    }

    private static string BuildAnalyseFallback(
        Poste poste,
        Candidature candidature,
        IReadOnlyList<CritereEvaluation> criteres,
        IReadOnlyDictionary<int, NiveauEvaluation> finals,
        int? scoreCompatibilite,
        bool english)
    {
        var sb = new StringBuilder();
        if (english)
        {
            sb.AppendLine($"Local analysis for « {poste.Titre} » (candidate #{candidature.CandidateProfileId}).");
            sb.AppendLine(scoreCompatibilite is int s
                ? $"Compatibility score available: {s}%."
                : "No compatibility score available for this application.");
            sb.AppendLine(criteres.Count == 0
                ? "No skill criteria defined on this job — complete the job profile, then regenerate."
                : $"{criteres.Count} criterion(a) defined; {finals.Count} with a final evaluation level.");
            sb.AppendLine("AI generation was unavailable — this summary was produced locally without an external model.");
        }
        else
        {
            sb.AppendLine($"Analyse locale pour « {poste.Titre} » (candidat #{candidature.CandidateProfileId}).");
            sb.AppendLine(scoreCompatibilite is int s
                ? $"Score de compatibilité disponible : {s}%."
                : "Aucun score de compatibilité disponible pour cette candidature.");
            sb.AppendLine(criteres.Count == 0
                ? "Aucun critère de compétence défini sur ce poste — complétez le profil du poste puis régénérez."
                : $"{criteres.Count} critère(s) défini(s) ; {finals.Count} avec un niveau final renseigné.");
            sb.AppendLine("La génération IA était indisponible — ce résumé a été produit localement sans modèle externe.");
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Recalcule (si Compatibilite est actif) et reporte dans l'index partagé le score global, les scores
    /// par axe, les tags clés et de vigilance, ainsi que la complétion de la grille H — cette dernière
    /// alimente le tableau de bord Analytics indépendamment de l'activation de Compatibilite (c'est une
    /// donnée du candidat, pas un résultat de calcul de compatibilité). <paramref name="precomputed"/> évite
    /// un second calcul quand l'appelant vient juste de l'obtenir (voir <see cref="GetCandidaturesAsync"/>).
    /// </summary>
    private async Task UpsertCandidatureIndexAsync(Candidature candidature, string posteTitre, int companyId, CompatibiliteResultView? precomputed, CancellationToken cancellationToken)
    {
        var compatibiliteActif = await moduleRegistry.IsActiveAsync(companyId, "Compatibilite", coreDb, cancellationToken);

        var calcul = precomputed;
        if (compatibiliteActif && calcul is null)
        {
            var companyProfileId = await companyProfileService.GetOrCreateProfileIdAsync(cancellationToken);
            calcul = await compatibiliteService.CalculerCompatibiliteAsync(candidature.CandidateProfileId, companyProfileId, cancellationToken);
        }

        var candidateCriteria = await candidateProfileService.GetCompatibilityCriteriaAsync(candidature.CandidateProfileId, cancellationToken);

        // Tags clés = compétences techniques déclarées par le candidat (grille H) : la dimension la plus
        // parlante pour un premier tri visuel dans le Vivier, sans dupliquer toute la grille. Contrairement
        // à la complétion de grille ci-dessous, on la garde volontairement conditionnée à Compatibilite actif
        // (comportement inchangé par rapport à avant ce cycle).
        var tagsCles = compatibiliteActif ? candidateCriteria?.TechniqueTags ?? [] : [];
        var grilleCandidatComplete = EstGrilleComplete(candidateCriteria);
        var axisScores = calcul is null ? null : ToAxisScores(calcul.ScoresParAxe);
        var pointsVigilanceTags = calcul?.PointsVigilanceTagsPartages ?? [];

        await recruitmentIndex.UpsertCandidatureAsync(
            companyId, candidature.PosteId, posteTitre, candidature.CandidateProfileId,
            candidature.Statut.ToString(), calcul?.ScoreGlobal, tagsCles,
            axisScores, pointsVigilanceTags, grilleCandidatComplete, cancellationToken);
    }

    private static CandidatureAxisScores ToAxisScores(IReadOnlyList<AxisScoreView> scores) => new(
        Technique: scores.FirstOrDefault(s => s.Axis == CompatibilityAxis.Technique)?.Score,
        Comportementale: scores.FirstOrDefault(s => s.Axis == CompatibilityAxis.Comportementale)?.Score,
        Culturelle: scores.FirstOrDefault(s => s.Axis == CompatibilityAxis.Culturelle)?.Score,
        Organisationnelle: scores.FirstOrDefault(s => s.Axis == CompatibilityAxis.Organisationnelle)?.Score,
        Motivationnelle: scores.FirstOrDefault(s => s.Axis == CompatibilityAxis.Motivationnelle)?.Score);

    /// <summary>Même définition que <c>GrilleComplete</c> dans <c>QuestionnaireCandidat.razor</c> — les 4 axes à tags, le rythme, et les points de vigilance doivent tous être renseignés.</summary>
    private static bool EstGrilleComplete(CandidateCompatibilityCriteriaView? criteria) =>
        criteria is not null
        && criteria.TechniqueTags.Count > 0
        && criteria.ComportementaleTags.Count > 0
        && criteria.CulturelleTags.Count > 0
        && criteria.RythmeTravail is not null
        && criteria.MotivationnelleTags.Count > 0
        && criteria.PointsVigilanceTags.Count > 0;

    public async Task<IReadOnlyList<PosteOuvertView>> GetPostesOuvertsAsync(int candidateProfileId, CancellationToken cancellationToken = default)
    {
        // Lit l'index partagé (schéma core) au lieu d'itérer chaque schéma tenant un par un — l'ancienne
        // approche (parcourir Companies puis ouvrir un DbContext par entreprise) ne passait pas à l'échelle
        // au-delà de quelques tenants. L'index est tenu à jour par CreatePosteAsync/UpdatePosteAsync/
        // SetPosteStatutAsync (voir IRecruitmentIndexService pour la stratégie de synchronisation).
        var postesOuverts = await recruitmentIndex.GetPostesOuvertsAsync(cancellationToken);
        // Paire (CompanyId, PosteId) : PosteId seul n'est PAS un identifiant global (auto-incrémenté par
        // schéma tenant), donc deux entreprises différentes peuvent avoir chacune un poste "PosteId=1" —
        // ne comparer que sur PosteId ferait passer une candidature chez l'une pour une candidature chez l'autre.
        var postesDejaPostules = (await recruitmentIndex.GetPostesAvecCandidatureAsync(candidateProfileId, cancellationToken)).ToHashSet();

        return postesOuverts
            .Select(p => new PosteOuvertView(p.CompanyId, p.CompanyName, p.PosteId, p.Titre, p.Description, p.Departement, postesDejaPostules.Contains((p.CompanyId, p.PosteId))))
            .ToList();
    }

    public async Task PostulerAsync(int companyId, int posteId, int candidateProfileId, CancellationToken cancellationToken = default)
    {
        var company = await coreDb.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new InvalidOperationException("Entreprise introuvable.");

        await using var db = await CreateDbForSchemaAsync(company.SchemaName, cancellationToken);

        var dejaPostule = await db.Candidatures.AnyAsync(c => c.PosteId == posteId && c.CandidateProfileId == candidateProfileId, cancellationToken);
        if (dejaPostule) return;

        var poste = await db.Postes.AsNoTracking().FirstOrDefaultAsync(p => p.Id == posteId, cancellationToken)
            ?? throw new InvalidOperationException("Poste introuvable.");

        var candidature = new Candidature { PosteId = posteId, CandidateProfileId = candidateProfileId };
        db.Candidatures.Add(candidature);
        await db.SaveChangesAsync(cancellationToken);

        // Pas de calcul de compatibilité ici : le tenant ambiant de CETTE requête est celui du CANDIDAT qui
        // postule, pas celui de l'entreprise ciblée (voir CreateDbForSchemaAsync — schéma explicite, pas
        // ITenantContext) — appeler ICompanyProfileService/ICompatibiliteService ici lirait le mauvais
        // schéma. Le score sera calculé au premier passage de l'entreprise sur GetCandidaturesAsync
        // (déjà le comportement existant avant ce cycle) et reporté dans l'index à ce moment-là.
        // ICandidateProfileService, lui, est un schéma FIXE (pas tenant-scopé) — sûr à appeler ici pour
        // connaître la complétion de grille H dès la candidature, sans attendre ce premier passage.
        var candidateCriteria = await candidateProfileService.GetCompatibilityCriteriaAsync(candidateProfileId, cancellationToken);
        await recruitmentIndex.UpsertCandidatureAsync(
            companyId, posteId, poste.Titre, candidateProfileId,
            candidature.Statut.ToString(), scoreCompatibilite: null, tagsCles: [],
            axisScores: null, pointsVigilanceTags: [], grilleCandidatComplete: EstGrilleComplete(candidateCriteria), cancellationToken);
    }

    public async Task<Invitation> InviterCandidatAsync(int posteId, string email, string emetteurUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email requis.", nameof(email));
        if (string.IsNullOrWhiteSpace(emetteurUserId))
            throw new ArgumentException("Émetteur requis.", nameof(emetteurUserId));

        var company = await GetActiveCompanyAsync(cancellationToken);
        var rattache = await coreDb.UserCompanyLinks.AsNoTracking()
            .AnyAsync(l => l.UserId == emetteurUserId && l.CompanyId == company.Id, cancellationToken);
        if (!rattache)
            throw new InvalidOperationException("Utilisateur non rattaché à l'entreprise active.");

        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var posteExiste = await db.Postes.AsNoTracking().AnyAsync(p => p.Id == posteId, cancellationToken);
        if (!posteExiste)
            throw new InvalidOperationException("Poste introuvable dans le tenant actif.");

        return await invitationService.CreerAsync(
            emetteurUserId,
            email,
            InvitationType.CandidaturePoste,
            contextId: posteId,
            coreDb,
            cancellationToken);
    }

    public async Task<IReadOnlyList<InvitationView>> GetInvitationsCandidatEnCoursAsync(int posteId, CancellationToken cancellationToken = default)
    {
        // Sécurité : le poste doit exister dans le schéma ambiant (pas de fuite d'invitations d'un autre tenant
        // qui partagerait le même PosteId numérique).
        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var posteExiste = await db.Postes.AsNoTracking().AnyAsync(p => p.Id == posteId, cancellationToken);
        if (!posteExiste)
            return [];

        var company = await GetActiveCompanyAsync(cancellationToken);
        var emetteurIds = await coreDb.UserCompanyLinks.AsNoTracking()
            .Where(l => l.CompanyId == company.Id)
            .Select(l => l.UserId)
            .ToListAsync(cancellationToken);

        var invitations = await coreDb.Invitations.AsNoTracking()
            .Where(i => i.Type == InvitationType.CandidaturePoste
                && i.ContextId == posteId
                && i.Statut == InvitationStatus.EnAttente
                && emetteurIds.Contains(i.EmetteurUserId))
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

        return invitations
            .Select(i => new InvitationView(
                i.Id,
                i.EmailInvite,
                i.CreatedAt,
                LienRelatif: $"/invitations/accepter/{i.Token}"))
            .ToList();
    }

    public async Task RevokerInvitationCandidatAsync(int invitationId, string requestingUserId, CancellationToken cancellationToken = default)
    {
        var invitation = await coreDb.Invitations.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == invitationId && i.Type == InvitationType.CandidaturePoste, cancellationToken);
        if (invitation is null)
            return;

        // Ne révoquer que si le poste de l'invitation appartient au tenant ambiant.
        if (invitation.ContextId is int posteId)
        {
            await using var db = await CreateAmbientDbAsync(cancellationToken);
            if (!await db.Postes.AsNoTracking().AnyAsync(p => p.Id == posteId, cancellationToken))
                return;
        }

        await invitationService.RevoquerAsync(invitationId, requestingUserId, coreDb, cancellationToken);
    }

    public async Task FinaliserCandidatureDepuisInvitationAsync(Invitation invitation, string accepteurUserId, CancellationToken cancellationToken = default)
    {
        if (invitation.Type != InvitationType.CandidaturePoste)
            throw new InvalidOperationException("Invitation de type incorrect pour une candidature poste.");

        var posteId = invitation.ContextId
            ?? throw new InvalidOperationException("Invitation candidature sans ContextId (PosteId) — invitation mal formée.");

        // PosteId n'est pas global : on cherche le poste dans les schémas des entreprises de l'émetteur.
        var companyIds = await coreDb.UserCompanyLinks.AsNoTracking()
            .Where(l => l.UserId == invitation.EmetteurUserId)
            .Select(l => l.CompanyId)
            .ToListAsync(cancellationToken);

        var companies = await coreDb.Companies.AsNoTracking()
            .Where(c => companyIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        Company? companyCible = null;
        foreach (var company in companies)
        {
            await using var db = await CreateDbForSchemaAsync(company.SchemaName, cancellationToken);
            if (await db.Postes.AsNoTracking().AnyAsync(p => p.Id == posteId, cancellationToken))
            {
                companyCible = company;
                break;
            }
        }

        if (companyCible is null)
            throw new InvalidOperationException("Poste introuvable pour l'émetteur de l'invitation.");

        var candidateProfileId = await candidateProfileService.GetOrCreateProfileIdAsync(accepteurUserId, cancellationToken);
        await PostulerAsync(companyCible.Id, posteId, candidateProfileId, cancellationToken);
    }
}
