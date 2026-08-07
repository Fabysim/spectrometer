using Microsoft.EntityFrameworkCore;
using Spectrometre.Modules.ProfilCoach.Entities;

namespace Spectrometre.Modules.ProfilCoach.Data;

/// <summary>Schéma fixe <c>profil_coach</c> — non tenant-scopé, voir <see cref="CoachProfile"/>.</summary>
public sealed class ProfilCoachDbContext(DbContextOptions<ProfilCoachDbContext> options) : DbContext(options)
{
    public const string SchemaName = "profil_coach";

    public DbSet<CoachProfile> CoachProfiles => Set<CoachProfile>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(SchemaName);

        builder.Entity<CoachProfile>(e =>
        {
            e.HasIndex(p => p.UserId).IsUnique();
        });
    }
}
