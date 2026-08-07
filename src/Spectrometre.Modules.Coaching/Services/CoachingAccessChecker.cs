using Spectrometre.Core.Modules;

namespace Spectrometre.Modules.Coaching.Services;

/// <summary>
/// Implémentation réelle de <see cref="ICoachingAccessChecker"/> (Core) — simple relais vers
/// <see cref="ICoachingService.GetSuiviUserIdSiAutoriseAsync"/>, même principe que
/// <c>CandidatureExistenceChecker</c> (PostesRecrutement) et <c>ProfileChangeRecorder</c> (SuiviEvolutif).
/// Enregistrée dans le conteneur DI depuis <c>Spectrometre.Host.Program</c>, jamais depuis
/// <c>Spectrometre.Modules.GestionDuTemps</c>.
/// </summary>
public sealed class CoachingAccessChecker(ICoachingService coachingService) : ICoachingAccessChecker
{
    public Task<string?> GetSuiviUserIdSiAutoriseAsync(string suiviUserId, string requestingCoachUserId, CancellationToken cancellationToken = default) =>
        coachingService.GetSuiviUserIdSiAutoriseAsync(suiviUserId, requestingCoachUserId, cancellationToken);
}
