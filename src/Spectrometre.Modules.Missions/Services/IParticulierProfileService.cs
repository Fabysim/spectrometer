namespace Spectrometre.Modules.Missions.Services;

public interface IParticulierProfileService
{
    Task<ParticulierProfileView?> TryGetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<ParticulierProfileView?> TryGetByIdAsync(int particulierProfileId, CancellationToken cancellationToken = default);

    Task<int> GetOrCreateProfileIdAsync(string userId, string nom, string prenoms, CancellationToken cancellationToken = default);
}
