using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Invitations;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.GestionDuTemps.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Multi-entreprise et managers (réutilisation du mécanisme d'invitation générique déjà construit pour
/// Coaching — voir <see cref="InvitationType.CompanyEmploye"/>). Couvre : émission/acceptation/révocation
/// d'une invitation manager, restriction au seul Propriétaire, sélection d'entreprise dans Gestion du temps
/// pour un utilisateur lié à plusieurs entreprises, et la frontière stricte de l'Étape 4 (le rattachement à
/// une entreprise ne donne accès au Gestion du temps personnel de PERSONNE d'autre).
/// </summary>
[Collection("Base de données partagée")]
public sealed class ManagerInvitationTests(ServiceFixture fixture)
{
    private IServiceScope NewScope() => fixture.Services.CreateScope();

    private async Task<ApplicationUser> CreateUserAsync(IServiceScope scope, string emailPrefix)
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = $"{emailPrefix}-{Guid.NewGuid()}@example.test";
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, "Str0ng!Passw0rd");
        Assert.True(result.Succeeded);
        fixture.TrackUserForCleanup(user.Id);
        return user;
    }

    [Fact]
    public async Task Proprietaire_InvitePuisManagerAccepte_PuisRevocation_CoupeLeRattachement()
    {
        var suffix = Guid.NewGuid();
        var ownerUserId = $"manager-cycle-owner-{suffix}";
        var company = await fixture.CreateCompanyAsync($"Entreprise Manager {suffix}", ownerUserId);

        using var scope = NewScope();
        var provisioning = scope.ServiceProvider.GetRequiredService<ICompanyProvisioningService>();
        var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var manager = await CreateUserAsync(scope, "manager-invite");

        // Étape 1 : émission — même mécanisme générique que Coaching (InvitationType.CompanyEmploye, ContextId = CompanyId).
        var invitation = await provisioning.InviterEmployeAsync(ownerUserId, company.Id, manager.Email!, coreDb);
        Assert.NotNull(invitation);
        Assert.Equal(InvitationType.CompanyEmploye, invitation!.Type);
        Assert.Equal(company.Id, invitation.ContextId);

        var invitationsEnCours = await provisioning.GetInvitationsEmployeEnCoursAsync(company.Id, coreDb);
        Assert.Contains(invitationsEnCours, i => i.Id == invitation.Id);

        // Étape 2 : acceptation (simulée au niveau service — la page InvitationAcceptancePage appelle exactement ceci).
        await provisioning.FinaliserEmployeDepuisInvitationAsync(invitation, manager.Id, coreDb);

        var managers = await provisioning.GetEmployesAsync(company.Id, coreDb);
        Assert.Contains(managers, l => l.UserId == manager.Id);

        var companiesDuManager = await provisioning.GetCompaniesForUserAsync(manager.Id, coreDb);
        Assert.Contains(companiesDuManager, c => c.Id == company.Id);

        // Révocation : le rattachement est effectivement coupé.
        var revoque = await provisioning.RevokeEmployeAsync(ownerUserId, company.Id, manager.Id, coreDb);
        Assert.True(revoque);

        var managersApresRevocation = await provisioning.GetEmployesAsync(company.Id, coreDb);
        Assert.DoesNotContain(managersApresRevocation, l => l.UserId == manager.Id);
    }

    [Fact]
    public async Task SeulLeProprietaire_PeutInviterUnManager()
    {
        var suffix = Guid.NewGuid();
        var ownerUserId = $"manager-cycle-owner-{suffix}";
        var company = await fixture.CreateCompanyAsync($"Entreprise Manager Restriction {suffix}", ownerUserId);

        using var scope = NewScope();
        var provisioning = scope.ServiceProvider.GetRequiredService<ICompanyProvisioningService>();
        var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        var manager = await CreateUserAsync(scope, "manager-non-proprietaire");
        var invitationInitiale = await provisioning.InviterEmployeAsync(ownerUserId, company.Id, manager.Email!, coreDb);
        await provisioning.FinaliserEmployeDepuisInvitationAsync(invitationInitiale!, manager.Id, coreDb);

        // Le manager fraîchement rattaché tente d'inviter quelqu'un d'autre — refusé.
        var invitationParManager = await provisioning.InviterEmployeAsync(manager.Id, company.Id, "quelqu-un@example.test", coreDb);
        Assert.Null(invitationParManager);

        // Un tiers totalement étranger à l'entreprise — refusé aussi.
        var tiersUserId = $"manager-cycle-tiers-{suffix}";
        var invitationParTiers = await provisioning.InviterEmployeAsync(tiersUserId, company.Id, "quelqu-un-2@example.test", coreDb);
        Assert.Null(invitationParTiers);
    }

    [Fact]
    public async Task UtilisateurLieAPlusieursEntreprises_PeutTaggerSesRituelsSurLUneOuLAutre()
    {
        var suffix = Guid.NewGuid();
        var userId = $"multi-entreprise-{suffix}";
        var companyA = await fixture.CreateCompanyAsync($"Entreprise Multi A {suffix}", userId);
        var companyB = await fixture.CreateCompanyAsync($"Entreprise Multi B {suffix}", userId);

        using var scope = NewScope();
        var gdt = scope.ServiceProvider.GetRequiredService<IGestionDuTempsService>();

        // Toujours vrai avant le rattachement à une 2e entreprise : la vérification d'appartenance
        // (VerifierCompanyIdAsync) accepte N'IMPORTE LAQUELLE des entreprises réellement liées, pas
        // seulement la première — c'est exactement ce que ce test prouve.
        await gdt.UpsertTypeDeTempsAsync(userId, null, "cle-a", "Type A", new TimeOnly(9, 0), new TimeOnly(10, 0), "1111100", 1, companyA.Id);
        await gdt.UpsertTypeDeTempsAsync(userId, null, "cle-b", "Type B", new TimeOnly(9, 0), new TimeOnly(10, 0), "1111100", 2, companyB.Id);

        var types = await gdt.GetTypesDeTempsAsync(userId);
        Assert.Contains(types, t => t.Cle == "cle-a" && t.CompanyId == companyA.Id);
        Assert.Contains(types, t => t.Cle == "cle-b" && t.CompanyId == companyB.Id);
    }

    [Fact]
    public async Task Frontiere_LeRattachementAUneEntreprise_NeDonnePasAccesAuGestionDuTempsPersonnelDuManager()
    {
        var suffix = Guid.NewGuid();
        var ownerUserId = $"frontiere-owner-{suffix}";
        var company = await fixture.CreateCompanyAsync($"Entreprise Frontiere {suffix}", ownerUserId);

        using var scope = NewScope();
        var provisioning = scope.ServiceProvider.GetRequiredService<ICompanyProvisioningService>();
        var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var gdt = scope.ServiceProvider.GetRequiredService<IGestionDuTempsService>();

        var manager = await CreateUserAsync(scope, "frontiere-manager");
        var invitation = await provisioning.InviterEmployeAsync(ownerUserId, company.Id, manager.Email!, coreDb);
        await provisioning.FinaliserEmployeDepuisInvitationAsync(invitation!, manager.Id, coreDb);

        // Le manager tague son propre type de temps sur l'entreprise partagée — donnée strictement personnelle.
        await gdt.UpsertTypeDeTempsAsync(manager.Id, null, "cle-personnelle-manager", "Rituel personnel du manager",
            new TimeOnly(8, 0), new TimeOnly(9, 0), "1111100", 1, company.Id);

        // Le propriétaire (même entreprise !) ne voit RIEN du Gestion du temps du manager — chaque appel
        // reste scopé par le userId explicite du CALLEUR, jamais par appartenance à une entreprise commune.
        var typesProprietaire = await gdt.GetTypesDeTempsAsync(ownerUserId);
        Assert.DoesNotContain(typesProprietaire, t => t.Cle == "cle-personnelle-manager");

        // Le manager, lui, voit bien son propre rituel.
        var typesManager = await gdt.GetTypesDeTempsAsync(manager.Id);
        Assert.Contains(typesManager, t => t.Cle == "cle-personnelle-manager");
    }
}
