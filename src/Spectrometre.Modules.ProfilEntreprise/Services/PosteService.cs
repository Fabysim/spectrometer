using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Data;
using Spectrometre.Core.Invitations;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Recruitment;
using Spectrometre.Core.Tenancy;
using Spectrometre.Core.Compatibility;
using Spectrometre.Modules.ProfilEntreprise.Data;
using Spectrometre.Modules.ProfilEntreprise.Entities;
using Spectrometre.Modules.ProfilCandidat.Services;
using Spectrometre.Modules.ProfilEntreprise.Services;

namespace Spectrometre.Modules.ProfilEntreprise.Services;

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
    IDbContextFactory<ProfilEntrepriseDbContext> dbFactory,
    ITenantContext tenantContext,
    CoreDbContext coreDb,
    IModuleRegistry moduleRegistry,
    ICompanyProfileService companyProfileService,
    ICandidateProfileService candidateProfileService,
    ICompatibiliteScoreService compatibiliteScoreService,
    IRecruitmentIndexService recruitmentIndex,
    IRecrutementEntretienCleanup recrutementEntretienCleanup,
    IPosteCritereIaService posteCritereIa,
    IInvitationService invitationService) : IPosteService
{
    private Task<ProfilEntrepriseDbContext> CreateAmbientDbAsync(CancellationToken ct) =>
        CreateDbForSchemaAsync(tenantContext.SchemaName, ct);

    private async Task<ProfilEntrepriseDbContext> CreateDbForSchemaAsync(string schema, CancellationToken ct)
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

        // Guides / analyses IA vivent dans le module Recrutement (DbContext séparé).
        await recrutementEntretienCleanup.DeleteDonneesEntretienPourPosteAsync(posteId, cancellationToken);

        // Pas de FK Candidature/Critere → Poste dans le modèle : suppression manuelle des dépendances.
        var candidatureIds = await db.Candidatures
            .Where(c => c.PosteId == posteId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        if (candidatureIds.Count > 0)
        {
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

        var generations = await db.GenerationsCriteresIaPoste
            .Where(g => g.PosteId == posteId)
            .ToListAsync(cancellationToken);
        db.GenerationsCriteresIaPoste.RemoveRange(generations);

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
            CompatibiliteScoresSnapshot? calcul = null;
            if (compatibiliteActif && companyProfileId is int cpid)
                calcul = await compatibiliteScoreService.CalculerScoresAsync(c.CandidateProfileId, cpid, cancellationToken);

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
        CompatibiliteScoresSnapshot? calcul = null;
        if (compatibiliteActif)
        {
            var companyProfileId = await companyProfileService.GetOrCreateProfileIdAsync(cancellationToken);
            calcul = await compatibiliteScoreService.CalculerScoresAsync(candidature.CandidateProfileId, companyProfileId, cancellationToken);
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

    public async Task<IReadOnlyList<CritereEvaluationView>> GetCriteresPosteOuvertAsync(
        int companyId,
        int posteId,
        CancellationToken cancellationToken = default)
    {
        var company = await coreDb.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null)
            return [];

        await using var db = await CreateDbForSchemaAsync(company.SchemaName, cancellationToken);
        var posteOuvert = await db.Postes.AsNoTracking()
            .AnyAsync(p => p.Id == posteId && p.Statut == PosteStatut.Ouvert, cancellationToken);
        if (!posteOuvert)
            return [];

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

        var evaluations = await db.EvaluationsCriteresCandidature.AsNoTracking()
            .Where(e => e.CandidatureId == candidatureId)
            .ToDictionaryAsync(e => e.CritereId, cancellationToken);

        return criteres
            .Select(c =>
            {
                evaluations.TryGetValue(c.Id, out var eval);
                return new EvaluationCritereView(
                    c.Id,
                    c.Categorie,
                    c.Libelle,
                    c.NiveauRequis,
                    eval?.NiveauDeclare,
                    eval?.NiveauFinal,
                    c.OrdreAffichage);
            })
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

    public async Task<int> GenererCriteresIaAsync(int posteId, bool forcerRegeneration = false, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var poste = await db.Postes.AsNoTracking().FirstOrDefaultAsync(p => p.Id == posteId, cancellationToken);
        if (poste is null)
            return 0;

        // Pas de champ CompétencesRequises sur Poste (modulaire) — le paramètre reste disponible
        // pour le prompt/hash (null → chaîne vide), aligné sur titre/description/tâches.
        string? competencesRequises = null;
        var hashContexte = CalculerHashContexteCriteresIa(
            poste.Titre,
            poste.Description,
            poste.TachesDescription,
            competencesRequises);

        var generation = await db.GenerationsCriteresIaPoste
            .FirstOrDefaultAsync(g => g.PosteId == posteId, cancellationToken);

        if (!forcerRegeneration
            && generation is not null
            && generation.HashContexte == hashContexte
            && generation.GenereeParIa)
        {
            return 0;
        }

        IReadOnlyList<(string Categorie, string Libelle, int NiveauRequis)> suggestions;
        try
        {
            suggestions = await posteCritereIa.SuggererCriteresAsync(
                poste.Titre,
                poste.Description,
                poste.TachesDescription,
                competencesRequises,
                cancellationToken);
        }
        catch
        {
            // Filet : IPosteCritereIaService ne doit pas lever, mais on ne remonte jamais d'exception.
            return -1;
        }

        // Liste vide = échec IA ou réponse inutilisable — ne pas verrouiller le hash (retry possible).
        if (suggestions.Count == 0)
            return -1;

        var existants = await db.CriteresEvaluation.AsNoTracking()
            .Where(c => c.PosteId == posteId)
            .Select(c => new { c.Categorie, c.Libelle, c.OrdreAffichage })
            .ToListAsync(cancellationToken);

        var clesExistantes = existants
            .Select(c => CleCritere(c.Categorie, c.Libelle))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var prochainOrdre = existants.Count == 0 ? 0 : existants.Max(c => c.OrdreAffichage) + 1;
        var ajoutes = 0;

        foreach (var (categorie, libelle, niveauRequis) in suggestions)
        {
            var cle = CleCritere(categorie, libelle);
            if (string.IsNullOrWhiteSpace(cle) || clesExistantes.Contains(cle))
                continue;

            // UpsertCritereAsync ouvre son propre DbContext — acceptable ici (agrégation séquentielle).
            await UpsertCritereAsync(posteId, id: null, categorie, libelle, niveauRequis, prochainOrdre, cancellationToken);
            clesExistantes.Add(cle);
            prochainOrdre++;
            ajoutes++;
        }

        // Recharger la génération dans ce contexte (peut avoir été créée entre-temps).
        generation = await db.GenerationsCriteresIaPoste
            .FirstOrDefaultAsync(g => g.PosteId == posteId, cancellationToken);
        if (generation is null)
        {
            generation = new GenerationCriteresIaPoste { PosteId = posteId };
            db.GenerationsCriteresIaPoste.Add(generation);
        }

        generation.HashContexte = hashContexte;
        generation.GenereeParIa = true;
        generation.GenereeLe = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return ajoutes;
    }

    private static string CleCritere(string? categorie, string? libelle) =>
        $"{(categorie ?? "").Trim()}|{(libelle ?? "").Trim()}";

    /// <summary>SHA-256 hex du contexte poste pour l'idempotence des critères IA.</summary>
    private static string CalculerHashContexteCriteresIa(
        string titre,
        string? description,
        string? tachesDescription,
        string? competencesRequises)
    {
        var sb = new StringBuilder();
        sb.Append(titre?.Trim() ?? "").Append('|')
            .Append(description?.Trim() ?? "").Append('|')
            .Append(tachesDescription?.Trim() ?? "").Append('|')
            .Append(competencesRequises?.Trim() ?? "");
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes);
    }

    /// <summary>
    /// Recalcule (si Compatibilite est actif) et reporte dans l'index partagé le score global, les scores
    /// par axe, les tags clés et de vigilance, ainsi que la complétion de la grille H — cette dernière
    /// alimente le tableau de bord Analytics indépendamment de l'activation de Compatibilite (c'est une
    /// donnée du candidat, pas un résultat de calcul de compatibilité). <paramref name="precomputed"/> évite
    /// un second calcul quand l'appelant vient juste de l'obtenir (voir <see cref="GetCandidaturesAsync"/>).
    /// </summary>
    private async Task UpsertCandidatureIndexAsync(Candidature candidature, string posteTitre, int companyId, CompatibiliteScoresSnapshot? precomputed, CancellationToken cancellationToken)
    {
        var compatibiliteActif = await moduleRegistry.IsActiveAsync(companyId, "Compatibilite", coreDb, cancellationToken);

        var calcul = precomputed;
        if (compatibiliteActif && calcul is null)
        {
            var companyProfileId = await companyProfileService.GetOrCreateProfileIdAsync(cancellationToken);
            calcul = await compatibiliteScoreService.CalculerScoresAsync(candidature.CandidateProfileId, companyProfileId, cancellationToken);
        }

        var candidateCriteria = await candidateProfileService.GetCompatibilityCriteriaAsync(candidature.CandidateProfileId, cancellationToken);

        // Tags clés = compétences techniques déclarées par le candidat (grille H) : la dimension la plus
        // parlante pour un premier tri visuel dans le Vivier, sans dupliquer toute la grille. Contrairement
        // à la complétion de grille ci-dessous, on la garde volontairement conditionnée à Compatibilite actif
        // (comportement inchangé par rapport à avant ce cycle).
        var tagsCles = compatibiliteActif ? candidateCriteria?.TechniqueTags ?? [] : [];
        var grilleCandidatComplete = EstGrilleComplete(candidateCriteria);
        var axisScores = calcul is null ? null : new CandidatureAxisScores(
            calcul.Technique, calcul.Comportementale, calcul.Culturelle, calcul.Organisationnelle, calcul.Motivationnelle);
        var pointsVigilanceTags = calcul?.PointsVigilanceTags ?? [];

        await recruitmentIndex.UpsertCandidatureAsync(
            companyId, candidature.PosteId, posteTitre, candidature.CandidateProfileId,
            candidature.Statut.ToString(), calcul?.ScoreGlobal, tagsCles,
            axisScores, pointsVigilanceTags, grilleCandidatComplete, cancellationToken);
    }


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

    public async Task<PosteDetailCandidatView?> GetPosteOuvertDetailAsync(
        int companyId,
        int posteId,
        int candidateProfileId,
        CancellationToken cancellationToken = default)
    {
        var company = await coreDb.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null)
            return null;

        await using var db = await CreateDbForSchemaAsync(company.SchemaName, cancellationToken);
        // Ouvert uniquement — fermé ou absent → null uniforme (pas de fuite d'existence).
        var poste = await db.Postes.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == posteId && p.Statut == PosteStatut.Ouvert, cancellationToken);
        if (poste is null)
            return null;

        var dejaPostule = await db.Candidatures.AsNoTracking()
            .AnyAsync(c => c.PosteId == posteId && c.CandidateProfileId == candidateProfileId, cancellationToken);

        return new PosteDetailCandidatView(
            company.Id,
            company.Name,
            poste.Id,
            poste.Titre,
            poste.Departement,
            poste.OffreTexte,
            poste.OffreGenereeLe,
            poste.OffreGenereeParIa,
            dejaPostule);
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

    public async Task<(bool Succes, string? Erreur)> PostulerAvecGrilleAsync(
        int companyId,
        int posteId,
        int candidateProfileId,
        IReadOnlyDictionary<int, NiveauEvaluation> niveauxDeclares,
        CancellationToken cancellationToken = default)
    {
        niveauxDeclares ??= new Dictionary<int, NiveauEvaluation>();

        var company = await coreDb.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null)
            return (false, "Entreprise introuvable.");

        await using var db = await CreateDbForSchemaAsync(company.SchemaName, cancellationToken);

        var dejaPostule = await db.Candidatures.AnyAsync(
            c => c.PosteId == posteId && c.CandidateProfileId == candidateProfileId,
            cancellationToken);
        if (dejaPostule)
            return (true, null);

        var poste = await db.Postes.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == posteId && p.Statut == PosteStatut.Ouvert, cancellationToken);
        if (poste is null)
            return (false, "Poste introuvable ou fermé.");

        var criteres = await db.CriteresEvaluation.AsNoTracking()
            .Where(c => c.PosteId == posteId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        foreach (var critereId in criteres)
        {
            if (!niveauxDeclares.ContainsKey(critereId))
                return (false, "Grille d'évaluation incomplète : chaque critère doit avoir un niveau déclaré.");
        }

        // Rejeter des IDs de critères étrangers au poste (évite d'accepter une grille « complète » bidon).
        if (niveauxDeclares.Keys.Any(id => !criteres.Contains(id)))
            return (false, "Grille d'évaluation invalide : critère inconnu pour ce poste.");

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var candidature = new Candidature { PosteId = posteId, CandidateProfileId = candidateProfileId };
            db.Candidatures.Add(candidature);
            await db.SaveChangesAsync(cancellationToken);

            var now = DateTimeOffset.UtcNow;
            foreach (var critereId in criteres)
            {
                db.EvaluationsCriteresCandidature.Add(new EvaluationCritereCandidature
                {
                    CandidatureId = candidature.Id,
                    CritereId = critereId,
                    NiveauDeclare = niveauxDeclares[critereId],
                    NiveauFinal = null,
                    UpdatedAt = now,
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            var candidateCriteria = await candidateProfileService.GetCompatibilityCriteriaAsync(candidateProfileId, cancellationToken);
            await recruitmentIndex.UpsertCandidatureAsync(
                companyId, posteId, poste.Titre, candidateProfileId,
                candidature.Statut.ToString(), scoreCompatibilite: null, tagsCles: [],
                axisScores: null, pointsVigilanceTags: [], grilleCandidatComplete: EstGrilleComplete(candidateCriteria), cancellationToken);

            return (true, null);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
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

    public async Task<int> RattacherCandidatDepuisVivierAsync(int posteId, int candidateProfileId, CancellationToken cancellationToken = default)
    {
        var company = await GetActiveCompanyAsync(cancellationToken);

        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var poste = await db.Postes.AsNoTracking().FirstOrDefaultAsync(p => p.Id == posteId, cancellationToken)
            ?? throw new InvalidOperationException("Poste introuvable dans l'entreprise active.");

        var existante = await db.Candidatures
            .FirstOrDefaultAsync(c => c.PosteId == posteId && c.CandidateProfileId == candidateProfileId, cancellationToken);
        if (existante is not null)
            return existante.Id;

        // Garde structurelle : uniquement un candidat DÉJÀ connu de CETTE entreprise via l'index
        // (jamais un accès "candidat au hasard", jamais une vérif déléguée au module Vivier).
        var candidaturesEntreprise = await recruitmentIndex.GetCandidaturesPourEntrepriseAsync(company.Id, cancellationToken);
        if (!candidaturesEntreprise.Any(c => c.CandidateProfileId == candidateProfileId))
            throw new InvalidOperationException(
                "Ce candidat n'a aucune candidature dans l'entreprise active — rattachement depuis le vivier impossible.");

        var candidature = new Candidature { PosteId = posteId, CandidateProfileId = candidateProfileId };
        db.Candidatures.Add(candidature);
        await db.SaveChangesAsync(cancellationToken);

        // Tenant ambiant = entreprise active : le calcul de compatibilité (si module actif) est sûr ici,
        // contrairement à PostulerAsync qui traverse un autre schéma.
        await UpsertCandidatureIndexAsync(candidature, poste.Titre, company.Id, precomputed: null, cancellationToken);
        return candidature.Id;
    }
}
