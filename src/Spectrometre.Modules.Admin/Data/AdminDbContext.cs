using Microsoft.EntityFrameworkCore;
using Spectrometre.Modules.Admin.Entities;

namespace Spectrometre.Modules.Admin.Data;

/// <summary>
/// Schéma fixe <c>admin</c> — non tenant-scopé, sans lien avec aucune entreprise. Seule table : le journal
/// d'audit minimal des actions d'écriture (voir <see cref="AdminAuditLogEntry"/>). Introduit avec ce cycle
/// (le cycle précédent, strictement lecture seule + promotion, n'avait besoin d'aucun stockage propre).
/// </summary>
public sealed class AdminDbContext(DbContextOptions<AdminDbContext> options) : DbContext(options)
{
    public const string SchemaName = "admin";

    public DbSet<AdminAuditLogEntry> AuditLog => Set<AdminAuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(SchemaName);

        builder.Entity<AdminAuditLogEntry>(e =>
        {
            e.HasIndex(a => a.CreatedAt);
        });
    }
}
