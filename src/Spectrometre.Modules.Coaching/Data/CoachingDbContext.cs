using Microsoft.EntityFrameworkCore;
using Spectrometre.Modules.Coaching.Entities;

namespace Spectrometre.Modules.Coaching.Data;

/// <summary>Schéma fixe <c>coaching</c> — non tenant-scopé, scopé par UserId comme Gestion du temps/Profil Coach.</summary>
public sealed class CoachingDbContext(DbContextOptions<CoachingDbContext> options) : DbContext(options)
{
    public const string SchemaName = "coaching";

    public DbSet<LienCoaching> LiensCoaching => Set<LienCoaching>();
    public DbSet<AnamneseCoaching> AnamnesesCoaching => Set<AnamneseCoaching>();

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
    }
}
