using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Data;

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
}

public sealed partial class CompanyProvisioningService(ITenantSchemaNameGenerator schemaNameGenerator) : ICompanyProvisioningService
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
