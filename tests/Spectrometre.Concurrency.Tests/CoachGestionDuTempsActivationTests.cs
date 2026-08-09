using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Modules;
using Spectrometre.Modules.GestionDuTemps.Services;
using Spectrometre.Modules.ProfilCoach.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

[Collection("Base de données partagée")]
public sealed class CoachGestionDuTempsActivationTests(ServiceFixture fixture)
{
    private IServiceScope NewScope() => fixture.Services.CreateScope();

    [Fact]
    public async Task ActivateGestionDuTemps_PersisteEtIsActiveForCoachRefleteLeChangement()
    {
        var userId = $"coach-gdt-{Guid.NewGuid()}";
        int coachProfileId;

        using (var scope = NewScope())
        {
            var profiles = scope.ServiceProvider.GetRequiredService<ICoachProfileService>();
            coachProfileId = await profiles.GetOrCreateProfileIdAsync(userId);
            await profiles.SaveProfilAsync(userId, "Coach GDT", "Bio", "temps", visibleDansAnnuaire: false);

            var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            if (!await coreDb.CoachSubscriptions.AnyAsync(s => s.CoachProfileId == coachProfileId))
            {
                coreDb.CoachSubscriptions.Add(new CoachSubscription
                {
                    CoachProfileId = coachProfileId,
                    PlanCode = PlanCodes.Coach,
                    Status = SubscriptionStatus.Active,
                });
                await coreDb.SaveChangesAsync();
            }

            var registry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
            if (!await registry.IsActiveForCoachAsync(coachProfileId, "ProfilCoach", coreDb))
                await registry.ActivateForCoachAsync(coachProfileId, "ProfilCoach", coreDb);

            Assert.False(await registry.IsActiveForCoachAsync(coachProfileId, "GestionDuTemps", coreDb));
        }

        // Même logique que CoachOnboardingService.ActivateGestionDuTempsAsync (Host) —
        // testée ici sans référence Host : ActivateForCoach uniquement (plus d'upgrade PlanCode).
        using (var scope = NewScope())
        {
            var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var registry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();

            await registry.ActivateForCoachAsync(coachProfileId, "GestionDuTemps", coreDb);

            Assert.True(await registry.IsActiveForCoachAsync(coachProfileId, "GestionDuTemps", coreDb));
        }

        using (var scope = NewScope())
        {
            var access = scope.ServiceProvider.GetRequiredService<IGestionDuTempsAccessService>();
            Assert.True(await access.HasAccessAsync(userId));
        }
    }
}
