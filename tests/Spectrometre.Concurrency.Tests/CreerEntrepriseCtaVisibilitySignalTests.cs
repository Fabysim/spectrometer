using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Modules;
using Spectrometre.Modules.ProfilCandidat.Services;
using Spectrometre.Modules.ProfilCoach.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Signal utilisé by <c>Dashboard.razor</c>/<c>MainLayout.razor</c> to hide « Créer une entreprise »
/// for users registered as Candidate or Coach. No Blazor render-test framework in this project —
/// full UI coverage is done in the browser. Here we lock the service-level signal
/// (<see cref="IModuleRegistry.IsActiveForCandidateAsync"/> / <see cref="IModuleRegistry.IsActiveForCoachAsync"/>),
/// never via <c>GetActiveModuleCodesAsync</c>.
/// </summary>
[Collection("Base de données partagée")]
public sealed class CreerEntrepriseCtaVisibilitySignalTests(ServiceFixture fixture)
{
    private static async Task<bool> EstCandidatOuCoachAsync(
        IModuleRegistry moduleRegistry,
        int candidateProfileId,
        int coachProfileId,
        CoreDbContext coreDb) =>
        await moduleRegistry.IsActiveForCandidateAsync(candidateProfileId, "ProfilCandidat", coreDb)
        || await moduleRegistry.IsActiveForCoachAsync(coachProfileId, "ProfilCoach", coreDb);

    [Fact]
    public async Task CoquilleVideSansActivation_NEstPasCandidatNiCoachEffectif()
    {
        using var scope = fixture.Services.CreateScope();
        var moduleRegistry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
        var candidateService = scope.ServiceProvider.GetRequiredService<ICandidateProfileService>();
        var coachService = scope.ServiceProvider.GetRequiredService<ICoachProfileService>();
        var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var suffix = Guid.NewGuid();
        var candidateProfileId = await candidateService.GetOrCreateProfileIdAsync($"cta-shell-candidat-{suffix}");
        var coachProfileId = await coachService.GetOrCreateProfileIdAsync($"cta-shell-coach-{suffix}");

        // Même cas que GetOrCreate* sur le dashboard sans onboarding : coquille vide, modules inactifs.
        Assert.False(await EstCandidatOuCoachAsync(moduleRegistry, candidateProfileId, coachProfileId, coreDb));
    }

    [Fact]
    public async Task CandidatAvecProfilCandidatActif_LeSignalEstVrai()
    {
        using var scope = fixture.Services.CreateScope();
        var moduleRegistry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
        var candidateService = scope.ServiceProvider.GetRequiredService<ICandidateProfileService>();
        var coachService = scope.ServiceProvider.GetRequiredService<ICoachProfileService>();
        var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var suffix = Guid.NewGuid();
        var candidateProfileId = await candidateService.GetOrCreateProfileIdAsync($"cta-candidat-actif-{suffix}");
        var coachProfileId = await coachService.GetOrCreateProfileIdAsync($"cta-candidat-coach-shell-{suffix}");

        coreDb.CandidateSubscriptions.Add(new CandidateSubscription
        {
            CandidateProfileId = candidateProfileId,
            PlanCode = PlanCodes.Standard,
            Status = SubscriptionStatus.Active,
        });
        await coreDb.SaveChangesAsync();
        await moduleRegistry.ActivateForCandidateAsync(candidateProfileId, "ProfilCandidat", coreDb);

        Assert.True(await moduleRegistry.IsActiveForCandidateAsync(candidateProfileId, "ProfilCandidat", coreDb));
        Assert.True(await EstCandidatOuCoachAsync(moduleRegistry, candidateProfileId, coachProfileId, coreDb));
    }

    [Fact]
    public async Task CoachAvecProfilCoachActif_LeSignalEstVrai()
    {
        using var scope = fixture.Services.CreateScope();
        var moduleRegistry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
        var candidateService = scope.ServiceProvider.GetRequiredService<ICandidateProfileService>();
        var coachService = scope.ServiceProvider.GetRequiredService<ICoachProfileService>();
        var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var suffix = Guid.NewGuid();
        var candidateProfileId = await candidateService.GetOrCreateProfileIdAsync($"cta-coach-candidat-shell-{suffix}");
        var coachProfileId = await coachService.GetOrCreateProfileIdAsync($"cta-coach-actif-{suffix}");

        coreDb.CoachSubscriptions.Add(new CoachSubscription
        {
            CoachProfileId = coachProfileId,
            PlanCode = PlanCodes.Coach,
            Status = SubscriptionStatus.Active,
        });
        await coreDb.SaveChangesAsync();
        await moduleRegistry.ActivateForCoachAsync(coachProfileId, "ProfilCoach", coreDb);

        Assert.True(await moduleRegistry.IsActiveForCoachAsync(coachProfileId, "ProfilCoach", coreDb));
        Assert.True(await EstCandidatOuCoachAsync(moduleRegistry, candidateProfileId, coachProfileId, coreDb));
    }
}
