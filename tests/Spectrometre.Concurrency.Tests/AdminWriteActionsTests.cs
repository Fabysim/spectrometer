using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Modules;
using Spectrometre.Modules.Admin.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Actions d'écriture de la zone Admin au-delà de la promotion/rétrogradation : activer/désactiver un
/// module pour un client, éditer un plan, traçabilité. Réutilisent exclusivement <see cref="IModuleRegistry"/>
/// (voir <c>IModuleRegistry.SetActiveAsync</c>, ajouté pour ce cycle — jamais de logique d'activation
/// parallèle) et <c>PlanModuleEntitlement</c> — mêmes tables que le reste de l'application.
/// </summary>
[Collection("Base de données partagée")]
public sealed class AdminWriteActionsTests(ServiceFixture fixture)
{
    private static ClaimsPrincipal AdminCaller() =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.Role, PlatformRoles.Admin)], "Test"));

    private static ClaimsPrincipal NonAdminCaller() => new(new ClaimsIdentity([], "Test"));

    [Fact]
    public async Task ActiverPuisDesactiverUnModule_PourUneEntreprise_EstEffectifImmediatement()
    {
        var suffix = Guid.NewGuid();
        var company = await fixture.CreateCompanyAsync($"Entreprise Admin Write {suffix}", $"admin-write-owner-{suffix}");

        using var scope = fixture.Services.CreateScope();
        var adminService = scope.ServiceProvider.GetRequiredService<IAdminService>();
        var moduleRegistry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
        var coreDb = scope.ServiceProvider.GetRequiredService<Spectrometre.Core.Data.CoreDbContext>();
        var caller = AdminCaller();

        // GestionDuTemps n'est jamais activé par défaut à la création d'une entreprise (voir CreateCompanyAsync).
        Assert.False(await moduleRegistry.IsActiveAsync(company.Id, "Vivier", coreDb));

        await adminService.DefinirActivationModuleAsync(caller, ModuleActivationSubjectType.Company, company.Id, "Vivier", true);
        Assert.True(await moduleRegistry.IsActiveAsync(company.Id, "Vivier", coreDb));

        await adminService.DefinirActivationModuleAsync(caller, ModuleActivationSubjectType.Company, company.Id, "Vivier", false);
        Assert.False(await moduleRegistry.IsActiveAsync(company.Id, "Vivier", coreDb));

        // Idempotent : répéter la désactivation ne lève pas.
        await adminService.DefinirActivationModuleAsync(caller, ModuleActivationSubjectType.Company, company.Id, "Vivier", false);
    }

    [Fact]
    public async Task AjouterPuisRetirerUnModuleDUnPlan_EstEffectifPourLesSujetsSurCePlan()
    {
        var suffix = Guid.NewGuid();
        var planCode = $"plan-test-{suffix}";

        using var scope = fixture.Services.CreateScope();
        var adminService = scope.ServiceProvider.GetRequiredService<IAdminService>();
        var caller = AdminCaller();

        await adminService.AjouterModuleAuPlanAsync(caller, planCode, "Analytics");
        var plans = await adminService.GetPlansAsync(caller);
        var plan = Assert.Single(plans, p => p.PlanCode == planCode);
        Assert.Contains("Analytics", plan.ModulesInclus);

        // Idempotent à l'ajout.
        await adminService.AjouterModuleAuPlanAsync(caller, planCode, "Analytics");
        plans = await adminService.GetPlansAsync(caller);
        plan = Assert.Single(plans, p => p.PlanCode == planCode);
        Assert.Single(plan.ModulesInclus, m => m == "Analytics");

        await adminService.RetirerModuleDuPlanAsync(caller, planCode, "Analytics");
        plans = await adminService.GetPlansAsync(caller);
        Assert.DoesNotContain(plans, p => p.PlanCode == planCode);
    }

    [Fact]
    public async Task ChaqueActionDEcriture_EstHistorisee()
    {
        var suffix = Guid.NewGuid();
        var company = await fixture.CreateCompanyAsync($"Entreprise Admin Historique {suffix}", $"admin-hist-owner-{suffix}");
        var planCode = $"plan-histo-{suffix}";

        using var scope = fixture.Services.CreateScope();
        var adminService = scope.ServiceProvider.GetRequiredService<IAdminService>();
        var caller = AdminCaller();

        await adminService.DefinirActivationModuleAsync(caller, ModuleActivationSubjectType.Company, company.Id, "Analytics", true);
        await adminService.AjouterModuleAuPlanAsync(caller, planCode, "Analytics");

        var historique = await adminService.GetHistoriqueAsync(caller, take: 200);
        Assert.Contains(historique, h => h.Action == "ActivationModule" && h.Cible.Contains($"Company #{company.Id}") && h.Cible.Contains("Analytics"));
        Assert.Contains(historique, h => h.Action == "AjoutModuleAuPlan" && h.Cible.Contains(planCode) && h.Cible.Contains("Analytics"));
    }

    [Fact]
    public async Task AppelantNonAdmin_EstRefuseSurToutesLesNouvellesActions()
    {
        using var scope = fixture.Services.CreateScope();
        var adminService = scope.ServiceProvider.GetRequiredService<IAdminService>();
        var caller = NonAdminCaller();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            adminService.DefinirActivationModuleAsync(caller, ModuleActivationSubjectType.Company, 1, "Analytics", true));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            adminService.GetModulesPourSujetAsync(caller, ModuleActivationSubjectType.Company, 1));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => adminService.GetPlansAsync(caller));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            adminService.AjouterModuleAuPlanAsync(caller, "Standard", "Analytics"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            adminService.RetirerModuleDuPlanAsync(caller, "Standard", "Analytics"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => adminService.GetHistoriqueAsync(caller));
    }
}
