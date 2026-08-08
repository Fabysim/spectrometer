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
    public DbSet<CritereEvaluation> CriteresEvaluation => Set<CritereEvaluation>();
    public DbSet<EvaluationCritereCandidature> EvaluationsCriteresCandidature => Set<EvaluationCritereCandidature>();
    public DbSet<GuideDeuxiemeEntrevue> GuidesDeuxiemeEntrevue => Set<GuideDeuxiemeEntrevue>();
    public DbSet<AnalyseIaPoste> AnalysesIaPoste => Set<AnalyseIaPoste>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(TenantSchema);

        builder.Entity<Candidature>(e =>
        {
            e.HasIndex(c => new { c.PosteId, c.CandidateProfileId }).IsUnique();
            e.HasMany(c => c.EvaluationsFinales)
                .WithOne()
                .HasForeignKey(ev => ev.CandidatureId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CritereEvaluation>(e =>
        {
            e.HasIndex(c => new { c.PosteId, c.OrdreAffichage });
            e.Property(c => c.Categorie).HasMaxLength(200);
            e.Property(c => c.Libelle).HasMaxLength(500);
        });

        builder.Entity<EvaluationCritereCandidature>(e =>
        {
            e.HasIndex(ev => new { ev.CandidatureId, ev.CritereId }).IsUnique();
            e.HasOne<CritereEvaluation>()
                .WithMany()
                .HasForeignKey(ev => ev.CritereId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<GuideDeuxiemeEntrevue>(e =>
        {
            e.HasIndex(g => g.PosteId).IsUnique();
            e.HasOne<Poste>()
                .WithMany()
                .HasForeignKey(g => g.PosteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AnalyseIaPoste>(e =>
        {
            e.HasIndex(a => new { a.PosteId, a.CandidatureId }).IsUnique();
            e.HasOne<Candidature>()
                .WithMany()
                .HasForeignKey(a => a.CandidatureId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
