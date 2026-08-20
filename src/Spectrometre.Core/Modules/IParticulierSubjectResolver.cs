namespace Spectrometre.Core.Modules;

/// <summary>
/// Résout l'identifiant particulier (<see cref="ModuleActivationSubjectType.Particulier"/>) à partir de l'utilisateur Identity.
/// </summary>
public interface IParticulierSubjectResolver
{
    Task<int> GetOrCreateParticulierProfileIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<int?> TryGetParticulierProfileIdAsync(string userId, CancellationToken cancellationToken = default);
}
