using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Modules;
using Spectrometre.Modules.Missions.Services;
using MissionsModule = Spectrometre.Modules.Missions.ServiceCollectionExtensions;

namespace Spectrometre.Host.Onboarding;

/// <summary>Onboarding particulier — mirror de <see cref="CoachOnboardingService"/>.</summary>
public sealed class ParticulierOnboardingService(
    IParticulierProfileService particulierProfileService,
    IModuleRegistry moduleRegistry)
{
    private static readonly IReadOnlyList<string> FreeTierModuleCodes = [MissionsModule.Manifest.Code];

    public async Task<int> CreateParticulierAsync(
        string userId,
        string nom,
        string prenoms,
        CoreDbContext coreDb,
        CancellationToken cancellationToken = default)
    {
        var particulierProfileId = await particulierProfileService.GetOrCreateProfileIdAsync(userId, nom, prenoms, cancellationToken);

        var existing = await coreDb.ParticulierSubscriptions.FirstOrDefaultAsync(
            s => s.ParticulierProfileId == particulierProfileId, cancellationToken);
        if (existing is null)
        {
            coreDb.ParticulierSubscriptions.Add(new ParticulierSubscription
            {
                ParticulierProfileId = particulierProfileId,
                PlanCode = PlanCodes.Particulier,
                Status = SubscriptionStatus.Active,
            });
            await coreDb.SaveChangesAsync(cancellationToken);
        }

        await ActivateModulesInOrderAsync(particulierProfileId, FreeTierModuleCodes, coreDb, cancellationToken);
        return particulierProfileId;
    }

    private async Task ActivateModulesInOrderAsync(
        int particulierProfileId,
        IReadOnlyList<string> moduleCodes,
        CoreDbContext coreDb,
        CancellationToken cancellationToken)
    {
        foreach (var moduleCode in moduleCodes)
        {
            if (await moduleRegistry.IsActiveForParticulierAsync(particulierProfileId, moduleCode, coreDb, cancellationToken))
                continue;

            var activeCodes = await moduleRegistry.GetActiveModuleCodesForParticulierAsync(particulierProfileId, coreDb, cancellationToken);
            var manifest = moduleRegistry.Find(moduleCode) ?? throw new InvalidOperationException($"Module inconnu : {moduleCode}");
            if (moduleRegistry.CanActivate(moduleCode, activeCodes, out _) || manifest.RequiredModuleCodes.Count == 0)
                await moduleRegistry.ActivateForParticulierAsync(particulierProfileId, moduleCode, coreDb, cancellationToken);
        }
    }
}
