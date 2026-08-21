using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Modules.Coaching.Entities;
using Spectrometre.Modules.Coaching.Services;
using Spectrometre.Modules.ProfilCoach.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Gardes d'accès et cycle brouillon → terminer → archive pour <see cref="IObjectifsCoachingService"/>.
/// Direction d'accès : coach propriétaire du lien actif uniquement
/// (<c>requestingCoachUserId == LienCoaching.CoachUserId</c>), inverse de
/// <see cref="ICoachingService.GetSuiviUserIdSiAutoriseAsync"/>.
/// </summary>
[Collection("Base de données partagée")]
public sealed class ObjectifsCoachingTests(ServiceFixture fixture)
{
    private IServiceScope NewScope() => fixture.Services.CreateScope();

    private async Task<(int LienId, string CoachUserId, string SuiviUserId)> CreerLienActifAsync()
    {
        var suffix = Guid.NewGuid();
        var suiviUserId = $"obj-coaching-suivi-{suffix}";
        var coachUserId = $"obj-coaching-coach-{suffix}";

        using (var scope = NewScope())
        {
            var coachProfileService = scope.ServiceProvider.GetRequiredService<ICoachProfileService>();
            await coachProfileService.SaveProfilAsync(coachUserId, "Coach Obj", "Bio", "objectifs", visibleDansAnnuaire: true);
        }

        using (var scope = NewScope())
        {
            var coaching = scope.ServiceProvider.GetRequiredService<ICoachingService>();
            Assert.True(await coaching.DemanderCoachDepuisAnnuaireAsync(suiviUserId, coachUserId));
            var lien = Assert.Single(await coaching.GetLiensPourCoachAsync(coachUserId));
            Assert.True(await coaching.AccepterAsync(lien.Id, coachUserId));
            return (lien.Id, coachUserId, suiviUserId);
        }
    }

    [Fact]
    public async Task AccesRefuse_QuandCoachNEstPasProprietaireDuLien()
    {
        var (lienId, coachUserId, _) = await CreerLienActifAsync();
        var autreCoach = $"obj-coaching-autre-{Guid.NewGuid()}";

        using var scope = NewScope();
        var svc = scope.ServiceProvider.GetRequiredService<IObjectifsCoachingService>();

        Assert.Null(await svc.GetPeriodeCouranteAsync(lienId, autreCoach));
        Assert.False(await svc.SaveObjectifsAsync(lienId, autreCoach, [
            new ObjectifCoachingInput(null, DateOnly.FromDateTime(DateTime.UtcNow), "X", null, AtteinteObjectifCoaching.NonDefini, null, null)
        ]));
        Assert.False(await svc.TerminerPeriodeAsync(lienId, autreCoach));
        Assert.Empty(await svc.GetArchivesAsync(lienId, autreCoach));

        // Le vrai coach a toujours accès.
        Assert.NotNull(await svc.GetPeriodeCouranteAsync(lienId, coachUserId));
    }

    [Fact]
    public async Task AccesRefuse_QuandLienNonActif()
    {
        var (lienId, coachUserId, suiviUserId) = await CreerLienActifAsync();

        using (var scope = NewScope())
        {
            var coaching = scope.ServiceProvider.GetRequiredService<ICoachingService>();
            Assert.True(await coaching.RevoquerAsync(lienId, suiviUserId));
        }

        using (var scope = NewScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<IObjectifsCoachingService>();
            Assert.Null(await svc.GetPeriodeCouranteAsync(lienId, coachUserId));
            Assert.False(await svc.SaveObjectifsAsync(lienId, coachUserId, []));
            Assert.False(await svc.TerminerPeriodeAsync(lienId, coachUserId));
            Assert.Empty(await svc.GetArchivesAsync(lienId, coachUserId));
        }
    }

    [Fact]
    public async Task AccesRefuse_QuandDemandeurEstLaPersonneSuivie()
    {
        var (lienId, _, suiviUserId) = await CreerLienActifAsync();

        using var scope = NewScope();
        var svc = scope.ServiceProvider.GetRequiredService<IObjectifsCoachingService>();
        Assert.Null(await svc.GetPeriodeCouranteAsync(lienId, suiviUserId));
        Assert.False(await svc.SaveObjectifsAsync(lienId, suiviUserId, []));
    }

