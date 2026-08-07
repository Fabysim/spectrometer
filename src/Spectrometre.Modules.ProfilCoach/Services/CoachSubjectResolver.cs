using Spectrometre.Core.Modules;

namespace Spectrometre.Modules.ProfilCoach.Services;

/// <summary>
/// Implémentation réelle de <see cref="ICoachSubjectResolver"/> (Core) — relais vers
/// <see cref="ICoachProfileService.GetOrCreateProfileIdAsync"/>, même principe que
/// <c>CandidateSubjectResolver</c> (ProfilCandidat).
/// </summary>
public sealed class CoachSubjectResolver(ICoachProfileService coachProfileService) : ICoachSubjectResolver
{
    public Task<int> GetOrCreateCoachProfileIdAsync(string userId, CancellationToken cancellationToken = default) =>
        coachProfileService.GetOrCreateProfileIdAsync(userId, cancellationToken);
}
