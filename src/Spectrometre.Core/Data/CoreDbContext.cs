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
    public DbSet<PlanModuleEntitlement> PlanModuleEntitlements => Set<PlanModuleEntitlement>();
    public DbSet<Plan> Plans => Set<Plan>();
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

        builder.Entity<PlanModuleEntitlement>(e =>
        {
            e.HasIndex(x => new { x.PlanCode, x.ModuleCode }).IsUnique();
        });

        builder.Entity<Plan>(e =>
        {
            e.HasIndex(p => p.Code).IsUnique();
            e.Property(p => p.Code).HasMaxLength(100);
            e.Property(p => p.Nom).HasMaxLength(200);
            e.Property(p => p.PrixDevise).HasMaxLength(10);
            e.Property(p => p.PrixMontant).HasPrecision(18, 2);
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

        SeedPlanModuleEntitlements(builder);
        SeedPlans(builder);
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
    /// Codes de module en toutes lettres (le noyau ne référence aucun type de module, voir la contrainte
    /// d'architecture — même raison que les chaînes littérales déjà utilisées dans <c>PosteService</c>/
    /// <c>ProfileChangeRecorder</c>). <see cref="PlanCodes.Standard"/> reprend exactement les 8 modules du
    /// domaine Matching Emploi déjà activés par défaut pour toute entreprise (voir
    /// <c>CompanyOnboardingService</c>) — sans GestionDuTemps, vendu séparément.
    /// <see cref="PlanCodes.StandardPlusTemps"/> ajoute GestionDuTemps, pour tester le gating.
    /// </summary>
    private static void SeedPlanModuleEntitlements(ModelBuilder builder)
    {
        // Liste historique (ids 1–8 Standard, 9–17 StandardPlusTemps, 18 Coach) — ne PAS y intercaler
        // de nouveaux codes : ça renuméroterait les seeds existants. Les ajouts Matching Emploi
        // s'appendent après avec les prochains Id libres (voir SuiviEmployes ci-dessous).
        string[] modulesMatchingEmploi =
        [
            "ProfilCandidat", "ProfilEntreprise", "Compatibilite", "Recrutement",
            "Vivier", "Entretien", "SuiviEvolutif", "Analytics",
        ];

        var entitlements = new List<PlanModuleEntitlement>();
        var id = 1;

        foreach (var moduleCode in modulesMatchingEmploi)
            entitlements.Add(new PlanModuleEntitlement { Id = id++, PlanCode = PlanCodes.Standard, ModuleCode = moduleCode });

        foreach (var moduleCode in modulesMatchingEmploi.Append("GestionDuTemps"))
            entitlements.Add(new PlanModuleEntitlement { Id = id++, PlanCode = PlanCodes.StandardPlusTemps, ModuleCode = moduleCode });

        // Plan Coach : gratuit, un seul module inclus (ProfilCoach — voir CoachOnboardingService).
        entitlements.Add(new PlanModuleEntitlement { Id = id++, PlanCode = PlanCodes.Coach, ModuleCode = "ProfilCoach" });

        // SuiviEmployes (Matching Emploi) — Ids 1000/1001 : la plage 19+ est déjà polluée en base
        // de dév par des PlanCode « plan-histo-* » créés par les tests (PK collision sinon).
        entitlements.Add(new PlanModuleEntitlement { Id = 1000, PlanCode = PlanCodes.Standard, ModuleCode = "SuiviEmployes" });
        entitlements.Add(new PlanModuleEntitlement { Id = 1001, PlanCode = PlanCodes.StandardPlusTemps, ModuleCode = "SuiviEmployes" });

        // Coach + Gestion du temps (usage personnel) — Ids 2000/2001 : 1002+ déjà pollué par des
        // PlanCode « plan-histo-* » créés par les tests (même problème que SuiviEmployes à 1000).
        entitlements.Add(new PlanModuleEntitlement { Id = 2000, PlanCode = PlanCodes.CoachPlusTemps, ModuleCode = "ProfilCoach" });
        entitlements.Add(new PlanModuleEntitlement { Id = 2001, PlanCode = PlanCodes.CoachPlusTemps, ModuleCode = "GestionDuTemps" });

        builder.Entity<PlanModuleEntitlement>().HasData(entitlements);
    }

    /// <summary>
    /// PLACEHOLDER — montants inventés pour le seed initial, à ajuster manuellement depuis
    /// <c>/admin/plans</c> (pas des prix définitifs de production).
    /// </summary>
    private static void SeedPlans(ModelBuilder builder)
    {
        var createdAt = new DateTimeOffset(2026, 8, 9, 0, 0, 0, TimeSpan.Zero);
        builder.Entity<Plan>().HasData(
            new Plan
            {
                Id = 1,
                Code = PlanCodes.Standard,
                Nom = "Standard",
                PrixMontant = 49m,
                PrixDevise = "EUR",
                Periodicite = PeriodicitePlan.Mensuel,
                Actif = true,
                CreatedAt = createdAt,
            },
            new Plan
            {
                Id = 2,
                Code = PlanCodes.StandardPlusTemps,
                Nom = "Standard + Temps",
                PrixMontant = 79m,
                PrixDevise = "EUR",
                Periodicite = PeriodicitePlan.Mensuel,
                Actif = true,
                CreatedAt = createdAt,
            },
            new Plan
            {
                Id = 3,
                Code = PlanCodes.Coach,
                Nom = "Coach (gratuit)",
                PrixMontant = 0m,
                PrixDevise = "EUR",
                Periodicite = PeriodicitePlan.Mensuel,
                Actif = true,
                CreatedAt = createdAt,
            },
            new Plan
            {
                Id = 4,
                Code = PlanCodes.CoachPlusTemps,
                Nom = "Coach + Temps",
                PrixMontant = 19m,
                PrixDevise = "EUR",
                Periodicite = PeriodicitePlan.Mensuel,
                Actif = true,
                CreatedAt = createdAt,
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
