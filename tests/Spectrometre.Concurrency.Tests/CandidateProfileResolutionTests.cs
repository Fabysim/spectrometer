using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Modules.ProfilCandidat.Services;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Reproduit la course identifiée sur <c>CandidateProfileService.GetOrCreateProfileIdAsync</c> — même
/// défaut que celui corrigé sur <c>CoachProfileService</c> pendant le cycle Coaching (voir sa remarque) :
/// <c>Home.razor</c> et <c>MainLayout.razor</c> résolvent chacun indépendamment le profil candidat dès le
/// premier rendu, potentiellement en concurrence lors du pré-rendu SSR d'une même requête. Avant le
/// correctif, la seconde résolution levait <c>Npgsql.PostgresException</c> (23505, violation de l'index
/// unique sur <c>UserId</c>) au lieu de relire le profil déjà créé par la première.
/// </summary>
[Collection("Base de données partagée")]
public sealed class CandidateProfileResolutionTests(ServiceFixture fixture)
{
    [Fact]
    public async Task ResolutionConcurrente_DuMemeProfilCandidat_NeLevePasEtRetourneLeMemeId()
    {
        var candidateService = fixture.Services.GetRequiredService<ICandidateProfileService>();
        var userId = $"test-race-candidat-{Guid.NewGuid()}";

        using var barrier = new Barrier(2);
        Task<int> RunAsync() => Task.Run(async () =>
        {
            barrier.SignalAndWait();
            return await candidateService.GetOrCreateProfileIdAsync(userId);
        });

        var taskA = RunAsync();
        var taskB = RunAsync();
        var ids = await Task.WhenAll(taskA, taskB);

        Assert.Equal(ids[0], ids[1]);
    }
}
