using Spectrometre.Modules.ProfilEntreprise.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Core.Invitations;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.Recrutement.Services;
using Spectrometre.Modules.ProfilCandidat.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Invitation candidat (type <see cref="InvitationType.CandidaturePoste"/>) : émission par le propriétaire,
/// acceptation qui crée une <c>Candidature</c> liée au bon poste, et refus si le ContextId pointe un
/// poste hors des entreprises de l'émetteur (même pattern que <see cref="ManagerInvitationTests"/>).
/// </summary>
[Collection("Base de données partagée")]
public sealed class CandidatureInvitationTests(ServiceFixture fixture)
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
    public async Task Proprietaire_InviteCandidat_AcceptationCreeCandidatureSurLeBonPoste()
    {
        var suffix = Guid.NewGuid();
        var ownerUserId = $"candidat-invite-owner-{suffix}";
        var company = await fixture.CreateCompanyAsync($"Entreprise Candidat Invite {suffix}", ownerUserId);

        using var scope = NewScope();
        var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        tenantContext.SetActiveCompany(company.Id, company.SchemaName);
        var posteService = scope.ServiceProvider.GetRequiredService<IPosteService>();
        var candidateProfiles = scope.ServiceProvider.GetRequiredService<ICandidateProfileService>();

        var posteId = await posteService.CreatePosteAsync($"Poste invite {suffix}", "Desc", null);
        var candidat = await CreateUserAsync(scope, "candidat-invite");

        var invitation = await posteService.InviterCandidatAsync(posteId, candidat.Email!, ownerUserId);
        Assert.Equal(InvitationType.CandidaturePoste, invitation.Type);
        Assert.Equal(posteId, invitation.ContextId);

        var enCours = await posteService.GetInvitationsCandidatEnCoursAsync(posteId);
        Assert.Contains(enCours, i => i.Id == invitation.Id && i.EmailInvite == candidat.Email);

        // Même finalisation que InvitationAcceptancePage pour CandidaturePoste.
        await posteService.FinaliserCandidatureDepuisInvitationAsync(invitation, candidat.Id);
        await scope.ServiceProvider.GetRequiredService<IInvitationService>()
            .MarquerAccepteeAsync(invitation.Id, coreDb);

        var candidateProfileId = await candidateProfiles.GetOrCreateProfileIdAsync(candidat.Id);
        var candidatures = await posteService.GetCandidaturesAsync(posteId);
        var candidature = Assert.Single(candidatures);
        Assert.Equal(posteId, candidature.PosteId);
        Assert.Equal(candidateProfileId, candidature.CandidateProfileId);

        Assert.Empty(await posteService.GetInvitationsCandidatEnCoursAsync(posteId));
    }

    [Fact]
    public async Task Candidat_NePeutPasFinaliserUneInvitationDontLePosteNAppartientPasALEmetteur()
    {
        var suffix = Guid.NewGuid();
        var ownerA = $"candidat-invite-owner-a-{suffix}";
        var ownerB = $"candidat-invite-owner-b-{suffix}";
        var companyA = await fixture.CreateCompanyAsync($"Entreprise Invite A {suffix}", ownerA);
        var companyB = await fixture.CreateCompanyAsync($"Entreprise Invite B {suffix}", ownerB);

        using var scope = NewScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var posteService = scope.ServiceProvider.GetRequiredService<IPosteService>();
        var invitationService = scope.ServiceProvider.GetRequiredService<IInvitationService>();
        var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();

        tenantContext.SetActiveCompany(companyB.Id, companyB.SchemaName);
        var posteIdB = await posteService.CreatePosteAsync($"Poste B {suffix}", null, null);

        // Invitation émise par A mais ContextId = poste de B (A n'est pas lié à B) — finalisation doit échouer.
        var candidat = await CreateUserAsync(scope, "candidat-wrong-poste");
        var invitationTrompante = await invitationService.CreerAsync(
            ownerA,
            candidat.Email!,
            InvitationType.CandidaturePoste,
            contextId: posteIdB,
            coreDb);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => posteService.FinaliserCandidatureDepuisInvitationAsync(invitationTrompante, candidat.Id));
        Assert.Contains("introuvable", ex.Message, StringComparison.OrdinalIgnoreCase);

        // Aucune candidature créée côté B.
        Assert.Empty(await posteService.GetCandidaturesAsync(posteIdB));

        // Côté A : poste réel, mais pas de candidature non plus (le ContextId ne pointait pas vers A).
        tenantContext.SetActiveCompany(companyA.Id, companyA.SchemaName);
        var posteIdA = await posteService.CreatePosteAsync($"Poste A {suffix}", null, null);
        Assert.Empty(await posteService.GetCandidaturesAsync(posteIdA));
    }

    [Fact]
    public async Task InvitationPourPosteA_NApparaitPasEtNeCreeRienSurEntrepriseB()
    {
        var suffix = Guid.NewGuid();
        var ownerA = $"candidat-iso-owner-a-{suffix}";
        var ownerB = $"candidat-iso-owner-b-{suffix}";
        var companyA = await fixture.CreateCompanyAsync($"Entreprise Iso Invite A {suffix}", ownerA);
        var companyB = await fixture.CreateCompanyAsync($"Entreprise Iso Invite B {suffix}", ownerB);

        using var scope = NewScope();
        var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
        var posteService = scope.ServiceProvider.GetRequiredService<IPosteService>();
        var candidateProfiles = scope.ServiceProvider.GetRequiredService<ICandidateProfileService>();

        tenantContext.SetActiveCompany(companyA.Id, companyA.SchemaName);
        var posteIdA = await posteService.CreatePosteAsync($"Poste iso A {suffix}", null, null);

        tenantContext.SetActiveCompany(companyB.Id, companyB.SchemaName);
        var posteIdB = await posteService.CreatePosteAsync($"Poste iso B {suffix}", null, null);

        tenantContext.SetActiveCompany(companyA.Id, companyA.SchemaName);
        var candidat = await CreateUserAsync(scope, "candidat-iso");
        var invitation = await posteService.InviterCandidatAsync(posteIdA, candidat.Email!, ownerA);
        await posteService.FinaliserCandidatureDepuisInvitationAsync(invitation, candidat.Id);

        var profileId = await candidateProfiles.GetOrCreateProfileIdAsync(candidat.Id);
        var candidaturesA = await posteService.GetCandidaturesAsync(posteIdA);
        Assert.Equal(profileId, Assert.Single(candidaturesA).CandidateProfileId);

        // Même PosteId numérique éventuel côté B : aucune invitation listée, aucune candidature.
        tenantContext.SetActiveCompany(companyB.Id, companyB.SchemaName);
        Assert.Empty(await posteService.GetInvitationsCandidatEnCoursAsync(posteIdA));
        Assert.Empty(await posteService.GetInvitationsCandidatEnCoursAsync(posteIdB));
        Assert.Empty(await posteService.GetCandidaturesAsync(posteIdB));
        if (posteIdA == posteIdB)
            Assert.Empty(await posteService.GetCandidaturesAsync(posteIdA));
    }
}
