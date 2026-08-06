using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Billing;
using Spectrometre.Core.Data;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.GestionDuTemps.Services;
using Spectrometre.Modules.ProfilCandidat.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Vérifie la généralisation du registre d'activation (voir <see cref="ModuleActivationSubjectType"/>) et
/// le gating par plan tarifaire (voir <see cref="PlanModuleEntitlement"/>) introduits pour que Gestion du
/// temps (et un futur profil indépendant) puissent être vendus sans être couplés à la notion d'entreprise.
/// </summary>
[Collection("Base de données partagée")]
public sealed class ModuleActivationTests(ServiceFixture fixture)
{
    private const string GestionDuTemps = "GestionDuTemps";

    [Fact]
    public async Task ActivationCandidat_SansAbonnement_NEstJamaisEffective_MemeSiExplicitementActivee()
    {
        using var scope = fixture.Services.CreateScope();
        var moduleRegistry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
        var candidateService = scope.ServiceProvider.GetRequiredService<ICandidateProfileService>();
        var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var candidateProfileId = await candidateService.GetOrCreateProfileIdAsync($"gdt-candidat-sans-plan-{Guid.NewGuid()}");

        // Activé explicitement — mais aucun abonnement candidat n'existe encore.
        await moduleRegistry.ActivateForCandidateAsync(candidateProfileId, GestionDuTemps, coreDb);

        // L'indicateur d'activation, lui, est bien vrai (voir GetActiveModuleCodesForCandidateAsync,
        // non filtré par plan) — seule la vérification EFFECTIVE (IsActiveForCandidateAsync) doit refuser.
        var codesActives = await moduleRegistry.GetActiveModuleCodesForCandidateAsync(candidateProfileId, coreDb);
        Assert.Contains(GestionDuTemps, codesActives);

        Assert.False(await moduleRegistry.IsActiveForCandidateAsync(candidateProfileId, GestionDuTemps, coreDb));
    }

    [Fact]
    public async Task ActivationCandidat_AvecAbonnementIncluantLeModule_EstEffective()
    {
        using var scope = fixture.Services.CreateScope();
        var moduleRegistry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
        var candidateService = scope.ServiceProvider.GetRequiredService<ICandidateProfileService>();
        var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var candidateProfileId = await candidateService.GetOrCreateProfileIdAsync($"gdt-candidat-avec-plan-{Guid.NewGuid()}");

        coreDb.CandidateSubscriptions.Add(new Spectrometre.Core.Billing.CandidateSubscription
        {
            CandidateProfileId = candidateProfileId,
            PlanCode = PlanCodes.StandardPlusTemps,
            Status = SubscriptionStatus.Active,
        });
        await coreDb.SaveChangesAsync();

        Assert.False(await moduleRegistry.IsActiveForCandidateAsync(candidateProfileId, GestionDuTemps, coreDb));

        await moduleRegistry.ActivateForCandidateAsync(candidateProfileId, GestionDuTemps, coreDb);

        Assert.True(await moduleRegistry.IsActiveForCandidateAsync(candidateProfileId, GestionDuTemps, coreDb));

        // Désactivation : repasser IsActive à false doit redevenir inactif malgré un plan qui l'inclut.
        var activation = await coreDb.ModuleActivations.FirstAsync(a =>
            a.SubjectType == ModuleActivationSubjectType.Candidate && a.SubjectId == candidateProfileId && a.ModuleCode == GestionDuTemps);
        activation.IsActive = false;
        await coreDb.SaveChangesAsync();

        Assert.False(await moduleRegistry.IsActiveForCandidateAsync(candidateProfileId, GestionDuTemps, coreDb));
    }

    [Fact]
    public async Task ActivationEntreprise_AvecPlanStandard_NInclutPasGestionDuTemps_MemeSiActivee()
    {
        var suffix = Guid.NewGuid();
        var company = await fixture.CreateCompanyAsync($"Entreprise Gating Standard {suffix}", $"gating-owner-{suffix}");

        using var scope = fixture.Services.CreateScope();
        var moduleRegistry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
        var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        // CreateCompanyAsync (voir ServiceFixture) assigne déjà le plan Standard — activer explicitement
        // GestionDuTemps ne doit RIEN changer : Standard ne l'inclut pas.
        await moduleRegistry.ActivateForCompanyAsync(company.Id, GestionDuTemps, coreDb);

        Assert.False(await moduleRegistry.IsActiveAsync(company.Id, GestionDuTemps, coreDb));

        // Cas autorisé : passage au plan StandardPlusTemps pour CETTE entreprise — devient effectif
        // immédiatement, sans ré-activation (voir la règle documentée sur ModuleRegistry.IsActiveAsync).
        var subscription = await coreDb.TenantSubscriptions.FirstAsync(s => s.CompanyId == company.Id);
        subscription.PlanCode = PlanCodes.StandardPlusTemps;
        await coreDb.SaveChangesAsync();

        Assert.True(await moduleRegistry.IsActiveAsync(company.Id, GestionDuTemps, coreDb));

        // Les modules Matching Emploi déjà actifs pour cette entreprise ne sont pas affectés par ce
        // changement de plan (StandardPlusTemps est un sur-ensemble de Standard).
        Assert.True(await moduleRegistry.IsActiveAsync(company.Id, "Compatibilite", coreDb));
    }

