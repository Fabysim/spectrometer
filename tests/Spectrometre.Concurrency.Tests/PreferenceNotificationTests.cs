using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Modules;
using Spectrometre.Core.Notifications;
using Spectrometre.Core.Tenancy;
using Spectrometre.Modules.GestionDuTemps.Services;
using Spectrometre.Modules.ProfilCoach.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

[Collection("Base de données partagée")]
public sealed class PreferenceNotificationTests(ServiceFixture fixture)
{
    private IServiceScope NewScope() => fixture.Services.CreateScope();

    [Fact]
    public async Task CreerAsync_IgnoreQuandPreferenceCategorieDesactivee()
    {
        var userId = $"pref-block-{Guid.NewGuid()}";

        using (var scope = NewScope())
        {
            var prefs = scope.ServiceProvider.GetRequiredService<IPreferenceNotificationService>();
            await prefs.SetPreferenceAsync(userId, NotificationCategoryCodes.Coaching, active: false);

            var notif = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var id = await notif.CreerAsync(userId, "Titre", "Msg", "/x", "Coaching.DemandeRecue");
            Assert.Equal(0, id);
            Assert.Empty(await notif.GetNonLuesAsync(userId));
        }

        using (var scope = NewScope())
        {
            var prefs = scope.ServiceProvider.GetRequiredService<IPreferenceNotificationService>();
            await prefs.SetPreferenceAsync(userId, NotificationCategoryCodes.Coaching, active: true);

            var notif = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var id = await notif.CreerAsync(userId, "Titre", "Msg", "/x", "Coaching.DemandeRecue");
            Assert.True(id > 0);
            Assert.Contains(await notif.GetNonLuesAsync(userId), n => n.Id == id);
        }
    }

    [Fact]
    public async Task GetPreferencesAsync_NInclutPasSuiviEmployesSansModuleProprietaire()
    {
        var suffix = Guid.NewGuid();
        var candidat = $"pref-cand-{suffix}";
        var coach = $"pref-coach-{suffix}";

        using (var scope = NewScope())
        {
            var coachProfiles = scope.ServiceProvider.GetRequiredService<ICoachProfileService>();
            await coachProfiles.SaveProfilAsync(coach, "Coach Pref", "Bio", "test", visibleDansAnnuaire: true);

            // Profil candidat créé via demande coaching / subject resolver
            var candidateResolver = scope.ServiceProvider.GetRequiredService<ICandidateSubjectResolver>();
            _ = await candidateResolver.GetOrCreateCandidateProfileIdAsync(candidat);
        }

        using (var scope = NewScope())
        {
            var prefs = scope.ServiceProvider.GetRequiredService<IPreferenceNotificationService>();
            var forCandidat = await prefs.GetPreferencesAsync(candidat);
            Assert.Contains(forCandidat, p => p.CategorieCode == NotificationCategoryCodes.Coaching);
            Assert.DoesNotContain(forCandidat, p => p.CategorieCode == NotificationCategoryCodes.SuiviEmployes);

            var forCoach = await prefs.GetPreferencesAsync(coach);
            Assert.Contains(forCoach, p => p.CategorieCode == NotificationCategoryCodes.Coaching);
            Assert.DoesNotContain(forCoach, p => p.CategorieCode == NotificationCategoryCodes.SuiviEmployes);
        }
    }

    [Fact]
    public async Task GetPreferencesAsync_InclutSuiviEmployesPourProprietaireAvecModule()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var owner = $"pref-owner-{suffix}";
        var company = await fixture.CreateCompanyAsync($"Pref SE {suffix}", owner);

        using (var scope = NewScope())
        {
            var registry = scope.ServiceProvider.GetRequiredService<IModuleRegistry>();
            var core = scope.ServiceProvider.GetRequiredService<Spectrometre.Core.Data.CoreDbContext>();
            await registry.ActivateForCompanyAsync(company.Id, "SuiviEmployes", core);
        }

        using (var scope = NewScope())
        {
            var prefs = scope.ServiceProvider.GetRequiredService<IPreferenceNotificationService>();
            var list = await prefs.GetPreferencesAsync(owner);
            Assert.Contains(list, p => p.CategorieCode == NotificationCategoryCodes.SuiviEmployes);
        }
    }
}

public sealed class ActiviteNotificationSelectorTests
{
    [Fact]
    public void SelectDue_DebutEtFinDansLaFenetre_RespecteLesFlags()
    {
        var debut = new DateTime(2026, 8, 9, 14, 0, 0);
        var snap = new ActiviteScheduleSnapshot(
            1, "u", "Réunion",
            DateOnly.FromDateTime(debut),
            TimeOnly.FromDateTime(debut),
            DureeMinutes: 60,
            NotificationDebutEnvoyee: false,
            NotificationFinEnvoyee: false);

        var dueDebut = ActiviteNotificationSelector.SelectDue(
            [snap], debut, debut.AddMinutes(1));
        Assert.Single(dueDebut);
        Assert.Equal(ActiviteNotificationKind.Debut, dueDebut[0].Kind);

        var dueFin = ActiviteNotificationSelector.SelectDue(
            [snap], debut.AddMinutes(60), debut.AddMinutes(61));
        Assert.Single(dueFin);
        Assert.Equal(ActiviteNotificationKind.Fin, dueFin[0].Kind);

        var dejaEnvoye = snap with { NotificationDebutEnvoyee = true, NotificationFinEnvoyee = true };
        Assert.Empty(ActiviteNotificationSelector.SelectDue(
            [dejaEnvoye], debut, debut.AddMinutes(1)));
        Assert.Empty(ActiviteNotificationSelector.SelectDue(
            [dejaEnvoye], debut.AddMinutes(60), debut.AddMinutes(61)));
    }
}

[Collection("Base de données partagée")]
public sealed class ActiviteNotificationActionTests(ServiceFixture fixture)
{
    [Fact]
    public async Task DemarrerEtTerminer_RefuseNonProprietaire_SuccesProprietaire()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var owner = $"act-owner-{suffix}";
        var other = $"act-other-{suffix}";
        int activiteId;

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var gdt = scope.ServiceProvider.GetRequiredService<IGestionDuTempsService>();
            await gdt.GetOrCreateCycleActifAsync(owner);
            var types = await gdt.GetTypesDeTempsAsync(owner);
            var typeId = types[0].Id;
            var now = DateTime.Now;
            activiteId = await gdt.CreateActiviteAsync(
                owner,
                typeId,
                "Test notif action",
                DateOnly.FromDateTime(now),
                TimeOnly.FromDateTime(now),
                30,
                companyId: null);
        }

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var actions = scope.ServiceProvider.GetRequiredService<IActiviteNotificationActionService>();
            Assert.False(await actions.DemarrerSiProprietaireAsync(other, activiteId));
            Assert.True(await actions.DemarrerSiProprietaireAsync(owner, activiteId));
            Assert.False(await actions.TerminerSiProprietaireAsync(other, activiteId));
            Assert.True(await actions.TerminerSiProprietaireAsync(owner, activiteId));
        }
    }
}
