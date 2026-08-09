using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Invitations;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Notifications;
using Spectrometre.Core.Recruitment;
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
    public DbSet<CandidateSubscription> CandidateSubscriptions => Set<CandidateSubscription>();
    public DbSet<CoachSubscription> CoachSubscriptions => Set<CoachSubscription>();
    public DbSet<ModulePrix> ModulePrix => Set<ModulePrix>();
    public DbSet<PaiementEnregistre> PaiementsEnregistres => Set<PaiementEnregistre>();
    public DbSet<PosteIndexEntry> PosteIndexEntries => Set<PosteIndexEntry>();
    public DbSet<CandidatureIndexEntry> CandidatureIndexEntries => Set<CandidatureIndexEntry>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<NotificationUtilisateur> NotificationsUtilisateur => Set<NotificationUtilisateur>();
    public DbSet<PreferenceNotification> PreferencesNotification => Set<PreferenceNotification>();

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
            e.HasIndex(a => new { a.SubjectType, a.SubjectId, a.ModuleCode }).IsUnique();
        });

        builder.Entity<TenantSubscription>(e =>
        {
            e.HasIndex(s => s.CompanyId).IsUnique();
        });

        builder.Entity<CandidateSubscription>(e =>
        {
            e.HasIndex(s => s.CandidateProfileId).IsUnique();
        });

        builder.Entity<CoachSubscription>(e =>
        {
            e.HasIndex(s => s.CoachProfileId).IsUnique();
        });

        builder.Entity<PaiementEnregistre>(e =>
        {
            e.HasIndex(p => new { p.SubjectType, p.SubjectId, p.CreatedAt });
            e.Property(p => p.PlanCode).HasMaxLength(100);
            e.Property(p => p.Devise).HasMaxLength(10);
            e.Property(p => p.Moyen).HasMaxLength(200);
            e.Property(p => p.NotePar).HasMaxLength(256);
            e.Property(p => p.ModulesFactures).HasMaxLength(1000);
            e.Property(p => p.Montant).HasPrecision(18, 2);
        });

        builder.Entity<ModulePrix>(e =>
        {
            e.HasIndex(p => p.ModuleCode).IsUnique();
            e.Property(p => p.ModuleCode).HasMaxLength(100);
            e.Property(p => p.Devise).HasMaxLength(10);
            e.Property(p => p.PrixMensuel).HasPrecision(18, 2);
        });

        SeedModulePrix(builder);

        builder.Entity<PosteIndexEntry>(e =>
        {
            e.HasIndex(p => new { p.CompanyId, p.PosteId }).IsUnique();
            e.HasIndex(p => p.Statut);
        });

        builder.Entity<CandidatureIndexEntry>(e =>
        {
            // CompanyId fait partie de la clé : PosteId seul n'est pas global (voir CandidatureIndexEntry).
            e.HasIndex(c => new { c.CompanyId, c.PosteId, c.CandidateProfileId }).IsUnique();
            e.HasIndex(c => c.CandidateProfileId);
        });

        builder.Entity<Invitation>(e =>
        {
            e.HasIndex(i => i.Token).IsUnique();
            e.HasIndex(i => new { i.EmailInvite, i.Type });
            e.HasIndex(i => i.EmetteurUserId);
        });

        builder.Entity<NotificationUtilisateur>(e =>
        {
            e.HasIndex(n => new { n.UserId, n.LueLe });
            e.HasIndex(n => n.CreatedAt);
            e.Property(n => n.Titre).HasMaxLength(200);
            e.Property(n => n.TypeCode).HasMaxLength(100);
            e.Property(n => n.Lien).HasMaxLength(500);
        });

        builder.Entity<PreferenceNotification>(e =>
        {
            e.HasIndex(p => new { p.UserId, p.CategorieCode }).IsUnique();
            e.Property(p => p.CategorieCode).HasMaxLength(100);
        });
    }

    /// <summary>
    /// PLACEHOLDER — tarifs à la carte (pas définitifs). Modules enregistrés dans Host
    /// (<c>Program.cs</c> → <c>moduleRegistry.Register</c>) + <c>Admin</c> (hors registre, toujours gratuit).
    /// Bundle recrutement (Compatibilite/Recrutement/Vivier/Entretien/Analytics) = activations séparées
    /// donc prix séparés ici ; l'admin peut mettre 0 sur certains s'il veut un forfait groupé.
    /// Ajuster depuis <c>/admin/tarifs-modules</c>.
    /// </summary>
    private static void SeedModulePrix(ModelBuilder builder)
    {
        var createdAt = new DateTimeOffset(2026, 8, 9, 0, 0, 0, TimeSpan.Zero);

        ModulePrix Row(int id, string code, decimal prix, bool facturable) => new()
        {
            Id = id,
            ModuleCode = code,
            PrixMensuel = prix,
            Devise = "EUR",
            Facturable = facturable,
            CreatedAt = createdAt,
        };

        builder.Entity<ModulePrix>().HasData(
            // Socle — jamais facturés
            Row(1, "ProfilCandidat", 0m, false),
            Row(2, "ProfilEntreprise", 0m, false),
            Row(3, "ProfilCoach", 0m, false),
            Row(4, "Admin", 0m, false),
            // Add-ons (placeholders)
            Row(5, "Compatibilite", 15m, true),
            Row(6, "Recrutement", 25m, true),
            Row(7, "Vivier", 10m, true),
            Row(8, "Entretien", 15m, true),
            Row(9, "Analytics", 15m, true),
            Row(10, "SuiviEvolutif", 20m, true),
            Row(11, "SuiviEmployes", 30m, true),
            Row(12, "GestionDuTemps", 25m, true));
    }
}
