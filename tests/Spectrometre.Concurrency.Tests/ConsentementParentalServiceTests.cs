using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Spectrometre.Core.Identity;
using Spectrometre.Modules.JeunesPrestataires.Data;
using Spectrometre.Modules.JeunesPrestataires.Entities;
using Spectrometre.Modules.JeunesPrestataires.Resources;
using Spectrometre.Modules.JeunesPrestataires.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

[Collection("Base de données partagée")]
public sealed class ConsentementParentalServiceTests(ServiceFixture fixture)
{
    private IConsentementParentalService ConsentementService =>
        fixture.Services.GetRequiredService<IConsentementParentalService>();

    [Fact]
    public async Task ConfirmerAsync_ChampsObligatoiresManquants_ListePrecisementLesChamps()
    {
        var jeuneProfileId = await CreerJeuneProfileAsync(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-15)));

        await ConsentementService.SaveBrouillonAsync(jeuneProfileId, new ConsentementParentalFormModel
        {
            Parent1Nom = "Martin",
        });

        var result = await ConsentementService.ConfirmerAsync(jeuneProfileId, "", "", null);

        Assert.False(result.Success);
        Assert.Contains(ConsentementChamps.Parent1Lien, result.ChampsManquants);
        Assert.Contains(ConsentementChamps.AutorisationMissions, result.ChampsManquants);
        Assert.Contains(ConsentementChamps.NomJeuneConfirmation, result.ChampsManquants);
    }

    [Fact]
    public async Task ConfirmerAsync_Complet_FixeValideLe()
    {
        var jeuneProfileId = await CreerJeuneProfileAsync(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-15)));

        await ConsentementService.SaveBrouillonAsync(jeuneProfileId, CreerFormulaireComplet());

        var result = await ConsentementService.ConfirmerAsync(
            jeuneProfileId,
            "Léa Dupont",
            "Marie Martin",
            null);

        Assert.True(result.Success);
        Assert.True(await ConsentementService.EstConsentementValideAsync(jeuneProfileId));
    }

    [Fact]
    public async Task SaveBrouillonApresValidation_ReinitialiseValideLe()
    {
        var jeuneProfileId = await CreerJeuneProfileAsync(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-15)));

        await ConsentementService.SaveBrouillonAsync(jeuneProfileId, CreerFormulaireComplet());
        await ConsentementService.ConfirmerAsync(jeuneProfileId, "Léa Dupont", "Marie Martin", null);

        var form = CreerFormulaireComplet();
        form.Parent1Telephone = "0499887766";
        await ConsentementService.SaveBrouillonAsync(jeuneProfileId, form);

        Assert.False(await ConsentementService.EstConsentementValideAsync(jeuneProfileId));
    }

    [Fact]
    public async Task ReprendreEditionAsync_ReinitialiseValideLe()
    {
        var jeuneProfileId = await CreerJeuneProfileAsync(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-15)));

        await ConsentementService.SaveBrouillonAsync(jeuneProfileId, CreerFormulaireComplet());
        await ConsentementService.ConfirmerAsync(jeuneProfileId, "Léa Dupont", "Marie Martin", null);

        await ConsentementService.ReprendreEditionAsync(jeuneProfileId);

        Assert.False(await ConsentementService.EstConsentementValideAsync(jeuneProfileId));
    }

    [Fact]
    public async Task EstConsentementValideAsync_MajeurSansEnregistrement_RetourneTrue()
    {
        var jeuneProfileId = await CreerJeuneProfileAsync(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-22)));

        Assert.True(await ConsentementService.EstConsentementValideAsync(jeuneProfileId));
    }

    [Fact]
    public async Task EstConsentementValideAsync_MineurSansValidation_RetourneFalse()
    {
        var jeuneProfileId = await CreerJeuneProfileAsync(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-15)));

        Assert.False(await ConsentementService.EstConsentementValideAsync(jeuneProfileId));
    }

    [Fact]
    public async Task GetAsync_SansGardeCoach_ResteDisponiblePourLeJeune()
    {
        var jeuneProfileId = await CreerJeuneProfileAsync(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-15)));
        await ConsentementService.SaveBrouillonAsync(jeuneProfileId, CreerFormulaireComplet());

        var vue = await ConsentementService.GetAsync(jeuneProfileId);

        Assert.False(vue.EstValide);
        Assert.Equal("Marie Martin", vue.Entity.Parent1Nom);
        Assert.Equal("0470123456", vue.Entity.Parent1Telephone);
    }

    [Fact]
    public async Task TryGetPourCoachAsync_SansLienCoaching_RetourneNullMemeSiValide()
    {
        var jeuneProfileId = await CreerJeuneProfileAsync(DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-15)));
        await ConsentementService.SaveBrouillonAsync(jeuneProfileId, CreerFormulaireComplet());
        Assert.True((await ConsentementService.ConfirmerAsync(jeuneProfileId, "Léa Dupont", "Marie Martin", null)).Success);

        Assert.Null(await ConsentementService.TryGetPourCoachAsync("coach-inexistant", jeuneProfileId));
    }

    [Fact]
    public void GeneratePdf_ProduitUnPdfNonVide()
    {
        var jeune = new JeuneProfileView(
            1,
            "user",
            "Dupont",
            "Léa",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-15)),
            1,
            DateTimeOffset.UtcNow,
            ProfilAccompagnement.SansExperience);
        var entity = new ConsentementParental
        {
            Parent1Nom = "Marie Martin",
            Parent1Lien = "Mère",
            Parent1Adresse = "10 rue Centrale",
            Parent1Telephone = "0470123456",
            Parent1Email = "marie.martin@example.com",
            AutorisationMissions = true,
            AutorisationRevenus = true,
            PartParascolairePourcent = 70m,
            PartArgentDePochePourcent = 30m,
            AutorisationDonneesEtImage = true,
            EngagementScolariteSanteEquilibre = true,
            EngagementInformerContraintes = true,
            EngagementEncouragerCharte = true,
            EngagementSignalerMissionInadaptee = true,
            EngagementCollaborerCoach = true,
            NomJeuneConfirmation = "Léa Dupont",
            NomParent1Confirmation = "Marie Martin",
            ValideLe = DateTimeOffset.UtcNow,
        };
        var vue = new ConsentementParentalView(entity, true);
        var pdf = fixture.Services.GetRequiredService<IConsentementParentalPdfService>();
        var localizer = fixture.Services.GetRequiredService<IStringLocalizer<JeunesPrestatairesResource>>();

        var bytes = pdf.GeneratePdf(jeune, vue, localizer);

        Assert.NotEmpty(bytes);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }

    private async Task<int> CreerJeuneProfileAsync(DateOnly dateNaissance)
    {
        using var scope = fixture.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = $"jp-consent-{Guid.NewGuid()}@test.local";
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var create = await userManager.CreateAsync(user, "TestPassword123!");
        Assert.True(create.Succeeded);
        fixture.TrackUserForCleanup(user.Id);

        await using var db = await fixture.Services.GetRequiredService<IDbContextFactory<JeunesPrestatairesDbContext>>().CreateDbContextAsync();
        var profile = new JeuneProfile
        {
            UserId = user.Id,
            Nom = "Dupont",
            Prenoms = "Léa",
            DateNaissance = dateNaissance,
            InvitationId = 888_000 + Random.Shared.Next(1, 100_000),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.JeuneProfiles.Add(profile);
        await db.SaveChangesAsync();
        return profile.Id;
    }

    private static ConsentementParentalFormModel CreerFormulaireComplet() => new()
    {
        Parent1Nom = "Marie Martin",
        Parent1Lien = "Mère",
        Parent1Adresse = "10 rue Centrale",
        Parent1Telephone = "0470123456",
        Parent1Email = "marie.martin@example.com",
        AutorisationMissions = true,
        AutorisationRevenus = true,
        PartParascolairePourcent = 70m,
        PartArgentDePochePourcent = 30m,
        AutorisationDonneesEtImage = true,
        EngagementScolariteSanteEquilibre = true,
        EngagementInformerContraintes = true,
        EngagementEncouragerCharte = true,
        EngagementSignalerMissionInadaptee = true,
        EngagementCollaborerCoach = true,
    };
}
