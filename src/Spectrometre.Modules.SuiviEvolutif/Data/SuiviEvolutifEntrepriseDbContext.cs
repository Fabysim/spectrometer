using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.SuiviEvolutif.Entities;

namespace Spectrometre.Modules.SuiviEvolutif.Data;

/// <summary>Schéma = celui de l'entreprise active — même pattern que <c>ProfilEntrepriseDbContext</c> (voir son commentaire détaillé).</summary>
public sealed class SuiviEvolutifEntrepriseDbContext(DbContextOptions<SuiviEvolutifEntrepriseDbContext> options)
    : DbContext(options), ITenantScopedDbContext
{
    public string TenantSchema { get; set; } = "public";

    public DbSet<CompanyProfileChangeEntry> Entries => Set<CompanyProfileChangeEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(TenantSchema);

        builder.Entity<CompanyProfileChangeEntry>(e =>
        {
            e.HasIndex(c => new { c.CompanyProfileId, c.Horodatage });
        });
    }
}
