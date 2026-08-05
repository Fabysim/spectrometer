namespace Spectrometre.Core.Tenancy;

/// <summary>
/// Marqueur implémenté par le DbContext de chaque module dont les tables vivent dans le schéma
/// de l'entreprise active plutôt que dans un schéma fixe. Permet à <see cref="TenantModelCacheKeyFactory"/>
/// de fonctionner génériquement, sans connaître chaque DbContext de module (contrairement à V1 où
/// le cache key factory était couplé en dur à <c>ApplicationDbContext</c>).
/// </summary>
public interface ITenantScopedDbContext
{
    string TenantSchema { get; }
}
