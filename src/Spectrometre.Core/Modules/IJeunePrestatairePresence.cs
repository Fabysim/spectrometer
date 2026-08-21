namespace Spectrometre.Core.Modules;

/// <summary>
/// Inversion de dépendance : <c>Coaching</c> doit savoir si un compte est un jeune prestataire
/// (un seul coach actif) sans référencer <c>JeunesPrestataires</c> — le manifeste va dans l'autre
/// sens. Même recette que <see cref="ICoachingAccessChecker"/>.
/// </summary>
public interface IJeunePrestatairePresence
{
    Task<bool> EstJeunePrestataireAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>Filet de sécurité si le module JeunesPrestataires n'est pas câblé — jamais de jeune, donc pas de contrainte « un seul coach ».</summary>
public sealed class NoOpJeunePrestatairePresence : IJeunePrestatairePresence
{
    public Task<bool> EstJeunePrestataireAsync(string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
