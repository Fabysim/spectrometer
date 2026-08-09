using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Notifications;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

[Collection("Base de données partagée")]
public sealed class NotificationServiceTests(ServiceFixture fixture)
{
    private IServiceScope NewScope() => fixture.Services.CreateScope();

    [Fact]
    public async Task MarquerLueAsync_RefuseQuandNotificationDUnAutreUtilisateur()
    {
        var owner = $"notif-owner-{Guid.NewGuid()}";
        var other = $"notif-other-{Guid.NewGuid()}";
        int id;

        using (var scope = NewScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<INotificationService>();
            id = await svc.CreerAsync(owner, "Titre", "Message", "/coach/suivis", "Test.Type");
        }

        using (var scope = NewScope())
        {
            var svc = scope.ServiceProvider.GetRequiredService<INotificationService>();
            Assert.False(await svc.MarquerLueAsync(id, other));

            var nonLues = await svc.GetNonLuesAsync(owner);
            Assert.Contains(nonLues, n => n.Id == id);

            Assert.True(await svc.MarquerLueAsync(id, owner));
            Assert.DoesNotContain(await svc.GetNonLuesAsync(owner), n => n.Id == id);
        }
    }

    [Fact]
    public async Task DemandeCoaching_CreeNotificationPourLeCoach()
    {
        var suffix = Guid.NewGuid();
        var suivi = $"notif-suivi-{suffix}";
        var coach = $"notif-coach-{suffix}";

        using (var scope = NewScope())
        {
            var coachProfiles = scope.ServiceProvider.GetRequiredService<Spectrometre.Modules.ProfilCoach.Services.ICoachProfileService>();
            await coachProfiles.SaveProfilAsync(coach, "Coach Notif", "Bio", "test", visibleDansAnnuaire: true);
        }

        using (var scope = NewScope())
        {
            var coaching = scope.ServiceProvider.GetRequiredService<Spectrometre.Modules.Coaching.Services.ICoachingService>();
            Assert.True(await coaching.DemanderCoachDepuisAnnuaireAsync(suivi, coach));

            var notif = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var nonLues = await notif.GetNonLuesAsync(coach);
            Assert.Contains(nonLues, n => n.TypeCode == "Coaching.DemandeRecue" && n.Lien == "/coach/suivis");
        }
    }
}
