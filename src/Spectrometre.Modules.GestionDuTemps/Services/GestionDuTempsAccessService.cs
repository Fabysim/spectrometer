using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Data;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Tenancy;

namespace Spectrometre.Modules.GestionDuTemps.Services;

/// <summary>
/// <see cref="CoreDbContext"/> obtenu via <see cref="IDbContextFactory{TContext}"/>, pas injecté
/// directement — même raison que partout ailleurs (voir <c>ProfileChangeRecorder</c>) : cette vérification
/// est appelée depuis <c>OnInitializedAsync</c> de la page ET du layout, potentiellement dans des circuits
/// qui se chevauchent, un <c>CoreDbContext</c> scoped partagé planterait sous usage concurrent.
/// </summary>
public sealed class GestionDuTempsAccessService(
    IDbContextFactory<CoreDbContext> coreDbFactory,
    IModuleRegistry moduleRegistry,
    ICandidateSubjectResolver candidateSubjectResolver,
    ICompanyProvisioningService companyProvisioningService) : IGestionDuTempsAccessService
{
    public async Task<bool> HasAccessAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);

        var candidateProfileId = await candidateSubjectResolver.GetOrCreateCandidateProfileIdAsync(userId, cancellationToken);
        if (await moduleRegistry.IsActiveForCandidateAsync(candidateProfileId, ServiceCollectionExtensions.Manifest.Code, coreDb, cancellationToken))
            return true;

        var companies = await companyProvisioningService.GetCompaniesForUserAsync(userId, coreDb, cancellationToken);
        foreach (var company in companies)
        {
            if (await moduleRegistry.IsActiveAsync(company.Id, ServiceCollectionExtensions.Manifest.Code, coreDb, cancellationToken))
                return true;
        }

        return false;
    }

    public async Task<bool> HasCandidateAccessAsync(string userId, CancellationToken cancellationToken = default)
    {
        await using var coreDb = await coreDbFactory.CreateDbContextAsync(cancellationToken);

        var candidateProfileId = await candidateSubjectResolver.GetOrCreateCandidateProfileIdAsync(userId, cancellationToken);
        return await moduleRegistry.IsActiveForCandidateAsync(candidateProfileId, ServiceCollectionExtensions.Manifest.Code, coreDb, cancellationToken);
    }
}
