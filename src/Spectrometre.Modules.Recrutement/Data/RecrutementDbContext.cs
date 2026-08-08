using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Recrutement.Entities;

namespace Spectrometre.Modules.Recrutement.Data;

/// <summary>
/// Données d'assistance à l'entretien (guides 2ème entrevue, analyses IA) — Poste / Candidature
/// vivent désormais dans <c>ProfilEntrepriseDbContext</c>. Les <c>PosteId</c> / <c>CandidatureId</c>
/// sont des clés logiques (index unique), sans FK EF cross-DbContext.
/// </summary>
public sealed class RecrutementDbContext(DbContextOptions<RecrutementDbContext> options)
    : DbContext(options), ITenantScopedDbContext
{
    public string TenantSchema { get; set; } = "public";

    public DbSet<GuideDeuxiemeEntrevue> GuidesDeuxiemeEntrevue => Set<GuideDeuxiemeEntrevue>();
    public DbSet<AnalyseIaPoste> AnalysesIaPoste => Set<AnalyseIaPoste>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(TenantSchema);

        builder.Entity<GuideDeuxiemeEntrevue>(e =>
        {
            e.HasIndex(g => g.PosteId).IsUnique();
        });

        builder.Entity<AnalyseIaPoste>(e =>
        {
            e.HasIndex(a => new { a.PosteId, a.CandidatureId }).IsUnique();
        });
    }
}
