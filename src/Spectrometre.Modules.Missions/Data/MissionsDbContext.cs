using Microsoft.EntityFrameworkCore;
using Spectrometre.Modules.Missions.Entities;

namespace Spectrometre.Modules.Missions.Data;

/// <summary>Schéma fixe <c>missions</c> — non tenant-scopé.</summary>
public sealed class MissionsDbContext(DbContextOptions<MissionsDbContext> options) : DbContext(options)
{
    public const string SchemaName = "missions";

    public DbSet<ParticulierProfile> ParticulierProfiles => Set<ParticulierProfile>();
    public DbSet<Mission> Missions => Set<Mission>();
    public DbSet<MissionAcceptation> MissionAcceptations => Set<MissionAcceptation>();
    public DbSet<MissionPreparationCoche> MissionPreparationCoches => Set<MissionPreparationCoche>();
    public DbSet<MissionRetour> MissionRetours => Set<MissionRetour>();
    public DbSet<MissionEvaluationParticulier> MissionEvaluationsParticulier => Set<MissionEvaluationParticulier>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(SchemaName);

        builder.Entity<ParticulierProfile>(e =>
        {
            e.HasIndex(p => p.UserId).IsUnique();
        });

        builder.Entity<Mission>(e =>
        {
            e.HasIndex(m => m.ParticulierProfileId);
            e.HasIndex(m => m.Statut);
            e.Property(m => m.RemunerationMontant).HasPrecision(18, 2);
            e.Property(m => m.Titre).IsRequired();
            e.Property(m => m.RisqueParticulier).HasMaxLength(2000);
        });

        builder.Entity<MissionAcceptation>(e =>
        {
            e.HasIndex(a => a.MissionId);
            e.HasIndex(a => a.JeuneProfileId);
            e.HasOne(a => a.Mission)
                .WithMany(m => m.Acceptations)
                .HasForeignKey(a => a.MissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MissionPreparationCoche>(e =>
        {
            e.HasIndex(c => new { c.MissionAcceptationId, c.ItemKey }).IsUnique();
            e.Property(c => c.ItemKey).HasMaxLength(64);
            e.HasOne(c => c.MissionAcceptation)
                .WithMany()
                .HasForeignKey(c => c.MissionAcceptationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MissionRetour>(e =>
        {
            e.HasIndex(r => r.MissionAcceptationId).IsUnique();
            e.HasOne(r => r.MissionAcceptation)
                .WithMany()
                .HasForeignKey(r => r.MissionAcceptationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MissionEvaluationParticulier>(e =>
        {
            e.ToTable("MissionEvaluationsParticulier");
            e.HasIndex(r => r.MissionAcceptationId).IsUnique();
            e.HasOne(r => r.MissionAcceptation)
                .WithMany()
                .HasForeignKey(r => r.MissionAcceptationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
