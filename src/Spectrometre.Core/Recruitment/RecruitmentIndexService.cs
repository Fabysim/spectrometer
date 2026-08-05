using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Data;

namespace Spectrometre.Core.Recruitment;

/// <summary>Voir <see cref="IRecruitmentIndexService"/> pour la stratégie de synchronisation.</summary>
public sealed class RecruitmentIndexService(CoreDbContext db) : IRecruitmentIndexService
{
    public async Task UpsertPosteAsync(int companyId, string companyName, int posteId, string titre, string? description, string? departement, string statut, CancellationToken cancellationToken = default)
    {
        var entry = await db.PosteIndexEntries
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.PosteId == posteId, cancellationToken);

        if (entry is null)
        {
            entry = new PosteIndexEntry
            {
                CompanyId = companyId,
                CompanyName = companyName,
                PosteId = posteId,
                Titre = titre,
                Statut = statut,
            };
            db.PosteIndexEntries.Add(entry);
        }

        entry.CompanyName = companyName;
        entry.Titre = titre;
        entry.Description = description;
        entry.Departement = departement;
        entry.Statut = statut;
        entry.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpsertCandidatureAsync(int companyId, int posteId, string posteTitre, int candidateProfileId, string statut, int? scoreCompatibilite, IReadOnlyList<string> tagsCles, CancellationToken cancellationToken = default)
    {
        // CompanyId fait bien partie de la clé de correspondance : PosteId seul n'est pas un identifiant
        // global (auto-incrémenté par schéma tenant), voir le commentaire sur CandidatureIndexEntry.
        var entry = await db.CandidatureIndexEntries
            .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.PosteId == posteId && c.CandidateProfileId == candidateProfileId, cancellationToken);

        if (entry is null)
        {
            entry = new CandidatureIndexEntry
            {
                CompanyId = companyId,
                PosteId = posteId,
                PosteTitre = posteTitre,
                CandidateProfileId = candidateProfileId,
                Statut = statut,
            };
            db.CandidatureIndexEntries.Add(entry);
        }

        entry.PosteTitre = posteTitre;
        entry.Statut = statut;
        entry.ScoreCompatibilite = scoreCompatibilite;
        entry.TagsCles = tagsCles.ToList();
        entry.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PosteIndexView>> GetPostesOuvertsAsync(CancellationToken cancellationToken = default) =>
        await db.PosteIndexEntries
            .AsNoTracking()
            .Where(p => p.Statut == "Ouvert")
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new PosteIndexView(p.CompanyId, p.CompanyName, p.PosteId, p.Titre, p.Description, p.Departement, p.Statut))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<(int CompanyId, int PosteId)>> GetPostesAvecCandidatureAsync(int candidateProfileId, CancellationToken cancellationToken = default)
    {
        var rows = await db.CandidatureIndexEntries
            .AsNoTracking()
            .Where(c => c.CandidateProfileId == candidateProfileId)
            .Select(c => new { c.CompanyId, c.PosteId })
            .ToListAsync(cancellationToken);

        return rows.Select(r => (r.CompanyId, r.PosteId)).ToList();
    }

    public async Task<IReadOnlyList<CandidatureIndexView>> GetCandidaturesPourEntrepriseAsync(int companyId, CancellationToken cancellationToken = default) =>
        await db.CandidatureIndexEntries
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId)
            .OrderByDescending(c => c.ScoreCompatibilite ?? -1)
            .Select(c => new CandidatureIndexView(c.CompanyId, c.PosteId, c.PosteTitre, c.CandidateProfileId, c.Statut, c.ScoreCompatibilite, c.TagsCles, c.UpdatedAt))
            .ToListAsync(cancellationToken);
}
