using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Data;
using Spectrometre.Core.Invitations;

namespace Spectrometre.Core.Tenancy;

/// <summary>
/// Création d'une entreprise (tenant) : génère un schéma Postgres unique et l'enregistre dans le noyau.
/// Les migrations des modules activés s'appliquent séparément à ce schéma (voir chaque <c>AddXxxModule</c>).
/// </summary>
public interface ICompanyProvisioningService
{
    Task<Company> CreateCompanyAsync(string companyName, string ownerUserId, CoreDbContext db, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Company>> GetCompaniesForUserAsync(string userId, CoreDbContext db, CancellationToken cancellationToken = default);

    Task<bool> UserHasAccessAsync(string userId, int companyId, CoreDbContext db, CancellationToken cancellationToken = default);

    /// <summary>
    /// Met à jour les coordonnées administratives (nom, TVA, adresse) — même périmètre que le MVP
    /// <c>UpdateCompanyAsync</c>. Ne touche pas au schéma Postgres (<see cref="Company.SchemaName"/>).
    /// </summary>
    Task<bool> UpdateCompanyDetailsAsync(
        string userId,
        int companyId,
        string name,
        string? vta,
        string? address,
        string? city,
        string? postalCode,
        string? country,
        CoreDbContext db,
        CancellationToken cancellationToken = default);

    // ── Managers (cycle multi-entreprise) ───────────────────────────────────
    //
    // Réutilise le mécanisme d'invitation générique déjà en place pour Coaching (voir IInvitationService) —
    // seul InvitationType.CompanyEmploye et ContextId=CompanyId sont nouveaux. Seul le PROPRIÉTAIRE d'une
    // entreprise peut inviter/révoquer un manager pour ce premier cycle (voir la demande d'origine) — les
    // managers eux-mêmes n'ont accès à aucune de ces méthodes d'écriture.

    /// <summary>Émet une invitation manager pour <paramref name="companyId"/> — <c>null</c> si <paramref name="ownerUserId"/> n'est pas Propriétaire de cette entreprise.</summary>
    Task<Invitation?> InviterEmployeAsync(string ownerUserId, int companyId, string email, CoreDbContext db, CancellationToken cancellationToken = default);

    /// <summary>Managers déjà rattachés (Role = Manager) à cette entreprise.</summary>
    Task<IReadOnlyList<UserCompanyLink>> GetEmployesAsync(int companyId, CoreDbContext db, CancellationToken cancellationToken = default);

    /// <summary>Invitations manager en attente pour cette entreprise (même filtrage que <see cref="IInvitationService.ObtenirEmisesParAsync"/>, restreint au contexte de cette entreprise).</summary>
    Task<IReadOnlyList<Invitation>> GetInvitationsEmployeEnCoursAsync(int companyId, CoreDbContext db, CancellationToken cancellationToken = default);

    /// <summary>Révoque le rattachement d'un manager — <c>false</c> si <paramref name="ownerUserId"/> n'est pas Propriétaire, ou si aucun tel rattachement n'existe.</summary>
    Task<bool> RevokeEmployeAsync(string ownerUserId, int companyId, string employeUserId, CoreDbContext db, CancellationToken cancellationToken = default);

    /// <summary>Révoque une invitation manager en attente — seul le Propriétaire émetteur peut le faire.</summary>
    Task<bool> RevokeInvitationEmployeAsync(string ownerUserId, int invitationId, CoreDbContext db, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finalise le rattachement suite à l'acceptation d'une invitation manager — appelé depuis
    /// <c>InvitationAcceptancePage</c>, jamais directement. Idempotent : si le rattachement existe déjà
    /// (ex. double clic), ne fait rien plutôt que d'échouer sur l'index unique (UserId, CompanyId).
    /// </summary>
    Task FinaliserEmployeDepuisInvitationAsync(Invitation invitation, string accepteurUserId, CoreDbContext db, CancellationToken cancellationToken = default);
}

public sealed partial class CompanyProvisioningService(ITenantSchemaNameGenerator schemaNameGenerator, IInvitationService invitationService) : ICompanyProvisioningService
{
    public async Task<Company> CreateCompanyAsync(string companyName, string ownerUserId, CoreDbContext db, CancellationToken cancellationToken = default)
    {
        var baseSchema = schemaNameGenerator.GenerateSchemaName(companyName);
        var schema = await EnsureUniqueSchemaNameAsync(baseSchema, db, cancellationToken);

        await db.Database.ExecuteSqlRawAsync(
            "CREATE SCHEMA IF NOT EXISTS " + QuoteValidatedSchema(schema) + ";", cancellationToken);

        var company = new Company { Name = companyName, SchemaName = schema };
        db.Companies.Add(company);
        await db.SaveChangesAsync(cancellationToken);

        db.UserCompanyLinks.Add(new UserCompanyLink
        {
            UserId = ownerUserId,
            CompanyId = company.Id,
            Role = CompanyRole.Proprietaire,
        });
        await db.SaveChangesAsync(cancellationToken);

        return company;
    }

    public async Task<IReadOnlyList<Company>> GetCompaniesForUserAsync(string userId, CoreDbContext db, CancellationToken cancellationToken = default) =>
        await db.UserCompanyLinks
            .Where(l => l.UserId == userId)
            .Select(l => l.Company!)
            .ToListAsync(cancellationToken);

    public Task<bool> UserHasAccessAsync(string userId, int companyId, CoreDbContext db, CancellationToken cancellationToken = default) =>
        db.UserCompanyLinks.AnyAsync(l => l.UserId == userId && l.CompanyId == companyId, cancellationToken);

    public async Task<bool> UpdateCompanyDetailsAsync(
        string userId,
        int companyId,
        string name,
        string? vta,
        string? address,
        string? city,
        string? postalCode,
        string? country,
        CoreDbContext db,
        CancellationToken cancellationToken = default)
    {
        if (!await UserHasAccessAsync(userId, companyId, db, cancellationToken))
            return false;

        var company = await db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company is null)
            return false;

        var trimmedName = name?.Trim() ?? "";
        if (string.IsNullOrEmpty(trimmedName))
            return false;

        company.Name = trimmedName;
        company.VTA = NormalizeOptional(vta);
        company.Address = NormalizeOptional(address);
        company.City = NormalizeOptional(city);
        company.PostalCode = NormalizeOptional(postalCode);
        company.Country = NormalizeOptional(country);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static async Task<bool> IsOwnerAsync(string userId, int companyId, CoreDbContext db, CancellationToken cancellationToken) =>
        await db.UserCompanyLinks.AnyAsync(l => l.UserId == userId && l.CompanyId == companyId && l.Role == CompanyRole.Proprietaire, cancellationToken);

    public async Task<Invitation?> InviterEmployeAsync(string ownerUserId, int companyId, string email, CoreDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await IsOwnerAsync(ownerUserId, companyId, db, cancellationToken))
            return null;

        return await invitationService.CreerAsync(ownerUserId, email, InvitationType.CompanyEmploye, companyId, db, cancellationToken);
    }

    public async Task<IReadOnlyList<UserCompanyLink>> GetEmployesAsync(int companyId, CoreDbContext db, CancellationToken cancellationToken = default) =>
        await db.UserCompanyLinks
            .Where(l => l.CompanyId == companyId && l.Role == CompanyRole.Employe)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Invitation>> GetInvitationsEmployeEnCoursAsync(int companyId, CoreDbContext db, CancellationToken cancellationToken = default) =>
        await db.Invitations
            .Where(i => i.Type == InvitationType.CompanyEmploye && i.ContextId == companyId && i.Statut == InvitationStatus.EnAttente)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<bool> RevokeEmployeAsync(string ownerUserId, int companyId, string employeUserId, CoreDbContext db, CancellationToken cancellationToken = default)
    {
        if (!await IsOwnerAsync(ownerUserId, companyId, db, cancellationToken))
            return false;

        var link = await db.UserCompanyLinks.FirstOrDefaultAsync(
            l => l.UserId == employeUserId && l.CompanyId == companyId && l.Role == CompanyRole.Employe, cancellationToken);
        if (link is null)
            return false;

        db.UserCompanyLinks.Remove(link);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RevokeInvitationEmployeAsync(string ownerUserId, int invitationId, CoreDbContext db, CancellationToken cancellationToken = default) =>
        await invitationService.RevoquerAsync(invitationId, ownerUserId, db, cancellationToken);

    public async Task FinaliserEmployeDepuisInvitationAsync(Invitation invitation, string accepteurUserId, CoreDbContext db, CancellationToken cancellationToken = default)
    {
        var companyId = invitation.ContextId
            ?? throw new InvalidOperationException("Invitation manager sans ContextId (CompanyId) — invitation mal formée.");

        var dejaRattache = await db.UserCompanyLinks.AnyAsync(
            l => l.UserId == accepteurUserId && l.CompanyId == companyId, cancellationToken);
        if (dejaRattache)
            return;

        db.UserCompanyLinks.Add(new UserCompanyLink
        {
            UserId = accepteurUserId,
            CompanyId = companyId,
            Role = CompanyRole.Employe,
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static async Task<string> EnsureUniqueSchemaNameAsync(string baseSchema, CoreDbContext db, CancellationToken cancellationToken)
    {
        var schema = baseSchema;
        var suffix = 1;
        while (await db.Companies.AnyAsync(c => c.SchemaName == schema, cancellationToken))
        {
            suffix++;
            schema = $"{baseSchema}_{suffix}";
        }
        return schema;
    }

    private static string QuoteValidatedSchema(string schema)
    {
        if (!ValidSchemaName().IsMatch(schema))
            throw new InvalidOperationException("Schéma invalide.");
        return "\"" + schema + "\"";
    }

    [GeneratedRegex(@"^[a-z][a-z0-9_]{0,62}$")]
    private static partial Regex ValidSchemaName();
}
