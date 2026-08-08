using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Directory;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Admin.Data;
using Spectrometre.Modules.Admin.Entities;

namespace Spectrometre.Modules.Admin.Services;

/// <summary>Implémentation réelle de <see cref="IAdminService"/> — voir sa remarque pour la garde d'autorisation systématique.</summary>
public sealed class AdminService(
    IDbContextFactory<CoreDbContext> coreDbFactory,
    IDbContextFactory<AdminDbContext> adminDbFactory,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IModuleRegistry moduleRegistry,
    ICandidateDirectoryService candidateDirectory,
    ICoachDirectoryService coachDirectory,
    ICoachingLinkOverviewService coachingLinkOverview) : IAdminService
{
    private static void EnsureAdmin(ClaimsPrincipal caller)
    {
        if (!caller.IsInRole(PlatformRoles.Admin))
            throw new UnauthorizedAccessException("Accès réservé aux administrateurs de la plateforme.");
    }

    public async Task<IReadOnlyList<AdminCompanySummary>> GetEntreprisesAsync(ClaimsPrincipal caller, CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);

        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);

        var companies = await coreDb.Companies.AsNoTracking().ToListAsync(cancellationToken);
        var subscriptions = await coreDb.TenantSubscriptions.AsNoTracking().ToDictionaryAsync(s => s.CompanyId, cancellationToken);
        var activations = await coreDb.ModuleActivations.AsNoTracking()
            .Where(a => a.SubjectType == ModuleActivationSubjectType.Company && a.IsActive)
            .ToListAsync(cancellationToken);
        var modulesByCompany = activations.GroupBy(a => a.SubjectId).ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(a => a.ModuleCode).ToList());

        var owners = await coreDb.UserCompanyLinks.AsNoTracking()
            .Where(l => l.Role == CompanyRole.Proprietaire)
            .ToListAsync(cancellationToken);
        var ownerUserIdByCompany = owners.GroupBy(l => l.CompanyId).ToDictionary(g => g.Key, g => g.First().UserId);
        var emailByUserId = await ResolveEmailsAsync(ownerUserIdByCompany.Values, cancellationToken);

        return companies.Select(c =>
        {
            subscriptions.TryGetValue(c.Id, out var sub);
            modulesByCompany.TryGetValue(c.Id, out var modules);
            string? ownerEmail = null;
            if (ownerUserIdByCompany.TryGetValue(c.Id, out var ownerUserId))
                emailByUserId.TryGetValue(ownerUserId, out ownerEmail);

            return new AdminCompanySummary(
                c.Id,
                c.Name,
                ownerEmail,
                sub?.Status.ToString() ?? "AucunAbonnement",
                sub?.PlanCode ?? "-",
                modules ?? [],
                c.CreatedAt);
        }).ToList();
    }

    public async Task<IReadOnlyList<AdminCandidateSummary>> GetCandidatsAsync(ClaimsPrincipal caller, CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);

        var candidates = await candidateDirectory.GetAllAsync(cancellationToken);

        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        var subscriptions = await coreDb.CandidateSubscriptions.AsNoTracking().ToDictionaryAsync(s => s.CandidateProfileId, cancellationToken);
        var activations = await coreDb.ModuleActivations.AsNoTracking()
            .Where(a => a.SubjectType == ModuleActivationSubjectType.Candidate && a.IsActive)
            .ToListAsync(cancellationToken);
        var modulesByCandidate = activations.GroupBy(a => a.SubjectId).ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(a => a.ModuleCode).ToList());
        var emailByUserId = await ResolveEmailsAsync(candidates.Select(c => c.UserId), cancellationToken);

        return candidates.Select(c =>
        {
            subscriptions.TryGetValue(c.CandidateProfileId, out var sub);
            modulesByCandidate.TryGetValue(c.CandidateProfileId, out var modules);
            emailByUserId.TryGetValue(c.UserId, out var email);

            return new AdminCandidateSummary(
                c.CandidateProfileId,
                c.UserId,
                email,
                sub?.Status.ToString() ?? "AucunAbonnement",
                sub?.PlanCode,
                modules ?? [],
                c.CreatedAt);
        }).ToList();
    }

    public async Task<IReadOnlyList<AdminCoachSummary>> GetCoachsAsync(ClaimsPrincipal caller, CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);

        var coachs = await coachDirectory.GetAllAsync(cancellationToken);
        var liens = await coachingLinkOverview.GetAllAsync(cancellationToken);
        var suivisParCoach = liens
            .Where(l => l.Statut == "Actif")
            .GroupBy(l => l.CoachUserId)
            .ToDictionary(g => g.Key, g => g.Count());

        var emailByUserId = await ResolveEmailsAsync(coachs.Select(c => c.UserId), cancellationToken);

        return coachs.Select(c =>
        {
            emailByUserId.TryGetValue(c.UserId, out var email);
            suivisParCoach.TryGetValue(c.UserId, out var nombreSuivis);

            return new AdminCoachSummary(c.CoachProfileId, c.UserId, email, c.NomAffiche, c.VisibleDansAnnuaire, nombreSuivis, c.CreatedAt);
        }).ToList();
    }

    public async Task<IReadOnlyList<AdminCoachingLinkSummary>> GetLiensCoachingAsync(ClaimsPrincipal caller, CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);

        var liens = await coachingLinkOverview.GetAllAsync(cancellationToken);
        var userIds = liens.SelectMany(l => new[] { l.SuiviUserId, l.CoachUserId });
        var emailByUserId = await ResolveEmailsAsync(userIds, cancellationToken);

        return liens.Select(l =>
        {
            emailByUserId.TryGetValue(l.SuiviUserId, out var suiviEmail);
            emailByUserId.TryGetValue(l.CoachUserId, out var coachEmail);
            return new AdminCoachingLinkSummary(l.Id, suiviEmail ?? l.SuiviUserId, coachEmail ?? l.CoachUserId, l.Statut, l.CreatedAt, l.AccepteLe);
        }).ToList();
    }

    public async Task<IReadOnlyList<AdminInvitationSummary>> GetInvitationsAsync(ClaimsPrincipal caller, CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);

        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        var invitations = await coreDb.Invitations.AsNoTracking().ToListAsync(cancellationToken);
        var emailByUserId = await ResolveEmailsAsync(invitations.Select(i => i.EmetteurUserId), cancellationToken);

        return invitations.Select(i =>
        {
            emailByUserId.TryGetValue(i.EmetteurUserId, out var emetteurEmail);
            return new AdminInvitationSummary(i.Id, emetteurEmail ?? i.EmetteurUserId, i.EmailInvite, i.Type.ToString(), i.Statut.ToString(), i.CreatedAt, i.ExpireLe, i.AccepteeLe);
        }).ToList();
    }

    public async Task<AdminGlobalCounts> GetCompteursGlobauxAsync(ClaimsPrincipal caller, CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);

        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        var totalEntreprises = await coreDb.Companies.CountAsync(cancellationToken);
        var totalCandidats = (await candidateDirectory.GetAllAsync(cancellationToken)).Count;
        var totalCoachs = (await coachDirectory.GetAllAsync(cancellationToken)).Count;

        var repartition = await coreDb.ModuleActivations.AsNoTracking()
            .Where(a => a.IsActive)
            .GroupBy(a => a.ModuleCode)
            .Select(g => new { ModuleCode = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return new AdminGlobalCounts(totalEntreprises, totalCandidats, totalCoachs, repartition.ToDictionary(r => r.ModuleCode, r => r.Count));
    }

    public async Task<AdminSearchResult?> RechercherParEmailAsync(ClaimsPrincipal caller, string email, CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return null;

        var estAdmin = await userManager.IsInRoleAsync(user, PlatformRoles.Admin);

        // Coût O(n) sur le répertoire complet — acceptable au volume attendu pour ce premier cycle (voir
        // la remarque sur ICandidateDirectoryService : pas de méthode de recherche ciblée pour l'instant).
        var estCandidat = (await candidateDirectory.GetAllAsync(cancellationToken)).Any(c => c.UserId == user.Id);
        var estCoach = (await coachDirectory.GetAllAsync(cancellationToken)).Any(c => c.UserId == user.Id);

        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        var entreprises = await coreDb.UserCompanyLinks.AsNoTracking()
            .Where(l => l.UserId == user.Id)
            .Join(coreDb.Companies, l => l.CompanyId, c => c.Id, (l, c) => c.Name)
            .ToListAsync(cancellationToken);

        return new AdminSearchResult(user.Id, user.Email ?? user.UserName ?? user.Id, estAdmin, estCandidat, estCoach, entreprises);
    }

    public async Task<IReadOnlyList<AdminAccountSummary>> GetAdministrateursAsync(ClaimsPrincipal caller, CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);

        var admins = await userManager.GetUsersInRoleAsync(PlatformRoles.Admin);
        return admins
            .OrderBy(u => u.Email)
            .Select(u => new AdminAccountSummary(u.Id, u.Email ?? u.UserName ?? u.Id))
            .ToList();
    }

    public async Task<AdminActionOutcome> PromouvoirAsync(ClaimsPrincipal caller, string targetUserId, CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);

        var user = await userManager.FindByIdAsync(targetUserId);
        if (user is null)
            return AdminActionOutcome.UtilisateurIntrouvable;

        if (await userManager.IsInRoleAsync(user, PlatformRoles.Admin))
            return AdminActionOutcome.DejaAdmin;

        if (!await roleManager.RoleExistsAsync(PlatformRoles.Admin))
            await roleManager.CreateAsync(new IdentityRole(PlatformRoles.Admin));

        await userManager.AddToRoleAsync(user, PlatformRoles.Admin);
        return AdminActionOutcome.Succes;
    }

    public async Task<AdminActionOutcome> RetrograderAsync(ClaimsPrincipal caller, string targetUserId, CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);

        var user = await userManager.FindByIdAsync(targetUserId);
        if (user is null)
            return AdminActionOutcome.UtilisateurIntrouvable;

        if (!await userManager.IsInRoleAsync(user, PlatformRoles.Admin))
            return AdminActionOutcome.PasAdmin;

        var currentAdmins = await userManager.GetUsersInRoleAsync(PlatformRoles.Admin);
        if (currentAdmins.Count <= 1)
            return AdminActionOutcome.DernierAdminRestant;

        await userManager.RemoveFromRoleAsync(user, PlatformRoles.Admin);
        return AdminActionOutcome.Succes;
    }

    public async Task DefinirActivationModuleAsync(ClaimsPrincipal caller, ModuleActivationSubjectType subjectType, int subjectId, string moduleCode, bool actif, CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);

        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        await moduleRegistry.SetActiveAsync(subjectType, subjectId, moduleCode, actif, coreDb, cancellationToken);

        await JournaliserAsync(caller, actif ? "ActivationModule" : "DesactivationModule", $"{subjectType} #{subjectId} / {moduleCode}", cancellationToken);
    }

    public async Task<(IReadOnlyList<string> Tous, IReadOnlyList<string> Actifs)> GetModulesPourSujetAsync(ClaimsPrincipal caller, ModuleActivationSubjectType subjectType, int subjectId, CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);

        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        var tous = moduleRegistry.AllModules.Select(m => m.Code).OrderBy(c => c).ToList();
        var actifs = await moduleRegistry.GetActiveModuleCodesAsync(subjectType, subjectId, coreDb, cancellationToken);
        return (tous, actifs);
    }

    public async Task<IReadOnlyList<AdminPlanView>> GetPlansAsync(ClaimsPrincipal caller, CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);

        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        var entitlements = await coreDb.PlanModuleEntitlements.AsNoTracking().ToListAsync(cancellationToken);

        return entitlements
            .GroupBy(e => e.PlanCode)
            .OrderBy(g => g.Key)
            .Select(g => new AdminPlanView(g.Key, g.Select(e => e.ModuleCode).OrderBy(m => m).ToList()))
            .ToList();
    }

    public async Task AjouterModuleAuPlanAsync(ClaimsPrincipal caller, string planCode, string moduleCode, CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);

        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        var existe = await coreDb.PlanModuleEntitlements.AnyAsync(e => e.PlanCode == planCode && e.ModuleCode == moduleCode, cancellationToken);
        if (existe)
            return;

        // Id assigné explicitement plutôt que laissé à la séquence Postgres : les lignes seedées via
        // HasData (voir CoreDbContext.SeedPlanModuleEntitlements) n'avancent jamais la séquence associée à
        // la colonne identité, un INSERT sans Id explicite risquerait donc une collision avec un Id seedé.
        var prochainId = (await coreDb.PlanModuleEntitlements.MaxAsync(e => (int?)e.Id, cancellationToken) ?? 0) + 1;
        coreDb.PlanModuleEntitlements.Add(new PlanModuleEntitlement { Id = prochainId, PlanCode = planCode, ModuleCode = moduleCode });
        await coreDb.SaveChangesAsync(cancellationToken);

        await JournaliserAsync(caller, "AjoutModuleAuPlan", $"Plan {planCode} / {moduleCode}", cancellationToken);
    }

    public async Task RetirerModuleDuPlanAsync(ClaimsPrincipal caller, string planCode, string moduleCode, CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);

        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        var entitlement = await coreDb.PlanModuleEntitlements.FirstOrDefaultAsync(e => e.PlanCode == planCode && e.ModuleCode == moduleCode, cancellationToken);
        if (entitlement is null)
            return;

        coreDb.PlanModuleEntitlements.Remove(entitlement);
        await coreDb.SaveChangesAsync(cancellationToken);

        await JournaliserAsync(caller, "RetraitModuleDuPlan", $"Plan {planCode} / {moduleCode}", cancellationToken);
    }

    public async Task<IReadOnlyList<AdminAuditEntryView>> GetHistoriqueAsync(ClaimsPrincipal caller, int take = 50, CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);

        await using var adminDb = await adminDbFactory.CreateDbContextAsync(cancellationToken);
        var entries = await adminDb.AuditLog.AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        var emailByUserId = await ResolveEmailsAsync(entries.Select(e => e.AdminUserId), cancellationToken);

        return entries.Select(e =>
        {
            emailByUserId.TryGetValue(e.AdminUserId, out var email);
            return new AdminAuditEntryView(e.Id, email ?? e.AdminUserId, e.Action, e.Cible, e.CreatedAt);
        }).ToList();
    }

    private async Task JournaliserAsync(ClaimsPrincipal caller, string action, string cible, CancellationToken cancellationToken)
    {
        var adminUserId = caller.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "inconnu";

        await using var adminDb = await adminDbFactory.CreateDbContextAsync(cancellationToken);
        adminDb.AuditLog.Add(new AdminAuditLogEntry { AdminUserId = adminUserId, Action = action, Cible = cible });
        await adminDb.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Résolution par lot des emails (une seule requête) — évite le N+1 d'un <c>FindByIdAsync</c> par ligne sur chaque vue.</summary>
    private async Task<Dictionary<string, string?>> ResolveEmailsAsync(IEnumerable<string> userIds, CancellationToken cancellationToken)
    {
        var distinctIds = userIds.Distinct().ToList();
        if (distinctIds.Count == 0)
            return [];

        return await userManager.Users
            .Where(u => distinctIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email, cancellationToken);
    }
}
