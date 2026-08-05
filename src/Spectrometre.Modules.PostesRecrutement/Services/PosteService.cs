using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Data;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Compatibilite.Services;
using Spectrometre.Modules.PostesRecrutement.Data;
using Spectrometre.Modules.PostesRecrutement.Entities;
using Spectrometre.Modules.ProfilEntreprise.Services;

namespace Spectrometre.Modules.PostesRecrutement.Services;

/// <summary>
/// DbContext tenant-scopé instancié via <c>IDbContextFactory</c> (voir le commentaire détaillé sur
/// <c>CompanyProfileService</c>) : soit avec le schéma du tenant ambiant (gestion de ses propres postes),
/// soit avec le schéma explicite d'une autre entreprise (parcours des postes ouverts par un candidat,
/// une opération qui traverse plusieurs tenants et ne peut donc pas s'appuyer sur <see cref="ITenantContext"/>).
/// </summary>
public sealed class PosteService(
    IDbContextFactory<PostesRecrutementDbContext> dbFactory,
    ITenantContext tenantContext,
    CoreDbContext coreDb,
    IModuleRegistry moduleRegistry,
    ICompanyProfileService companyProfileService,
    ICompatibiliteService compatibiliteService) : IPosteService
{
    private Task<PostesRecrutementDbContext> CreateAmbientDbAsync(CancellationToken ct) =>
        CreateDbForSchemaAsync(tenantContext.SchemaName, ct);

    private async Task<PostesRecrutementDbContext> CreateDbForSchemaAsync(string schema, CancellationToken ct)
    {
        var db = await dbFactory.CreateDbContextAsync(ct);
        db.TenantSchema = schema;
        return db;
    }

    public async Task<int> CreatePosteAsync(string titre, string? description, string? departement, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var poste = new Poste { Titre = titre, Description = description, Departement = departement };
        db.Postes.Add(poste);
        await db.SaveChangesAsync(cancellationToken);
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
    }

    public async Task SetPosteStatutAsync(int posteId, PosteStatut statut, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var poste = await db.Postes.FirstOrDefaultAsync(p => p.Id == posteId, cancellationToken);
        if (poste is null) return;

        poste.Statut = statut;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CandidatureView>> GetCandidaturesAsync(int posteId, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateAmbientDbAsync(cancellationToken);
        var candidatures = await db.Candidatures
            .Where(c => c.PosteId == posteId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        // Intégration légère avec le Moteur de Compatibilité : uniquement si le module est actif pour ce
        // tenant, jamais de dépendance dure — le module reste utilisable sans Compatibilite activé.
        var compatibiliteActif = tenantContext.ActiveCompanyId is int companyId
            && await moduleRegistry.IsActiveAsync(companyId, "Compatibilite", coreDb, cancellationToken);

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
    }

    public async Task<IReadOnlyList<PosteOuvertView>> GetPostesOuvertsAsync(int candidateProfileId, CancellationToken cancellationToken = default)
    {
        // Parcourt tous les tenants : option la plus simple à ce stade (pas de module Vivier/index partagé
        // pour ce cycle). Acceptable pour un scaffold avec peu d'entreprises ; à revoir avec un
        // référentiel partagé si le nombre de tenants grandit significativement.
        var companies = await coreDb.Companies.AsNoTracking().ToListAsync(cancellationToken);
        var result = new List<PosteOuvertView>();

        foreach (var company in companies)
        {
            await using var db = await CreateDbForSchemaAsync(company.SchemaName, cancellationToken);

            List<Poste> postesOuverts;
            List<int> posteIdsDejaPostules;
            try
            {
                postesOuverts = await db.Postes.Where(p => p.Statut == PosteStatut.Ouvert).ToListAsync(cancellationToken);
                posteIdsDejaPostules = await db.Candidatures
                    .Where(c => c.CandidateProfileId == candidateProfileId)
                    .Select(c => c.PosteId)
                    .ToListAsync(cancellationToken);
            }
            catch (Npgsql.PostgresException)
            {
                // Le module n'a pas encore été activé/provisionné pour cette entreprise : ses tables
                // n'existent pas dans son schéma. On l'ignore plutôt que de faire échouer toute la recherche.
                continue;
            }

            result.AddRange(postesOuverts.Select(p => new PosteOuvertView(
                company.Id, company.Name, p.Id, p.Titre, p.Description, p.Departement,
                posteIdsDejaPostules.Contains(p.Id))));
        }

        return result;
    }

    public async Task PostulerAsync(int companyId, int posteId, int candidateProfileId, CancellationToken cancellationToken = default)
    {
        var company = await coreDb.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken)
            ?? throw new InvalidOperationException("Entreprise introuvable.");

        await using var db = await CreateDbForSchemaAsync(company.SchemaName, cancellationToken);

        var dejaPostule = await db.Candidatures.AnyAsync(c => c.PosteId == posteId && c.CandidateProfileId == candidateProfileId, cancellationToken);
        if (dejaPostule) return;

        db.Candidatures.Add(new Candidature { PosteId = posteId, CandidateProfileId = candidateProfileId });
        await db.SaveChangesAsync(cancellationToken);
    }
}
