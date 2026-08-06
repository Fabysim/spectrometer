using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Modules;
using Spectrometre.Modules.ProfilCandidat.Services;
using ProfilCandidatModule = Spectrometre.Modules.ProfilCandidat.ServiceCollectionExtensions;
using GestionDuTempsModule = Spectrometre.Modules.GestionDuTemps.ServiceCollectionExtensions;

namespace Spectrometre.Host.Onboarding;

/// <summary>
/// Équivalent candidat de <see cref="CompanyOnboardingService"/> — même structure (créer le sujet, ouvrir
/// un abonnement d'essai, activer le module gratuit), mais côté candidat il n'existait AUCUN chemin de
/// production créant une <see cref="CandidateSubscription"/> avant ce cycle (seulement des tests) : sans
/// cette classe, un candidat resterait bloqué à vie sur aucun module payant (échec fermé, voir
/// <c>ModuleRegistry.IsActiveAsync</c>/<c>IsActiveForCandidateAsync</c>).
/// </summary>
public sealed class CandidateOnboardingService(
    ICandidateProfileService candidateProfileService,
    IModuleRegistry moduleRegistry)
{
    /// <summary>Seul module activé automatiquement à l'inscription candidat — gratuit, inclus dans <see cref="PlanCodes.Standard"/>.</summary>
    private static readonly IReadOnlyList<string> FreeTierModuleCodes = [ProfilCandidatModule.Manifest.Code];

    public async Task<int> CreateCandidateAsync(string userId, CoreDbContext coreDb, CancellationToken cancellationToken = default)
    {
        var candidateProfileId = await candidateProfileService.GetOrCreateProfileIdAsync(userId, cancellationToken);

        // Un candidat n'a par défaut AUCUN abonnement (voir CandidateSubscription) — sans cette ligne,
        // aucun module ne serait effectivement actif pour lui, quel que soit l'état de ModuleActivation.
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

    /// <summary>Active Gestion du temps pour ce candidat (écran Ajouter un module) — nécessite de faire passer le plan à <see cref="PlanCodes.StandardPlusTemps"/> (le seul qui l'inclut) avant d'activer.</summary>
    public async Task ActivateGestionDuTempsAsync(int candidateProfileId, CoreDbContext coreDb, CancellationToken cancellationToken = default)
    {
        var subscription = await coreDb.CandidateSubscriptions.FirstOrDefaultAsync(s => s.CandidateProfileId == candidateProfileId, cancellationToken)
            ?? throw new InvalidOperationException($"Aucun abonnement pour le candidat {candidateProfileId}.");

        if (subscription.PlanCode != PlanCodes.StandardPlusTemps)
        {
            subscription.PlanCode = PlanCodes.StandardPlusTemps;
            await coreDb.SaveChangesAsync(cancellationToken);
        }

        await ActivateModulesInOrderAsync(candidateProfileId, [GestionDuTempsModule.Manifest.Code], coreDb, cancellationToken);
    }

    /// <summary>Même boucle d'activation que <see cref="CompanyOnboardingService"/>, côté candidat — voir sa remarque.</summary>
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
