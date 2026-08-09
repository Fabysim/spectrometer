using Microsoft.Extensions.DependencyInjection;
using Spectrometre.Core.Modules;
using Xunit;

namespace Spectrometre.Concurrency.Tests;

/// <summary>
/// Accès coach SuiviEmployes : l'interface Core ne doit jamais confirmer l'existence
/// de données si le lien de coaching n'est pas autorisé.
/// </summary>
public sealed class SuiviEmployesCoachAccessTests(ServiceFixture fixture) : IClassFixture<ServiceFixture>
{
    [Fact]
    public async Task GetSuiviUserIdSiAutorise_SansLien_RetourneNull()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var checker = scope.ServiceProvider.GetRequiredService<ICoachingAccessChecker>();

        var result = await checker.GetSuiviUserIdSiAutoriseAsync(
            suiviUserId: Guid.NewGuid().ToString("N"),
            requestingCoachUserId: Guid.NewGuid().ToString("N"));

        Assert.Null(result);
    }
}
