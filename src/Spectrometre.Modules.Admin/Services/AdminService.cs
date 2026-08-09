using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Directory;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Invitations;
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
    ICoachingLinkOverviewService coachingLinkOverview,
    IFacturationCalculatorService facturationCalculator) : IAdminService
{
    private static void EnsureAdmin(ClaimsPrincipal caller)
    {
        if (!caller.IsInRole(PlatformRoles.Admin))
            throw new UnauthorizedAccessException("Accès réservé aux administrateurs de la plateforme.");
    }

    private static string? NormalizeRecherche(string? recherche) =>
        string.IsNullOrWhiteSpace(recherche) ? null : recherche.Trim();

    private async Task<List<string>> FindUserIdsByEmailContainsAsync(string term, CancellationToken ct)
    {
        var lowered = term.ToLowerInvariant();
        return await userManager.Users
            .Where(u => u.Email != null && u.Email.ToLower().Contains(lowered))
            .Select(u => u.Id)
            .ToListAsync(ct);
    }

    public async Task<AdminPagedResult<AdminCompanySummary>> GetEntreprisesAsync(
        ClaimsPrincipal caller,
        int page = 1,
        int pageSize = AdminPaging.DefaultPageSize,
        string? recherche = null,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);
        (page, pageSize) = AdminPaging.Normalize(page, pageSize);
        var term = NormalizeRecherche(recherche);

        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<Company> companiesQuery = coreDb.Companies.AsNoTracking();

        if (term is not null)
        {
            var lowered = term.ToLowerInvariant();
            var matchingUserIds = await FindUserIdsByEmailContainsAsync(term, cancellationToken);
            var ownerCompanyIds = matchingUserIds.Count == 0
                ? []
                : await coreDb.UserCompanyLinks.AsNoTracking()
                    .Where(l => l.Role == CompanyRole.Proprietaire && matchingUserIds.Contains(l.UserId))
                    .Select(l => l.CompanyId)
                    .Distinct()
                    .ToListAsync(cancellationToken);
            var planCompanyIds = await coreDb.TenantSubscriptions.AsNoTracking()
                .Where(s => s.PlanCode.ToLower().Contains(lowered))
                .Select(s => s.CompanyId)
                .ToListAsync(cancellationToken);

            companiesQuery = companiesQuery.Where(c =>
                c.Name.ToLower().Contains(lowered)
                || ownerCompanyIds.Contains(c.Id)
                || planCompanyIds.Contains(c.Id));
        }

        var total = await companiesQuery.CountAsync(cancellationToken);
        var companies = await companiesQuery
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var companyIds = companies.Select(c => c.Id).ToList();
        var subscriptions = await coreDb.TenantSubscriptions.AsNoTracking()
            .Where(s => companyIds.Contains(s.CompanyId))
            .ToDictionaryAsync(s => s.CompanyId, cancellationToken);
        var activations = await coreDb.ModuleActivations.AsNoTracking()
            .Where(a => a.SubjectType == ModuleActivationSubjectType.Company && a.IsActive && companyIds.Contains(a.SubjectId))
            .ToListAsync(cancellationToken);
        var modulesByCompany = activations.GroupBy(a => a.SubjectId).ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(a => a.ModuleCode).ToList());

        var owners = await coreDb.UserCompanyLinks.AsNoTracking()
            .Where(l => l.Role == CompanyRole.Proprietaire && companyIds.Contains(l.CompanyId))
            .ToListAsync(cancellationToken);
        var ownerUserIdByCompany = owners.GroupBy(l => l.CompanyId).ToDictionary(g => g.Key, g => g.First().UserId);
        var emailByUserId = await ResolveEmailsAsync(ownerUserIdByCompany.Values, cancellationToken);

        var items = companies.Select(c =>
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

        return new AdminPagedResult<AdminCompanySummary>(items, total, page, pageSize);
    }

    public async Task<AdminPagedResult<AdminCandidateSummary>> GetCandidatsAsync(
        ClaimsPrincipal caller,
        int page = 1,
        int pageSize = AdminPaging.DefaultPageSize,
        string? recherche = null,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);
        (page, pageSize) = AdminPaging.Normalize(page, pageSize);
        var term = NormalizeRecherche(recherche);

        int total;
        IReadOnlyList<CandidateDirectoryEntry> candidates;

        if (term is null)
        {
            total = await candidateDirectory.CountAsync(cancellationToken: cancellationToken);
            candidates = await candidateDirectory.GetPageAsync((page - 1) * pageSize, pageSize, cancellationToken: cancellationToken);
        }
        else
        {
            var emailIds = await FindUserIdsByEmailContainsAsync(term, cancellationToken);
            await using var coreDbForSearch = await coreDbFactory.CreateDbContextAsync(cancellationToken);
            var lowered = term.ToLowerInvariant();
            var planIds = await coreDbForSearch.CandidateSubscriptions.AsNoTracking()
                .Where(s => s.PlanCode.ToLower().Contains(lowered))
                .Select(s => s.CandidateProfileId)
                .ToListAsync(cancellationToken);

            if (emailIds.Count == 0 && planIds.Count == 0)
                return new AdminPagedResult<AdminCandidateSummary>([], 0, page, pageSize);

            total = await candidateDirectory.CountAsync(matchingUserIds: emailIds, matchingProfileIds: planIds, cancellationToken: cancellationToken);
            candidates = await candidateDirectory.GetPageAsync(
                (page - 1) * pageSize,
                pageSize,
                matchingUserIds: emailIds,
                matchingProfileIds: planIds,
                cancellationToken: cancellationToken);
        }

        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        var candidateIds = candidates.Select(c => c.CandidateProfileId).ToList();
        var subscriptions = await coreDb.CandidateSubscriptions.AsNoTracking()
            .Where(s => candidateIds.Contains(s.CandidateProfileId))
            .ToDictionaryAsync(s => s.CandidateProfileId, cancellationToken);
        var activations = await coreDb.ModuleActivations.AsNoTracking()
            .Where(a => a.SubjectType == ModuleActivationSubjectType.Candidate && a.IsActive && candidateIds.Contains(a.SubjectId))
            .ToListAsync(cancellationToken);
        var modulesByCandidate = activations.GroupBy(a => a.SubjectId).ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(a => a.ModuleCode).ToList());
        var emailByUserId = await ResolveEmailsAsync(candidates.Select(c => c.UserId), cancellationToken);

        var items = candidates.Select(c =>
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

        return new AdminPagedResult<AdminCandidateSummary>(items, total, page, pageSize);
    }

    public async Task<AdminPagedResult<AdminCoachSummary>> GetCoachsAsync(
        ClaimsPrincipal caller,
        int page = 1,
        int pageSize = AdminPaging.DefaultPageSize,
        string? recherche = null,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);
        (page, pageSize) = AdminPaging.Normalize(page, pageSize);
        var term = NormalizeRecherche(recherche);

        int total;
        IReadOnlyList<CoachDirectoryEntry> coachs;

        if (term is null)
        {
            total = await coachDirectory.CountAsync(cancellationToken: cancellationToken);
            coachs = await coachDirectory.GetPageAsync((page - 1) * pageSize, pageSize, cancellationToken: cancellationToken);
        }
        else
        {
            var emailIds = await FindUserIdsByEmailContainsAsync(term, cancellationToken);
            await using var coreDbForSearch = await coreDbFactory.CreateDbContextAsync(cancellationToken);
            var lowered = term.ToLowerInvariant();
            var planIds = await coreDbForSearch.CoachSubscriptions.AsNoTracking()
                .Where(s => s.PlanCode.ToLower().Contains(lowered))
                .Select(s => s.CoachProfileId)
                .ToListAsync(cancellationToken);

            total = await coachDirectory.CountAsync(
                recherche: term,
                matchingUserIds: emailIds,
                matchingProfileIds: planIds,
                cancellationToken: cancellationToken);
            coachs = await coachDirectory.GetPageAsync(
                (page - 1) * pageSize,
                pageSize,
                recherche: term,
                matchingUserIds: emailIds,
                matchingProfileIds: planIds,
                cancellationToken: cancellationToken);
        }

        // Compteurs de suivis : charge légère sur les liens actifs (pas le détail paginé des liens).
        var liens = await coachingLinkOverview.GetAllAsync(cancellationToken);
        var suivisParCoach = liens
            .Where(l => l.Statut == "Actif")
            .GroupBy(l => l.CoachUserId)
            .ToDictionary(g => g.Key, g => g.Count());

        var coachIds = coachs.Select(c => c.CoachProfileId).ToList();
        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        var activations = await coreDb.ModuleActivations.AsNoTracking()
            .Where(a => a.SubjectType == ModuleActivationSubjectType.Coach && a.IsActive && coachIds.Contains(a.SubjectId))
            .ToListAsync(cancellationToken);
        var modulesByCoach = activations.GroupBy(a => a.SubjectId).ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(a => a.ModuleCode).ToList());

        var emailByUserId = await ResolveEmailsAsync(coachs.Select(c => c.UserId), cancellationToken);

        var items = coachs.Select(c =>
        {
            emailByUserId.TryGetValue(c.UserId, out var email);
            suivisParCoach.TryGetValue(c.UserId, out var nombreSuivis);
            modulesByCoach.TryGetValue(c.CoachProfileId, out var modules);
            return new AdminCoachSummary(c.CoachProfileId, c.UserId, email, c.NomAffiche, c.VisibleDansAnnuaire, nombreSuivis, modules ?? [], c.CreatedAt);
        }).ToList();

        return new AdminPagedResult<AdminCoachSummary>(items, total, page, pageSize);
    }

    public async Task<AdminPagedResult<AdminCoachingLinkSummary>> GetLiensCoachingAsync(
        ClaimsPrincipal caller,
        int page = 1,
        int pageSize = AdminPaging.DefaultPageSize,
        string? recherche = null,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);
        (page, pageSize) = AdminPaging.Normalize(page, pageSize);
        var term = NormalizeRecherche(recherche);

        int total;
        IReadOnlyList<CoachingLinkSummary> liens;

        if (term is null)
        {
            total = await coachingLinkOverview.CountAsync(cancellationToken: cancellationToken);
            liens = await coachingLinkOverview.GetPageAsync((page - 1) * pageSize, pageSize, cancellationToken: cancellationToken);
        }
        else
        {
            var matchingUserIds = await FindUserIdsByEmailContainsAsync(term, cancellationToken);
            total = await coachingLinkOverview.CountAsync(
                recherche: term,
                matchingUserIds: matchingUserIds,
                cancellationToken: cancellationToken);
            liens = await coachingLinkOverview.GetPageAsync(
                (page - 1) * pageSize,
                pageSize,
                recherche: term,
                matchingUserIds: matchingUserIds,
                cancellationToken: cancellationToken);
        }

        var userIds = liens.SelectMany(l => new[] { l.SuiviUserId, l.CoachUserId });
        var emailByUserId = await ResolveEmailsAsync(userIds, cancellationToken);

        var items = liens.Select(l =>
        {
            emailByUserId.TryGetValue(l.SuiviUserId, out var suiviEmail);
            emailByUserId.TryGetValue(l.CoachUserId, out var coachEmail);
            return new AdminCoachingLinkSummary(l.Id, suiviEmail ?? l.SuiviUserId, coachEmail ?? l.CoachUserId, l.Statut, l.CreatedAt, l.AccepteLe);
        }).ToList();

        return new AdminPagedResult<AdminCoachingLinkSummary>(items, total, page, pageSize);
    }

    public async Task<AdminPagedResult<AdminInvitationSummary>> GetInvitationsAsync(
        ClaimsPrincipal caller,
        int page = 1,
        int pageSize = AdminPaging.DefaultPageSize,
        string? recherche = null,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);
        (page, pageSize) = AdminPaging.Normalize(page, pageSize);
        var term = NormalizeRecherche(recherche);

        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<Invitation> invitationsQuery = coreDb.Invitations.AsNoTracking();

        if (term is not null)
        {
            var lowered = term.ToLowerInvariant();
            var matchingUserIds = await FindUserIdsByEmailContainsAsync(term, cancellationToken);
            var matchingStatuts = Enum.GetValues<InvitationStatus>()
                .Where(s => s.ToString().Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            var matchingTypes = Enum.GetValues<InvitationType>()
                .Where(t => t.ToString().Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            invitationsQuery = invitationsQuery.Where(i =>
                i.EmailInvite.ToLower().Contains(lowered)
                || matchingUserIds.Contains(i.EmetteurUserId)
                || matchingStatuts.Contains(i.Statut)
                || matchingTypes.Contains(i.Type));
        }

        var total = await invitationsQuery.CountAsync(cancellationToken);
        var invitations = await invitationsQuery
            .OrderByDescending(i => i.CreatedAt)
            .ThenBy(i => i.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var emailByUserId = await ResolveEmailsAsync(invitations.Select(i => i.EmetteurUserId), cancellationToken);

        var items = invitations.Select(i =>
        {
            emailByUserId.TryGetValue(i.EmetteurUserId, out var emetteurEmail);
            return new AdminInvitationSummary(i.Id, emetteurEmail ?? i.EmetteurUserId, i.EmailInvite, i.Type.ToString(), i.Statut.ToString(), i.CreatedAt, i.ExpireLe, i.AccepteeLe);
        }).ToList();

        return new AdminPagedResult<AdminInvitationSummary>(items, total, page, pageSize);
    }

    public async Task<AdminGlobalCounts> GetCompteursGlobauxAsync(ClaimsPrincipal caller, CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);

        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        var totalEntreprises = await coreDb.Companies.CountAsync(cancellationToken);
        var totalCandidats = await candidateDirectory.CountAsync(cancellationToken: cancellationToken);
        var totalCoachs = await coachDirectory.CountAsync(cancellationToken: cancellationToken);

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

    public async Task<AdminPagedResult<AdminAccountSummary>> GetAdministrateursAsync(
        ClaimsPrincipal caller,
        int page = 1,
        int pageSize = AdminPaging.DefaultPageSize,
        string? recherche = null,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);
        (page, pageSize) = AdminPaging.Normalize(page, pageSize);
        var term = NormalizeRecherche(recherche);

        // Identity GetUsersInRoleAsync ne expose pas de Skip/Take EF — volume attendu faible (admins plateforme).
        // Filtre email en mémoire pour la même raison (pas de requête IQueryable sur le rôle).
        var admins = (await userManager.GetUsersInRoleAsync(PlatformRoles.Admin))
            .OrderBy(u => u.Email)
            .AsEnumerable();

        if (term is not null)
        {
            var lowered = term.ToLowerInvariant();
            admins = admins.Where(u =>
                (u.Email ?? u.UserName ?? u.Id).ToLowerInvariant().Contains(lowered));
        }

        var filtered = admins.ToList();
        var total = filtered.Count;
        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminAccountSummary(u.Id, u.Email ?? u.UserName ?? u.Id))
            .ToList();

        return new AdminPagedResult<AdminAccountSummary>(items, total, page, pageSize);
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

    public Task<IReadOnlyList<AdminPlanView>> GetPlansAvecPrixAsync(
        ClaimsPrincipal caller,
        string? recherche = null,
        CancellationToken cancellationToken = default) =>
        GetPlansAsync(caller, recherche, cancellationToken);

    public async Task<IReadOnlyList<AdminPlanView>> GetPlansAsync(
        ClaimsPrincipal caller,
        string? recherche = null,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);
        var term = NormalizeRecherche(recherche);

        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        var entitlements = await coreDb.PlanModuleEntitlements.AsNoTracking().ToListAsync(cancellationToken);
        var plansByCode = await coreDb.Plans.AsNoTracking().ToDictionaryAsync(p => p.Code, cancellationToken);
        var prixByModule = await coreDb.ModulePrix.AsNoTracking()
            .ToDictionaryAsync(p => p.ModuleCode, cancellationToken);

        var modulesByPlan = entitlements
            .GroupBy(e => e.PlanCode)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(e => e.ModuleCode).OrderBy(m => m).ToList());

        var codes = modulesByPlan.Keys.Union(plansByCode.Keys).OrderBy(c => c).ToList();

        var views = codes.Select(code =>
        {
            plansByCode.TryGetValue(code, out var plan);
            modulesByPlan.TryGetValue(code, out var modules);
            modules ??= [];

            var detail = modules.Select(moduleCode =>
            {
                if (prixByModule.TryGetValue(moduleCode, out var prix))
                    return new AdminPlanModuleLigne(moduleCode, prix.PrixMensuel, prix.Devise, prix.Facturable, TarifDefini: true);

                // Pas de ligne dans ModulePrix : le tarif doit être saisi dans /admin/tarifs-modules.
                return new AdminPlanModuleLigne(moduleCode, 0m, "EUR", Facturable: false, TarifDefini: false);
            }).ToList();

            var facturables = detail.Where(d => d.TarifDefini && d.Facturable).ToList();
            var total = facturables.Sum(d => d.PrixMensuel);
            var deviseTotale = facturables
                .Select(d => d.Devise)
                .FirstOrDefault(d => !string.IsNullOrWhiteSpace(d))
                ?? "EUR";

            return new AdminPlanView(
                code,
                plan?.Nom ?? code,
                plan?.PrixMontant ?? 0m,
                plan?.PrixDevise ?? "EUR",
                plan?.Periodicite ?? PeriodicitePlan.Mensuel,
                plan?.Actif ?? true,
                modules,
                detail,
                total,
                deviseTotale);
        }).ToList();

        if (term is not null)
        {
            var lowered = term.ToLowerInvariant();
            views = views.Where(p =>
                p.PlanCode.ToLowerInvariant().Contains(lowered)
                || p.Nom.ToLowerInvariant().Contains(lowered)
                || p.ModulesInclus.Any(m => m.ToLowerInvariant().Contains(lowered))).ToList();
        }

        return views;
    }

    public async Task SetPrixPlanAsync(
        ClaimsPrincipal caller,
        string planCode,
        decimal prixMontant,
        string prixDevise,
        PeriodicitePlan periodicite,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);
        ArgumentException.ThrowIfNullOrWhiteSpace(planCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(prixDevise);

        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        var plan = await coreDb.Plans.FirstOrDefaultAsync(p => p.Code == planCode, cancellationToken);
        if (plan is null)
        {
            var prochainId = (await coreDb.Plans.MaxAsync(p => (int?)p.Id, cancellationToken) ?? 0) + 1;
            plan = new Plan
            {
                Id = prochainId,
                Code = planCode.Trim(),
                Nom = planCode.Trim(),
                PrixMontant = prixMontant,
                PrixDevise = prixDevise.Trim().ToUpperInvariant(),
                Periodicite = periodicite,
                Actif = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            coreDb.Plans.Add(plan);
        }
        else
        {
            plan.PrixMontant = prixMontant;
            plan.PrixDevise = prixDevise.Trim().ToUpperInvariant();
            plan.Periodicite = periodicite;
        }

        await coreDb.SaveChangesAsync(cancellationToken);
        await JournaliserAsync(caller, "SetPrixPlan", $"Plan {planCode} → {prixMontant} {prixDevise}/{periodicite}", cancellationToken);
    }

    public async Task SetPlanActifAsync(ClaimsPrincipal caller, string planCode, bool actif, CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);
        ArgumentException.ThrowIfNullOrWhiteSpace(planCode);

        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        var plan = await coreDb.Plans.FirstOrDefaultAsync(p => p.Code == planCode, cancellationToken);
        if (plan is null)
        {
            var prochainId = (await coreDb.Plans.MaxAsync(p => (int?)p.Id, cancellationToken) ?? 0) + 1;
            plan = new Plan
            {
                Id = prochainId,
                Code = planCode.Trim(),
                Nom = planCode.Trim(),
                PrixMontant = 0m,
                PrixDevise = "EUR",
                Periodicite = PeriodicitePlan.Mensuel,
                Actif = actif,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            coreDb.Plans.Add(plan);
        }
        else
        {
            plan.Actif = actif;
        }

        await coreDb.SaveChangesAsync(cancellationToken);
        await JournaliserAsync(caller, actif ? "ActiverPlan" : "DesactiverPlan", $"Plan {planCode}", cancellationToken);
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

    public async Task<AdminPagedResult<AdminAbonnementFacturationView>> GetAbonnementsFacturationAsync(
        ClaimsPrincipal caller,
        int page = 1,
        int pageSize = AdminPaging.DefaultPageSize,
        string? recherche = null,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);
        (page, pageSize) = AdminPaging.Normalize(page, pageSize);
        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        return await BuildAbonnementsPagedAsync(coreDb, enRetardUniquement: false, page, pageSize, recherche, cancellationToken);
    }

    public async Task<AdminPagedResult<AdminAbonnementFacturationView>> GetAbonnementsEnRetardAsync(
        ClaimsPrincipal caller,
        int page = 1,
        int pageSize = AdminPaging.DefaultPageSize,
        string? recherche = null,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);
        (page, pageSize) = AdminPaging.Normalize(page, pageSize);
        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        return await BuildAbonnementsPagedAsync(coreDb, enRetardUniquement: true, page, pageSize, recherche, cancellationToken);
    }

    public async Task EnregistrerPaiementAsync(
        ClaimsPrincipal caller,
        ModuleActivationSubjectType subjectType,
        int subjectId,
        string planCode,
        decimal montant,
        string devise,
        DateOnly dateReception,
        string moyen,
        DateOnly periodeDebut,
        DateOnly periodeFin,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);
        ArgumentException.ThrowIfNullOrWhiteSpace(planCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(devise);
        ArgumentException.ThrowIfNullOrWhiteSpace(moyen);

        var notePar = await ResolveAdminNoteParAsync(caller, cancellationToken);

        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);

        var montantDu = await facturationCalculator.CalculerMontantDuAsync(subjectType, subjectId, coreDb, cancellationToken);
        var modulesFactures = string.Join(',', montantDu.Lignes.Select(l => l.ModuleCode));

        coreDb.PaiementsEnregistres.Add(new PaiementEnregistre
        {
            SubjectType = subjectType,
            SubjectId = subjectId,
            PlanCode = planCode.Trim(),
            ModulesFactures = string.IsNullOrEmpty(modulesFactures) ? null : modulesFactures,
            Montant = montant,
            Devise = devise.Trim().ToUpperInvariant(),
            DateReception = dateReception,
            Moyen = moyen.Trim(),
            PeriodeCouverteDebut = periodeDebut,
            PeriodeCouverteFin = periodeFin,
            NotePar = notePar,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        var renewal = new DateTimeOffset(periodeFin.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        switch (subjectType)
        {
            case ModuleActivationSubjectType.Company:
            {
                var sub = await coreDb.TenantSubscriptions.FirstOrDefaultAsync(s => s.CompanyId == subjectId, cancellationToken)
                    ?? throw new InvalidOperationException("Abonnement entreprise introuvable.");
                sub.Status = SubscriptionStatus.Active;
                sub.RenewalDate = renewal;
                if (!string.IsNullOrWhiteSpace(planCode))
                    sub.PlanCode = planCode.Trim();
                break;
            }
            case ModuleActivationSubjectType.Candidate:
            {
                var sub = await coreDb.CandidateSubscriptions.FirstOrDefaultAsync(s => s.CandidateProfileId == subjectId, cancellationToken)
                    ?? throw new InvalidOperationException("Abonnement candidat introuvable.");
                sub.Status = SubscriptionStatus.Active;
                sub.RenewalDate = renewal;
                if (!string.IsNullOrWhiteSpace(planCode))
                    sub.PlanCode = planCode.Trim();
                break;
            }
            case ModuleActivationSubjectType.Coach:
            {
                var sub = await coreDb.CoachSubscriptions.FirstOrDefaultAsync(s => s.CoachProfileId == subjectId, cancellationToken)
                    ?? throw new InvalidOperationException("Abonnement coach introuvable.");
                sub.Status = SubscriptionStatus.Active;
                sub.RenewalDate = renewal;
                if (!string.IsNullOrWhiteSpace(planCode))
                    sub.PlanCode = planCode.Trim();
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(subjectType));
        }

        await coreDb.SaveChangesAsync(cancellationToken);
        await JournaliserAsync(caller, "EnregistrerPaiement", $"{subjectType} #{subjectId} / {planCode} / {montant} {devise}", cancellationToken);
    }

    public async Task<AdminPagedResult<AdminPaiementView>> GetHistoriquePaiementsAsync(
        ClaimsPrincipal caller,
        ModuleActivationSubjectType subjectType,
        int subjectId,
        int page = 1,
        int pageSize = AdminPaging.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);
        (page, pageSize) = AdminPaging.Normalize(page, pageSize);
        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);

        var query = coreDb.PaiementsEnregistres.AsNoTracking()
            .Where(p => p.SubjectType == subjectType && p.SubjectId == subjectId);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new AdminPaiementView(
                p.Id,
                p.PlanCode,
                p.ModulesFactures,
                p.Montant,
                p.Devise,
                p.DateReception,
                p.Moyen,
                p.PeriodeCouverteDebut,
                p.PeriodeCouverteFin,
                p.NotePar,
                p.CreatedAt))
            .ToListAsync(cancellationToken);

        return new AdminPagedResult<AdminPaiementView>(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<AdminModulePrixView>> GetModulePrixAsync(ClaimsPrincipal caller, CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);
        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);

        var existants = await coreDb.ModulePrix.AsNoTracking()
            .ToDictionaryAsync(p => p.ModuleCode, cancellationToken);

        // Catalogue complet : tout module connu du registre doit pouvoir être tarifé ici
        // (source de vérité consommée par /admin/plans et la facturation à la carte).
        var codes = moduleRegistry.AllModules
            .Select(m => m.Code)
            .Union(existants.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return codes.Select(code =>
        {
            if (existants.TryGetValue(code, out var row))
                return new AdminModulePrixView(row.ModuleCode, row.PrixMensuel, row.Devise, row.Facturable);

            var socle = EstModuleSocleNonFacturable(code);
            return new AdminModulePrixView(code, 0m, "EUR", Facturable: !socle);
        }).ToList();
    }

    private static bool EstModuleSocleNonFacturable(string moduleCode) =>
        moduleCode is "ProfilCandidat" or "ProfilEntreprise" or "ProfilCoach" or "Admin";

    public async Task SetModulePrixAsync(
        ClaimsPrincipal caller,
        string moduleCode,
        decimal prixMensuel,
        string devise,
        bool facturable,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(devise);

        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);
        var row = await coreDb.ModulePrix.FirstOrDefaultAsync(p => p.ModuleCode == moduleCode, cancellationToken);
        if (row is null)
        {
            var prochainId = (await coreDb.ModulePrix.MaxAsync(p => (int?)p.Id, cancellationToken) ?? 0) + 1;
            row = new ModulePrix
            {
                Id = prochainId,
                ModuleCode = moduleCode.Trim(),
                PrixMensuel = prixMensuel,
                Devise = devise.Trim().ToUpperInvariant(),
                Facturable = facturable,
                CreatedAt = DateTimeOffset.UtcNow,
            };
            coreDb.ModulePrix.Add(row);
        }
        else
        {
            row.PrixMensuel = prixMensuel;
            row.Devise = devise.Trim().ToUpperInvariant();
            row.Facturable = facturable;
        }

        await coreDb.SaveChangesAsync(cancellationToken);
        await JournaliserAsync(caller, "SetModulePrix", $"{moduleCode} → {prixMensuel} {devise} facturable={facturable}", cancellationToken);
    }

    public async Task SuspendreAbonnementAsync(
        ClaimsPrincipal caller,
        ModuleActivationSubjectType subjectType,
        int subjectId,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);
        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);

        switch (subjectType)
        {
            case ModuleActivationSubjectType.Company:
            {
                var sub = await coreDb.TenantSubscriptions.FirstOrDefaultAsync(s => s.CompanyId == subjectId, cancellationToken)
                    ?? throw new InvalidOperationException("Abonnement entreprise introuvable.");
                sub.Status = SubscriptionStatus.Suspendue;
                break;
            }
            case ModuleActivationSubjectType.Candidate:
            {
                var sub = await coreDb.CandidateSubscriptions.FirstOrDefaultAsync(s => s.CandidateProfileId == subjectId, cancellationToken)
                    ?? throw new InvalidOperationException("Abonnement candidat introuvable.");
                sub.Status = SubscriptionStatus.Suspendue;
                break;
            }
            case ModuleActivationSubjectType.Coach:
            {
                var sub = await coreDb.CoachSubscriptions.FirstOrDefaultAsync(s => s.CoachProfileId == subjectId, cancellationToken)
                    ?? throw new InvalidOperationException("Abonnement coach introuvable.");
                sub.Status = SubscriptionStatus.Suspendue;
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(subjectType));
        }

        await coreDb.SaveChangesAsync(cancellationToken);
        await JournaliserAsync(caller, "SuspendreAbonnement", $"{subjectType} #{subjectId}", cancellationToken);
    }

    private sealed record AbonnementStub(
        ModuleActivationSubjectType SubjectType,
        int SubjectId,
        string PlanCode,
        SubscriptionStatus Status,
        DateTimeOffset? RenewalDate,
        string? LibelleHint);

    /// <summary>
    /// Charge des stubs légers (sans <see cref="IFacturationCalculatorService.CalculerMontantDuAsync"/>),
    /// pagine, puis calcule le montant uniquement pour la page courante — cause principale de la lenteur
    /// historique de <c>/admin/facturation</c>.
    /// </summary>
    private async Task<AdminPagedResult<AdminAbonnementFacturationView>> BuildAbonnementsPagedAsync(
        CoreDbContext coreDb,
        bool enRetardUniquement,
        int page,
        int pageSize,
        string? recherche,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var stubs = new List<AbonnementStub>();
        var term = NormalizeRecherche(recherche);

        var companyRows = await (
            from sub in coreDb.TenantSubscriptions.AsNoTracking()
            join c in coreDb.Companies.AsNoTracking() on sub.CompanyId equals c.Id into cj
            from c in cj.DefaultIfEmpty()
            select new { sub.CompanyId, sub.PlanCode, sub.Status, sub.RenewalDate, Name = c != null ? c.Name : null }
        ).ToListAsync(cancellationToken);

        foreach (var row in companyRows)
        {
            if (enRetardUniquement && !EstEnRetard(row.Status, row.RenewalDate, today))
                continue;
            stubs.Add(new AbonnementStub(
                ModuleActivationSubjectType.Company,
                row.CompanyId,
                row.PlanCode,
                row.Status,
                row.RenewalDate,
                row.Name ?? $"Entreprise #{row.CompanyId}"));
        }

        var candidateSubs = await coreDb.CandidateSubscriptions.AsNoTracking().ToListAsync(cancellationToken);
        foreach (var sub in candidateSubs)
        {
            if (enRetardUniquement && !EstEnRetard(sub.Status, sub.RenewalDate, today))
                continue;
            stubs.Add(new AbonnementStub(
                ModuleActivationSubjectType.Candidate,
                sub.CandidateProfileId,
                sub.PlanCode,
                sub.Status,
                sub.RenewalDate,
                null));
        }

        var coachSubs = await coreDb.CoachSubscriptions.AsNoTracking().ToListAsync(cancellationToken);
        foreach (var sub in coachSubs)
        {
            if (enRetardUniquement && !EstEnRetard(sub.Status, sub.RenewalDate, today))
                continue;
            stubs.Add(new AbonnementStub(
                ModuleActivationSubjectType.Coach,
                sub.CoachProfileId,
                sub.PlanCode,
                sub.Status,
                sub.RenewalDate,
                null));
        }

        var ordered = stubs
            .OrderBy(a => a.SubjectType.ToString())
            .ThenBy(a => a.LibelleHint ?? a.SubjectId.ToString())
            .ToList();

        if (term is not null)
        {
            var emailIds = await FindUserIdsByEmailContainsAsync(term, cancellationToken);
            var emailIdSet = emailIds.ToHashSet();
            var candidates = await candidateDirectory.GetAllAsync(cancellationToken);
            var coaches = await coachDirectory.GetAllAsync(cancellationToken);
            var matchingCandidateIds = candidates
                .Where(c => emailIdSet.Contains(c.UserId))
                .Select(c => c.CandidateProfileId)
                .ToHashSet();
            var matchingCoachIds = coaches
                .Where(c => emailIdSet.Contains(c.UserId)
                    || c.NomAffiche.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Select(c => c.CoachProfileId)
                .ToHashSet();

            ordered = ordered.Where(s =>
                s.PlanCode.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (s.LibelleHint?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                || s.SubjectType.ToString().Contains(term, StringComparison.OrdinalIgnoreCase)
                || (s.SubjectType == ModuleActivationSubjectType.Candidate && matchingCandidateIds.Contains(s.SubjectId))
                || (s.SubjectType == ModuleActivationSubjectType.Coach && matchingCoachIds.Contains(s.SubjectId)))
                .ToList();
        }

        var total = ordered.Count;
        var pageStubs = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Libellés candidat/coach uniquement pour la page (évite ResolveEmails sur tout le catalogue).
        var candidateIds = pageStubs.Where(s => s.SubjectType == ModuleActivationSubjectType.Candidate).Select(s => s.SubjectId).ToHashSet();
        var coachIds = pageStubs.Where(s => s.SubjectType == ModuleActivationSubjectType.Coach).Select(s => s.SubjectId).ToHashSet();

        Dictionary<int, string> candidateLabels = [];
        if (candidateIds.Count > 0)
        {
            var candidates = await candidateDirectory.GetAllAsync(cancellationToken);
            var pageCandidates = candidates.Where(c => candidateIds.Contains(c.CandidateProfileId)).ToList();
            var emails = await ResolveEmailsAsync(pageCandidates.Select(c => c.UserId), cancellationToken);
            foreach (var c in pageCandidates)
            {
                emails.TryGetValue(c.UserId, out var email);
                candidateLabels[c.CandidateProfileId] = email ?? c.UserId;
            }
        }

        Dictionary<int, string> coachLabels = [];
        if (coachIds.Count > 0)
        {
            var coaches = await coachDirectory.GetAllAsync(cancellationToken);
            foreach (var c in coaches.Where(c => coachIds.Contains(c.CoachProfileId)))
                coachLabels[c.CoachProfileId] = string.IsNullOrWhiteSpace(c.NomAffiche) ? c.UserId : c.NomAffiche;
        }

        var items = new List<AdminAbonnementFacturationView>(pageStubs.Count);
        foreach (var stub in pageStubs)
        {
            var montant = await facturationCalculator.CalculerMontantDuAsync(
                stub.SubjectType, stub.SubjectId, coreDb, cancellationToken);

            var libelle = stub.SubjectType switch
            {
                ModuleActivationSubjectType.Company => stub.LibelleHint ?? $"Entreprise #{stub.SubjectId}",
                ModuleActivationSubjectType.Candidate => candidateLabels.GetValueOrDefault(stub.SubjectId, $"Candidat #{stub.SubjectId}"),
                ModuleActivationSubjectType.Coach => coachLabels.GetValueOrDefault(stub.SubjectId, $"Coach #{stub.SubjectId}"),
                _ => $"#{stub.SubjectId}"
            };

            items.Add(new AdminAbonnementFacturationView(
                stub.SubjectType,
                stub.SubjectId,
                libelle,
                stub.PlanCode,
                stub.Status,
                stub.RenewalDate,
                montant.Total,
                montant.Devise,
                montant.Lignes));
        }

        return new AdminPagedResult<AdminAbonnementFacturationView>(items, total, page, pageSize);
    }

    private static bool EstEnRetard(SubscriptionStatus status, DateTimeOffset? renewalDate, DateOnly today) =>
        status == SubscriptionStatus.Active
        && renewalDate is DateTimeOffset r
        && DateOnly.FromDateTime(r.UtcDateTime) < today;

    private async Task<string> ResolveAdminNoteParAsync(ClaimsPrincipal caller, CancellationToken cancellationToken)
    {
        var userId = caller.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return caller.Identity?.Name ?? "admin";

        var user = await userManager.FindByIdAsync(userId);
        return user?.Email ?? userId;
    }

    public async Task<AdminPagedResult<AdminAuditEntryView>> GetHistoriqueAsync(
        ClaimsPrincipal caller,
        int page = 1,
        int pageSize = AdminPaging.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(caller);
        (page, pageSize) = AdminPaging.Normalize(page, pageSize);

        await using var adminDb = await adminDbFactory.CreateDbContextAsync(cancellationToken);
        var total = await adminDb.AuditLog.CountAsync(cancellationToken);
        var entries = await adminDb.AuditLog.AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var emailByUserId = await ResolveEmailsAsync(entries.Select(e => e.AdminUserId), cancellationToken);
        var items = entries.Select(e =>
        {
            emailByUserId.TryGetValue(e.AdminUserId, out var email);
            return new AdminAuditEntryView(e.Id, email ?? e.AdminUserId, e.Action, e.Cible, e.CreatedAt);
        }).ToList();

        return new AdminPagedResult<AdminAuditEntryView>(items, total, page, pageSize);
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
