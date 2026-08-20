using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Modules;
using Spectrometre.Modules.ProfilCandidat.Services;
using ProfilCandidatModule = Spectrometre.Modules.ProfilCandidat.ServiceCollectionExtensions;
using GestionDuTempsModule = Spectrometre.Modules.GestionDuTemps.ServiceCollectionExtensions;

namespace Spectrometre.BackfillJeunesGdt;

/// <summary>
/// Copie locale de <see cref="Spectrometre.Host.Onboarding.CandidateOnboardingService"/> — cet outil
/// console ne référence pas Host (conflit de packages transitifs). Logique identique, à garder alignée.
/// </summary>
internal sealed class BackfillCandidateOnboarding(
    ICandidateProfileService candidateProfileService,
    IModuleRegistry moduleRegistry)
{
    private static readonly IReadOnlyList<string> FreeTierModuleCodes = [ProfilCandidatModule.Manifest.Code];

    public async Task<int> CreateCandidateAsync(string userId, CoreDbContext coreDb, CancellationToken cancellationToken = default)
    {
        var candidateProfileId = await candidateProfileService.GetOrCreateProfileIdAsync(userId, cancellationToken);

        var existing = await coreDb.CandidateSubscriptions.FirstOrDefaultAsync(s => s.CandidateProfileId == candidateProfileId, cancellationToken);
        if (existing is null)
        {
            coreDb.CandidateSubscriptions.Add(new CandidateSubscription
            {
                CandidateProfileId = candidateProfileId,
                PlanCode = PlanCodes.Standard,
                Status = SubscriptionStatus.Essai,
            });
            await coreDb.SaveChangesAsync(cancellationToken);
        }

        await ActivateModulesInOrderAsync(candidateProfileId, FreeTierModuleCodes, coreDb, cancellationToken);
        return candidateProfileId;
    }

    public async Task ActivateGestionDuTempsAsync(int candidateProfileId, CoreDbContext coreDb, CancellationToken cancellationToken = default)
    {
        _ = await coreDb.CandidateSubscriptions.FirstOrDefaultAsync(s => s.CandidateProfileId == candidateProfileId, cancellationToken)
            ?? throw new InvalidOperationException($"Aucun abonnement pour le candidat {candidateProfileId}.");

        await ActivateModulesInOrderAsync(candidateProfileId, [GestionDuTempsModule.Manifest.Code], coreDb, cancellationToken);
    }

    private async Task ActivateModulesInOrderAsync(int candidateProfileId, IReadOnlyList<string> moduleCodes, CoreDbContext coreDb, CancellationToken cancellationToken)
    {
        foreach (var moduleCode in moduleCodes)
        {
            if (await moduleRegistry.IsActiveForCandidateAsync(candidateProfileId, moduleCode, coreDb, cancellationToken))
                continue;

            var activeCodes = await moduleRegistry.GetActiveModuleCodesForCandidateAsync(candidateProfileId, coreDb, cancellationToken);
            var manifest = moduleRegistry.Find(moduleCode) ?? throw new InvalidOperationException($"Module inconnu : {moduleCode}");
            if (moduleRegistry.CanActivate(moduleCode, activeCodes, out _) || manifest.RequiredModuleCodes.Count == 0)
                await moduleRegistry.ActivateForCandidateAsync(candidateProfileId, moduleCode, coreDb, cancellationToken);
        }
    }
}