    [Fact]
    public async Task CycleComplet_Brouillon_Terminer_Archive()
    {
        var (lienId, coachUserId, suiviUserId) = await CreerLienActifAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using (var scope = NewScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<IObjectifsCoachingService>();

            var periode = await svc.GetPeriodeCouranteAsync(lienId, coachUserId);
            Assert.NotNull(periode);
            Assert.Equal(lienId, periode.LienCoachingId);
            Assert.Equal(suiviUserId, periode.SuiviUserId);
            Assert.False(periode.Archivee);
            Assert.Empty(periode.Objectifs);

            Assert.True(await svc.SaveObjectifsAsync(lienId, coachUserId, [
                new ObjectifCoachingInput(null, today, "Améliorer le sommeil", "Routine soir", AtteinteObjectifCoaching.NonDefini, null, null),
                new ObjectifCoachingInput(null, today, "Prioriser", null, AtteinteObjectifCoaching.Oui, "En cours", 70),
            ]));

            periode = await svc.GetPeriodeCouranteAsync(lienId, coachUserId);
            Assert.NotNull(periode);
            Assert.Equal(2, periode.Objectifs.Count);
            Assert.Contains(periode.Objectifs, o => o.Titre == "Améliorer le sommeil");
            Assert.Contains(periode.Objectifs, o => o.Titre == "Prioriser" && o.Atteinte == AtteinteObjectifCoaching.Oui && o.Note == 70);

            Assert.Empty(await svc.GetArchivesAsync(lienId, coachUserId));

            Assert.True(await svc.TerminerPeriodeAsync(lienId, coachUserId, [
                new ObjectifCoachingInput(null, today, "Améliorer le sommeil", "Routine soir", AtteinteObjectifCoaching.Oui, "OK", 90),
            ]));
        }

        using (var scope = NewScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<IObjectifsCoachingService>();

            var archives = await svc.GetArchivesAsync(lienId, coachUserId);
            var archive = Assert.Single(archives);
            Assert.True(archive.Archivee);
            var objArchive = Assert.Single(archive.Objectifs);
            Assert.Equal("Améliorer le sommeil", objArchive.Titre);
            Assert.Equal(AtteinteObjectifCoaching.Oui, objArchive.Atteinte);
            Assert.Equal(90, objArchive.Note);

            var nouvelle = await svc.GetPeriodeCouranteAsync(lienId, coachUserId);
            Assert.NotNull(nouvelle);
            Assert.False(nouvelle.Archivee);
            Assert.Empty(nouvelle.Objectifs);
            Assert.NotEqual(archive.Id, nouvelle.Id);
        }
    }

    [Fact]
    public async Task Jeune_LitSaPeriodeCourante_SansObservationNiNote_NiCelleDUnAutre()
    {
        var (lienId, coachUserId, suiviUserId) = await CreerLienActifAsync();
        var (autreLienId, autreCoach, autreSuivi) = await CreerLienActifAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using (var scope = NewScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<IObjectifsCoachingService>();
            Assert.Null(await svc.GetPeriodeCourantePourJeuneAsync(suiviUserId));

            Assert.True(await svc.SaveObjectifsAsync(lienId, coachUserId, [
                new ObjectifCoachingInput(null, today, "Mieux gérer mon temps", "Agenda simple", AtteinteObjectifCoaching.NonDefini, "Note coach interne", 40),
            ]));
            Assert.True(await svc.SaveObjectifsAsync(autreLienId, autreCoach, [
                new ObjectifCoachingInput(null, today, "Objectif secret d'un autre", null, AtteinteObjectifCoaching.Oui, "confidentiel", 99),
            ]));
        }

        using (var scope = NewScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<IObjectifsCoachingService>();
            var vue = await svc.GetPeriodeCourantePourJeuneAsync(suiviUserId);
            Assert.NotNull(vue);
            var objectif = Assert.Single(vue.Objectifs);
            Assert.Equal("Mieux gérer mon temps", objectif.Titre);
            Assert.Equal("Agenda simple", objectif.Moyens);
            Assert.Equal(AtteinteObjectifCoaching.NonDefini, objectif.Atteinte);
            Assert.DoesNotContain(vue.Objectifs, o => o.Titre.Contains("secret", StringComparison.OrdinalIgnoreCase));

            var autre = await svc.GetPeriodeCourantePourJeuneAsync(autreSuivi);
            Assert.NotNull(autre);
            Assert.Equal("Objectif secret d'un autre", Assert.Single(autre.Objectifs).Titre);

            Assert.Null(await svc.GetPeriodeCourantePourJeuneAsync($"inconnu-{Guid.NewGuid()}"));
        }
    }
}
