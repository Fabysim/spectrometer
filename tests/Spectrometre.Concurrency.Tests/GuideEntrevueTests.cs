using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Data;
using Spectrometre.Core.Identity;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.JeunesPrestataires.Catalog;
using Spectrometre.Modules.JeunesPrestataires.Data;
using Spectrometre.Modules.JeunesPrestataires.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

[Collection("Base de données partagée")]
public sealed class GuideEntrevueTests(ServiceFixture fixture)
{
    [Fact]
    public async Task Jeune_NePeutPasLireNiEcrireSonPropreGuide()
    {
        var (_, jeuneId, profileId) = await CreerJeuneAvecCoachAsync();
        var svc = fixture.Services.GetRequiredService<IGuideEntrevueService>();

        Assert.Null(await svc.GetOrCreateAsync(jeuneId, profileId));
        Assert.False(await svc.SaveAsync(
            jeuneId,
            profileId,
            "Motivations",
            null,
            null,
            "Notes secrètes",
            [new GuideEntrevuePeurNoteInput("peur_incapacite", "Note")]));
    }

    [Fact]
    public async Task CoachNonAutorise_NePeutNiLireNiEcrire()
    {
        var (_, _, profileId) = await CreerJeuneAvecCoachAsync();
        var autreCoach = await CreerUtilisateurAsync($"autre-coach-ge-{Guid.NewGuid()}@test.local");
        var svc = fixture.Services.GetRequiredService<IGuideEntrevueService>();

        Assert.Null(await svc.GetOrCreateAsync(autreCoach, profileId));
        Assert.False(await svc.SaveAsync(
            autreCoach,
            profileId,
            "Hack",
            null,
            null,
            null,
            []));
    }

    [Fact]
    public async Task SaveAsync_DeuxFois_MetAJourLeMemeEnregistrement()
    {
        var (coachId, _, profileId) = await CreerJeuneAvecCoachAsync();
        var svc = fixture.Services.GetRequiredService<IGuideEntrevueService>();

        Assert.True(await svc.SaveAsync(
            coachId,
            profileId,
            "Motivations v1",
            "Freins v1",
            "Missions v1",
            "Notes v1",
            [new GuideEntrevuePeurNoteInput("peur_erreur", "Note erreur v1")]));

        Assert.True(await svc.SaveAsync(
            coachId,
            profileId,
            "Motivations v2",
            "Freins v2",
            "Missions v2",
            "Notes v2",
            [
                new GuideEntrevuePeurNoteInput("peur_erreur", "Note erreur v2"),
                new GuideEntrevuePeurNoteInput("peur_jugement", "Note jugement"),
            ]));

        await using var db = await fixture.Services.GetRequiredService<IDbContextFactory<JeunesPrestatairesDbContext>>()
            .CreateDbContextAsync();
        var guides = await db.GuidesEntrevue.Where(g => g.JeuneProfileId == profileId).ToListAsync();
        Assert.Single(guides);

        var view = await svc.GetOrCreateAsync(coachId, profileId);
        Assert.NotNull(view);
        Assert.Equal(guides[0].Id, view!.Id);
        Assert.Equal("Motivations v2", view.Motivations);
        Assert.Equal("Freins v2", view.Freins);
        Assert.Equal("Missions v2", view.MissionsAdaptees);
        Assert.Equal("Notes v2", view.NotesConfidentielles);
        Assert.Equal("Note erreur v2", view.Peurs.Single(p => p.PeurKey == "peur_erreur").NoteCoach);
        Assert.Equal("Note jugement", view.Peurs.Single(p => p.PeurKey == "peur_jugement").NoteCoach);
        Assert.Equal(GuideEntrevueCatalog.Peurs.Count, view.Peurs.Count);
    }

    private async Task<(string CoachId, string JeuneId, int ProfileId)> CreerJeuneAvecCoachAsync()
    {
        var coachId = await CreerUtilisateurAsync($"coach-ge-{Guid.NewGuid()}@test.local");
        var jeuneEmail = $"jeune-ge-{Guid.NewGuid()}@test.local";
        var jeuneService = fixture.Services.GetRequiredService<IJeuneProfileService>();
        var coachingService = fixture.Services.GetRequiredService<ICoachingService>();

        var invite = await jeuneService.InviterJeuneAsync(
            coachId,
            jeuneEmail,
            "Martin",
            "Léa",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-15)),
            "http://localhost");
        Assert.True(invite.Success);

        var jeuneId = await CreerUtilisateurAsync(jeuneEmail);
        await using var coreDb = await fixture.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();
        var invitation = await coreDb.Invitations.FirstAsync(i => i.Id == invite.Invitation!.Id);

        var profile = await jeuneService.FinaliserDepuisInvitationAsync(invitation, jeuneId);
        await coachingService.FinaliserJeunePrestataireDepuisInvitationAsync(invitation, jeuneId);

        return (coachId, jeuneId, profile.Id);
    }

    private async Task<string> CreerUtilisateurAsync(string email)
    {
        using var scope = fixture.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, "TestPassword123!");
        Assert.True(result.Succeeded);
        fixture.TrackUserForCleanup(user.Id);
        return user.Id;
    }
}
