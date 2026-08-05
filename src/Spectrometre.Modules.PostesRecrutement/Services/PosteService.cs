using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Data;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Recruitment;
using Spectrometre.Core.Tenancy;
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
/// Écrit dans <see cref="IRecruitmentIndexService"/> (schéma <c>core</c>) à chaque poste créé/modifié,
/// candidature créée ou statut changé — c'est cet index, pas une traversée des schémas tenant, que lit
/// désormais <see cref="GetPostesOuvertsAsync"/> (voir le commentaire sur cette méthode pour l'historique).
/// </remarks>
public sealed class PosteService(
    IDbContextFactory<PostesRecrutementDbContext> dbFactory,
    ITenantContext tenantContext,
    CoreDbContext coreDb,
    IModuleRegistry moduleRegistry,
    ICompanyProfileService companyProfileService,
    ICandidateProfileService candidateProfileService,
    ICompatibiliteService compatibiliteService,
    IRecruitmentIndexService recruitmentIndex) : IPosteService
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

    public async Task<int> CreatePosteAsync(string titre, string? description, string? departement, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var poste = new Poste { Titre = titre, Description = description, Departement = departement };
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
            .Select(p => new PosteView(p.Id, p.Titre, p.Description, p.Departement, p.Statut, p.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task UpdatePosteAsync(int posteId, string titre, string? description, string? departement, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var poste = await db.Postes.FirstOrDefaultAsync(p => p.Id == posteId, cancellationToken);
        if (poste is null) return;

        poste.Titre = titre;
        poste.Description = description;
        poste.Departement = departement;
        await db.SaveChangesAsync(cancellationToken);

        var company = await GetActiveCompanyAsync(cancellationToken);
        await recruitmentIndex.UpsertPosteAsync(company.Id, company.Name, poste.Id, poste.Titre, poste.Description, poste.Departement, poste.Statut.ToString(), cancellationToken);
    }

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
            int? score = null;
            if (compatibiliteActif && companyProfileId is int cpid)
            {
                var calcul = await compatibiliteService.CalculerCompatibiliteAsync(c.CandidateProfileId, cpid, cancellationToken);
                score = calcul.ScoreGlobal;
            }

            result.Add(new CandidatureView(c.Id, c.PosteId, c.CandidateProfileId, c.Statut, c.CreatedAt, score));

            // Recalcul de compatibilité = un des deux évènements qui doivent tenir l'index à jour (voir
            // IRecruitmentIndexService) : dès que ce score est (re)connu, on le reporte dans l'index lu
            // par le Vivier, sans attendre un futur changement de statut.
            if (poste is not null)
                await UpsertCandidatureIndexAsync(c, poste.Titre, company.Id, score, cancellationToken);
        }

        return result;
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
        await UpsertCandidatureIndexAsync(candidature, poste.Titre, company.Id, precomputedScore: null, cancellationToken);
    }

    /// <summary>
    /// Recalcule (si Compatibilite est actif) et reporte dans l'index partagé le score + les tags clés
    /// d'une candidature. <paramref name="precomputedScore"/> évite un second calcul quand l'appelant
    /// vient juste de l'obtenir (voir <see cref="GetCandidaturesAsync"/>).
    /// </summary>
    private async Task UpsertCandidatureIndexAsync(Candidature candidature, string posteTitre, int companyId, int? precomputedScore, CancellationToken cancellationToken)
    {
        var compatibiliteActif = await moduleRegistry.IsActiveAsync(companyId, "Compatibilite", coreDb, cancellationToken);

        int? score = precomputedScore;
        IReadOnlyList<string> tagsCles = [];

        if (compatibiliteActif)
        {
            if (score is null)
            {
                var companyProfileId = await companyProfileService.GetOrCreateProfileIdAsync(cancellationToken);
                var calcul = await compatibiliteService.CalculerCompatibiliteAsync(candidature.CandidateProfileId, companyProfileId, cancellationToken);
                score = calcul.ScoreGlobal;
            }

            // Tags clés = compétences techniques déclarées par le candidat (grille H) : la dimension la
            // plus parlante pour un premier tri visuel dans le Vivier, sans dupliquer toute la grille.
            var candidateCriteria = await candidateProfileService.GetCompatibilityCriteriaAsync(candidature.CandidateProfileId, cancellationToken);
            tagsCles = candidateCriteria?.TechniqueTags ?? [];
        }

        await recruitmentIndex.UpsertCandidatureAsync(
            companyId, candidature.PosteId, posteTitre, candidature.CandidateProfileId,
            candidature.Statut.ToString(), score, tagsCles, cancellationToken);
    }

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
        await recruitmentIndex.UpsertCandidatureAsync(
            companyId, posteId, poste.Titre, candidateProfileId,
            candidature.Statut.ToString(), scoreCompatibilite: null, tagsCles: [], cancellationToken);
    }
}
