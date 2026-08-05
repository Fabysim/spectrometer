using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Compatibilite.Entities;

namespace Spectrometre.Modules.Compatibilite.Data;

/// <summary>
/// Schéma = celui de l'entreprise active. <see cref="TenantSchema"/> est affecté par l'appelant après
/// création via <c>IDbContextFactory</c> — voir le commentaire détaillé sur <c>ProfilEntrepriseDbContext</c>.
/// </summary>
public sealed class CompatibiliteDbContext(DbContextOptions<CompatibiliteDbContext> options)
    : DbContext(options), ITenantScopedDbContext
{
    public string TenantSchema { get; set; } = "public";

    public DbSet<CompatibilityResult> CompatibilityResults => Set<CompatibilityResult>();
    public DbSet<CompatibilityWeightSetting> CompatibilityWeightSettings => Set<CompatibilityWeightSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(TenantSchema);

        builder.Entity<CompatibilityResult>()
            .HasMany(r => r.VigilancePoints)
            .WithOne()
            .HasForeignKey(v => v.CompatibilityResultId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CompatibilityWeightSetting>(e =>
        {
            e.HasIndex(w => w.Axis).IsUnique();
            e.HasData(
                Enum.GetValues<CompatibilityAxis>().Select((axis, i) => new CompatibilityWeightSetting
                {
                    Id = i + 1,
                    Axis = axis,
                    WeightPercent = 20m,
                }));
        });
    }
}
