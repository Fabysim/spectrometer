using Microsoft.EntityFrameworkCore;
using Spectrometre.Modules.Coaching.Entities;

namespace Spectrometre.Modules.Coaching.Data;

/// <summary>Schéma fixe <c>coaching</c> — non tenant-scopé, scopé par UserId comme Gestion du temps/Profil Coach.</summary>
public sealed class CoachingDbContext(DbContextOptions<CoachingDbContext> options) : DbContext(options)
{
    public const string SchemaName = "coaching";

    public DbSet<LienCoaching> LiensCoaching => Set<LienCoaching>();
    public DbSet<AnamneseCoaching> AnamnesesCoaching => Set<AnamneseCoaching>();
    public DbSet<PeriodeObjectifsCoaching> PeriodesObjectifsCoaching => Set<PeriodeObjectifsCoaching>();
    public DbSet<ObjectifCoaching> ObjectifsCoaching => Set<ObjectifCoaching>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(SchemaName);

        builder.Entity<LienCoaching>(e =>
        {
            e.HasIndex(l => l.SuiviUserId);
            e.HasIndex(l => l.CoachUserId);
        });

        builder.Entity<AnamneseCoaching>(e =>
        {
            e.HasIndex(a => a.LienCoachingId).IsUnique();
        });

        builder.Entity<PeriodeObjectifsCoaching>(e =>
        {
            e.HasIndex(p => new { p.LienCoachingId, p.Archivee });
            e.HasMany(p => p.Objectifs)
                .WithOne(o => o.Periode)
                .HasForeignKey(o => o.PeriodeObjectifsCoachingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ObjectifCoaching>(e =>
        {
            e.HasIndex(o => o.PeriodeObjectifsCoachingId);
            e.Property(o => o.Titre).HasMaxLength(500);
        });
    }
}
