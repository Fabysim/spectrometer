using Microsoft.EntityFrameworkCore;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Modules;
using Spectrometre.Modules.ProfilCoach.Services;
using ProfilCoachModule = Spectrometre.Modules.ProfilCoach.ServiceCollectionExtensions;
using GestionDuTempsModule = Spectrometre.Modules.GestionDuTemps.ServiceCollectionExtensions;

namespace Spectrometre.Host.Onboarding;

/// <summary>
/// Équivalent coach de <see cref="CandidateOnboardingService"/> — même structure (créer le sujet, ouvrir un
/// abonnement, activer le module gratuit). Appelée à la fois depuis l'inscription (nouveau compte, profil
/// Coach choisi directement) et depuis l'acceptation d'une invitation Coaching par un compte EXISTANT
/// n'ayant pas encore de profil Coach (voir <c>InvitationAcceptanceService</c>) — <see cref="CreateCoachAsync"/>
/// est idempotente (ne crée rien en double) précisément pour ce second appelant : un utilisateur peut
/// cumuler plusieurs profils (ex. déjà Candidat, qui accepte ensuite une invitation à devenir Coach).
/// </summary>
public sealed class CoachOnboardingService(
    ICoachProfileService coachProfileService,
    IModuleRegistry moduleRegistry)
{
    /// <summary>Seul module activé automatiquement à l'inscription coach — gratuit, inclus dans <see cref="PlanCodes.Coach"/>.</summary>
    private static readonly IReadOnlyList<string> FreeTierModuleCodes = [ProfilCoachModule.Manifest.Code];

    public async Task<int> CreateCoachAsync(string userId, CoreDbContext coreDb, CancellationToken cancellationToken = default)
    {
        var coachProfileId = await coachProfileService.GetOrCreateProfileIdAsync(userId, cancellationToken);

        var existing = await coreDb.CoachSubscriptions.FirstOrDefaultAsync(s => s.CoachProfileId == coachProfileId, cancellationToken);
        if (existing is null)
        {
            coreDb.CoachSubscriptions.Add(new CoachSubscription
            {
                CoachProfileId = coachProfileId,
                PlanCode = PlanCodes.Coach,
                Status = SubscriptionStatus.Active,
            });
            await coreDb.SaveChangesAsync(cancellationToken);
        }

        await ActivateModulesInOrderAsync(coachProfileId, FreeTierModuleCodes, coreDb, cancellationToken);

        return coachProfileId;
    }

    /// <summary>
    /// Active Gestion du temps pour ce coach (écran Ajouter un module) — nécessite de faire passer le plan à
    /// <see cref="PlanCodes.CoachPlusTemps"/> (le seul plan coach qui l'inclut) avant d'activer.
    /// Même pattern que <see cref="CandidateOnboardingService.ActivateGestionDuTempsAsync"/>.
    /// </summary>
    public async Task ActivateGestionDuTempsAsync(int coachProfileId, CoreDbContext coreDb, CancellationToken cancellationToken = default)
    {
        var subscription = await coreDb.CoachSubscriptions.FirstOrDefaultAsync(s => s.CoachProfileId == coachProfileId, cancellationToken)
            ?? throw new InvalidOperationException($"Aucun abonnement pour le coach {coachProfileId}.");

        if (subscription.PlanCode != PlanCodes.CoachPlusTemps)
        {
            subscription.PlanCode = PlanCodes.CoachPlusTemps;
            await coreDb.SaveChangesAsync(cancellationToken);
        }

        await ActivateModulesInOrderAsync(coachProfileId, [GestionDuTempsModule.Manifest.Code], coreDb, cancellationToken);
    }

    /// <summary>Même boucle d'activation que <see cref="CandidateOnboardingService"/>, côté coach.</summary>
    private async Task ActivateModulesInOrderAsync(int coachProfileId, IReadOnlyList<string> moduleCodes, CoreDbContext coreDb, CancellationToken cancellationToken)
    {
        foreach (var moduleCode in moduleCodes)
        {
            if (await moduleRegistry.IsActiveForCoachAsync(coachProfileId, moduleCode, coreDb, cancellationToken))
                continue;

            var activeCodes = await moduleRegistry.GetActiveModuleCodesForCoachAsync(coachProfileId, coreDb, cancellationToken);
            var manifest = moduleRegistry.Find(moduleCode) ?? throw new InvalidOperationException($"Module inconnu : {moduleCode}");
            if (moduleRegistry.CanActivate(moduleCode, activeCodes, out _) || manifest.RequiredModuleCodes.Count == 0)
                await moduleRegistry.ActivateForCoachAsync(coachProfileId, moduleCode, coreDb, cancellationToken);
        }
    }
}
