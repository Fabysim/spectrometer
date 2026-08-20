using Spectrometre.Core.Modules;

namespace Spectrometre.Modules.Missions.Services;

public sealed class ParticulierSubjectResolver(IParticulierProfileService particulierProfileService) : IParticulierSubjectResolver
{
    public Task<int> GetOrCreateParticulierProfileIdAsync(string userId, CancellationToken cancellationToken = default) =>
        particulierProfileService.GetOrCreateProfileIdAsync(userId, "", "", cancellationToken);

    public async Task<int?> TryGetParticulierProfileIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var profil = await particulierProfileService.TryGetByUserIdAsync(userId, cancellationToken);
        return profil?.Id;
    }
}
