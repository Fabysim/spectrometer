using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.PostesRecrutement.Entities;

namespace Spectrometre.Modules.PostesRecrutement.Data;

/// <summary>
/// Schéma = celui de l'entreprise qui a publié les postes. <see cref="TenantSchema"/> est affecté par
/// l'appelant après création via <c>IDbContextFactory</c> (jamais résolu via <c>ITenantContext</c> injecté
/// au constructeur) — voir le commentaire détaillé sur <c>ProfilEntrepriseDbContext</c> pour la raison
/// (root service provider de la factory, incapable de résoudre un service scoped).
/// </summary>
public sealed class PostesRecrutementDbContext(DbContextOptions<PostesRecrutementDbContext> options)
    : DbContext(options), ITenantScopedDbContext
{
    public string TenantSchema { get; set; } = "public";

    public DbSet<Poste> Postes => Set<Poste>();
    public DbSet<Candidature> Candidatures => Set<Candidature>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(TenantSchema);

        builder.Entity<Candidature>(e =>
        {
            e.HasIndex(c => new { c.PosteId, c.CandidateProfileId }).IsUnique();
        });
    }
}
