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
/// l'enforcement par statut d'abonnement (Essai/Active vs Suspendue/Résiliée) — un module effectif exige
/// activation ET abonnement en cours, sans gating par PlanCode.
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

        // Désactivation : repasser IsActive à false doit redevenir inactif malgré un abo Active.
        var activation = await coreDb.ModuleActivations.FirstAsync(a =>
            a.SubjectType == ModuleActivationSubjectType.Candidate && a.SubjectId == candidateProfileId && a.ModuleCode == GestionDuTemps);
        activation.IsActive = false;
        await coreDb.SaveChangesAsync();

        Assert.False(await moduleRegistry.IsActiveForCandidateAsync(candidateProfileId, GestionDuTemps, coreDb));
    }

    [Fact]
    public async Task ActivationEntreprise_AbonnementSuspendue_CoupeIsActive_MemeSiModuleActive()
    {
        var suffix = Guid.NewGuid();
        var company = await fixture.CreateCompanyAsync($"Entreprise Suspendue {suffix}", $"suspendue-owner-{suffix}");

        using var scope = fixture.Services.CreateScope();
        var moduleRegistry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
        var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        await moduleRegistry.ActivateForCompanyAsync(company.Id, GestionDuTemps, coreDb);
        Assert.True(await moduleRegistry.IsActiveAsync(company.Id, GestionDuTemps, coreDb));
        Assert.True(await moduleRegistry.IsActiveAsync(company.Id, "Compatibilite", coreDb));

        var subscription = await coreDb.TenantSubscriptions.FirstAsync(s => s.CompanyId == company.Id);
        subscription.Status = SubscriptionStatus.Suspendue;
        await coreDb.SaveChangesAsync();

        Assert.False(await moduleRegistry.IsActiveAsync(company.Id, GestionDuTemps, coreDb));
        Assert.False(await moduleRegistry.IsActiveAsync(company.Id, "Compatibilite", coreDb));

        // L'indicateur d'activation reste vrai — seule la vérif effective coupe l'accès.
        var codesActives = await moduleRegistry.GetActiveModuleCodesAsync(company.Id, coreDb);
        Assert.Contains(GestionDuTemps, codesActives);

        subscription.Status = SubscriptionStatus.Active;
        await coreDb.SaveChangesAsync();
        Assert.True(await moduleRegistry.IsActiveAsync(company.Id, GestionDuTemps, coreDb));
    }

    [Fact]
    public async Task GestionDuTempsAccessService_RefuseSansAbonnement_AutoriseAvecAbonnementCandidatOuEntreprise()
    {
        var accessService = fixture.Services.GetRequiredService<IGestionDuTempsAccessService>();

        // Cas refusé : ni abonnement candidat, ni entreprise avec le module actif.
        var userSansAcces = $"gdt-access-refuse-{Guid.NewGuid()}";
        Assert.False(await accessService.HasAccessAsync(userSansAcces));

        // Cas autorisé (candidat) : abonnement Active + activation explicite.
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
                PlanCode = PlanCodes.Standard,
                Status = SubscriptionStatus.Active,
            });
            await coreDb.SaveChangesAsync();
            await moduleRegistry.ActivateForCandidateAsync(candidateProfileId, GestionDuTemps, coreDb);
        }
        Assert.True(await accessService.HasAccessAsync(userCandidat));
        Assert.True(await accessService.HasCandidateAccessAsync(userCandidat));

        // Cas autorisé (entreprise) : activation GDT + abo Active (PlanCode n'entre plus en jeu).
        var suffix = Guid.NewGuid();
        var userManager = $"gdt-access-manager-{suffix}";
        var company = await fixture.CreateCompanyAsync($"Entreprise Access GDT {suffix}", userManager);
        using (var scope = fixture.Services.CreateScope())
        {
            var moduleRegistry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
            var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            await moduleRegistry.ActivateForCompanyAsync(company.Id, GestionDuTemps, coreDb);
        }
        Assert.True(await accessService.HasAccessAsync(userManager));

        // « Mon coach » (Coaching côté personne suivie) dérive de Gestion du temps mais UNIQUEMENT côté
        // Candidat — un gestionnaire d'entreprise avec Gestion du temps actif ne doit PAS avoir accès à
        // cette dérivation, même si HasAccessAsync (le module lui-même) le lui accorde bien.
        Assert.False(await accessService.HasCandidateAccessAsync(userManager));
    }

    [Fact]
    public async Task TenantSubscriptionBackfill_RestaureLAccesDUneEntrepriseSansAbonnement_SansPerte()
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
        // Contrairement à fixture.CreateCompanyAsync (qui s'auto-enregistre), CreateCompanyAsync appelé
        // directement ci-dessus provisionne un VRAI schéma Postgres dédié sans que rien ne le sache — sans cet
        // enregistrement manuel, ServiceFixture.DisposeAsync ne le nettoierait jamais (voir sa remarque).
        fixture.TrackCompanyForCleanup(company);

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
