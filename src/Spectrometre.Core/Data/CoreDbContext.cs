using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Tenancy;

namespace Spectrometre.Core.Data;

/// <summary>
/// Schéma partagé (<c>core</c>) : identité, entreprises/tenants, liaison utilisateur↔entreprises,
/// registre d'activation des modules, abonnements. Toujours le même schéma quel que soit le tenant actif —
/// contrairement aux DbContext de modules qui, eux, ciblent le schéma de l'entreprise active.
/// </summary>
public sealed class CoreDbContext(DbContextOptions<CoreDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<UserCompanyLink> UserCompanyLinks => Set<UserCompanyLink>();
    public DbSet<ModuleActivation> ModuleActivations => Set<ModuleActivation>();
    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("core");

        builder.Entity<Company>(e =>
        {
            e.HasIndex(c => c.SchemaName).IsUnique();
        });

        builder.Entity<UserCompanyLink>(e =>
        {
            e.HasOne(l => l.Company)
                .WithMany(c => c.UserLinks)
                .HasForeignKey(l => l.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(l => new { l.UserId, l.CompanyId }).IsUnique();
        });

        builder.Entity<ModuleActivation>(e =>
        {
            e.HasIndex(a => new { a.CompanyId, a.ModuleCode }).IsUnique();
        });
    }
}
