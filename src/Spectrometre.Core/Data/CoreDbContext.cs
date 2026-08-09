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

        SeedPlanModuleEntitlements(builder);

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
}