    [Fact]
    public async Task GestionDuTempsAccessService_RefuseSansAbonnement_AutoriseAvecAbonnementCandidatOuEntreprise()
    {
        var accessService = fixture.Services.GetRequiredService<IGestionDuTempsAccessService>();

        // Cas refusé : ni abonnement candidat, ni entreprise abonnée à un plan incluant le module.
        var userSansAcces = $"gdt-access-refuse-{Guid.NewGuid()}";
        Assert.False(await accessService.HasAccessAsync(userSansAcces));

        // Cas autorisé (candidat) : abonnement StandardPlusTemps + activation explicite pour ce candidat.
        var userCandidat = $"gdt-access-candidat-{Guid.NewGuid()}";
        using (var scope = fixture.Services.CreateScope())
        {
            var candidateService = scope.ServiceProvider.GetRequiredService<ICandidateProfileService>();
            var moduleRegistry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
            var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

            var candidateProfileId = await candidateService.GetOrCreateProfileIdAsync(userCandidat);
            coreDb.CandidateSubscriptions.Add(new Spectrometre.Core.Billing.CandidateSubscription
            {
                CandidateProfileId = candidateProfileId,
                PlanCode = PlanCodes.StandardPlusTemps,
                Status = SubscriptionStatus.Active,
            });
            await coreDb.SaveChangesAsync();
            await moduleRegistry.ActivateForCandidateAsync(candidateProfileId, GestionDuTemps, coreDb);
        }
        Assert.True(await accessService.HasAccessAsync(userCandidat));

        // Cas autorisé (entreprise) : le gestionnaire d'une entreprise passée au plan StandardPlusTemps et
        // activée obtient l'accès, sans le moindre abonnement candidat personnel.
        var suffix = Guid.NewGuid();
        var userManager = $"gdt-access-manager-{suffix}";
        var company = await fixture.CreateCompanyAsync($"Entreprise Access GDT {suffix}", userManager);
        using (var scope = fixture.Services.CreateScope())
        {
            var moduleRegistry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
            var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var subscription = await coreDb.TenantSubscriptions.FirstAsync(s => s.CompanyId == company.Id);
            subscription.PlanCode = PlanCodes.StandardPlusTemps;
            await coreDb.SaveChangesAsync();
            await moduleRegistry.ActivateForCompanyAsync(company.Id, GestionDuTemps, coreDb);
        }
        Assert.True(await accessService.HasAccessAsync(userManager));
    }

    [Fact]
    public async Task TenantSubscriptionBackfill_RestaureLAccesDUneEntrepriseCreeeAvantLeGatingParPlan_SansPerte()
    {
        using var scope = fixture.Services.CreateScope();
        var provisioning = scope.ServiceProvider.GetRequiredService<ICompanyProvisioningService>();
        var moduleRegistry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
        var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var suffix = Guid.NewGuid();
        // Appelle ICompanyProvisioningService.CreateCompanyAsync DIRECTEMENT (pas fixture.CreateCompanyAsync,
        // qui crée désormais un abonnement) — simule fidèlement une entreprise créée AVANT ce cycle, jamais
        // passée par la création d'abonnement.
        var company = await provisioning.CreateCompanyAsync($"Entreprise Legacy Backfill {suffix}", $"legacy-owner-{suffix}", coreDb);

        Assert.False(await coreDb.TenantSubscriptions.AnyAsync(s => s.CompanyId == company.Id));

        // Deux modules "déjà activés" avant l'introduction du gating (indicateur seul, comme avant ce cycle).
        await moduleRegistry.ActivateForCompanyAsync(company.Id, "ProfilCandidat", coreDb);
        await moduleRegistry.ActivateForCompanyAsync(company.Id, "ProfilEntreprise", coreDb);
        var codesActivesAvant = await moduleRegistry.GetActiveModuleCodesAsync(company.Id, coreDb);

        // Sans abonnement, échec fermé : plus aucun module effectivement actif, malgré l'indicateur à vrai.
        Assert.False(await moduleRegistry.IsActiveAsync(company.Id, "ProfilCandidat", coreDb));
        Assert.False(await moduleRegistry.IsActiveAsync(company.Id, "ProfilEntreprise", coreDb));

        await TenantSubscriptionBackfill.RunAsync(coreDb);

        var subscription = await coreDb.TenantSubscriptions.SingleAsync(s => s.CompanyId == company.Id);
        Assert.Equal(PlanCodes.Standard, subscription.PlanCode);

        // Exactement les mêmes modules actifs qu'avant le backfill (aucune perte, aucun ajout).
        var codesActivesApres = await moduleRegistry.GetActiveModuleCodesAsync(company.Id, coreDb);
        Assert.Equal(codesActivesAvant.OrderBy(c => c), codesActivesApres.OrderBy(c => c));

        Assert.True(await moduleRegistry.IsActiveAsync(company.Id, "ProfilCandidat", coreDb));
        Assert.True(await moduleRegistry.IsActiveAsync(company.Id, "ProfilEntreprise", coreDb));

        // Idempotent : ré-exécuter le backfill ne touche pas une entreprise déjà abonnée (pas de doublon).
        await TenantSubscriptionBackfill.RunAsync(coreDb);
        Assert.Equal(1, await coreDb.TenantSubscriptions.CountAsync(s => s.CompanyId == company.Id));
    }
}
