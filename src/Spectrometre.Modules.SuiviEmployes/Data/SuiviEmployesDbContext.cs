using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.SuiviEmployes.Entities;

namespace Spectrometre.Modules.SuiviEmployes.Data;

/// <summary>
/// Évaluation continue des employés — schéma = entreprise active
/// (même pattern que <c>ProfilEntrepriseDbContext</c> / <c>RecrutementDbContext</c>).
/// <c>PosteId</c> / <c>CritereId</c> sont des clés logiques vers ProfilEntreprise, sans FK EF.
/// </summary>
public sealed class SuiviEmployesDbContext(DbContextOptions<SuiviEmployesDbContext> options)
    : DbContext(options), ITenantScopedDbContext
{
    public string TenantSchema { get; set; } = "public";

    public DbSet<EvaluationEmploye> EvaluationsEmploye => Set<EvaluationEmploye>();
    public DbSet<ValidationSocioProEmploye> ValidationsSocioProEmploye => Set<ValidationSocioProEmploye>();
    public DbSet<EvaluationEmployeCloturee> EvaluationsEmployeCloturees => Set<EvaluationEmployeCloturee>();
    public DbSet<AnalyseIaEmploye> AnalysesIaEmploye => Set<AnalyseIaEmploye>();
    public DbSet<EvaluationObjectifs> EvaluationsObjectifs => Set<EvaluationObjectifs>();
    public DbSet<Objectif> Objectifs => Set<Objectif>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(TenantSchema);

        builder.Entity<EvaluationEmploye>(e =>
        {
            e.HasIndex(x => new { x.UserCompanyLinkId, x.CritereId, x.EvaluationDate, x.DaySequence })
                .IsUnique();
            e.HasIndex(x => new { x.UserCompanyLinkId, x.PosteId, x.EvaluationDate });
        });

        builder.Entity<ValidationSocioProEmploye>(e =>
        {
            e.HasIndex(x => new { x.UserCompanyLinkId, x.PosteId }).IsUnique();
        });

        builder.Entity<EvaluationEmployeCloturee>(e =>
        {
            e.HasIndex(x => new { x.UserCompanyLinkId, x.PosteId, x.EvaluationDate }).IsUnique();
        });

        builder.Entity<AnalyseIaEmploye>(e =>
        {
            e.HasIndex(x => new { x.UserCompanyLinkId, x.DataHash }).IsUnique();
        });

        builder.Entity<EvaluationObjectifs>(e =>
        {
            e.HasIndex(x => new { x.UserCompanyLinkId, x.DateDebut, x.DateFin });
            e.HasMany(x => x.Objectifs)
                .WithOne(o => o.EvaluationObjectifs)
                .HasForeignKey(o => o.EvaluationObjectifsId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Objectif>(e =>
        {
            e.HasIndex(x => x.EvaluationObjectifsId);
        });
    }
}
