using Spectrometre.Modules.PostesRecrutement.Entities;

namespace Spectrometre.Modules.PostesRecrutement.Services;

public sealed record PosteView(int Id, string Titre, string? Description, string? Departement, PosteStatut Statut, DateTimeOffset CreatedAt);

/// <summary>Vue d'un poste ouvert pour la recherche côté candidat — regroupe le poste et l'entreprise qui l'a publié, puisque cette vue traverse plusieurs schémas tenant.</summary>
public sealed record PosteOuvertView(int CompanyId, string CompanyName, int PosteId, string Titre, string? Description, string? Departement, bool DejaPostule);

/// <summary>
/// <see cref="ScoreCompatibilite"/> n'est renseigné que si le module Compatibilité est actif pour ce
/// tenant (intégration légère, sans dépendance dure au manifeste — voir <c>ServiceCollectionExtensions</c>).
/// </summary>
public sealed record CandidatureView(int Id, int PosteId, int CandidateProfileId, CandidatureStatut Statut, DateTimeOffset CreatedAt, int? ScoreCompatibilite);

/// <summary>
/// Point d'entrée public du module Postes & Recrutement. Deux familles de méthodes : côté entreprise
/// (tenant ambiant, comme Profil Entreprise) et côté candidat (traversée explicite de plusieurs
/// schémas tenant, puisque parcourir les postes ouverts n'est pas une opération mono-tenant).
/// </summary>
public interface IPosteService
{
    // --- Côté entreprise (tenant actif via ITenantContext) ---
    Task<int> CreatePosteAsync(string titre, string? description, string? departement, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PosteView>> GetPostesAsync(CancellationToken cancellationToken = default);
    Task UpdatePosteAsync(int posteId, string titre, string? description, string? departement, CancellationToken cancellationToken = default);
    Task SetPosteStatutAsync(int posteId, PosteStatut statut, CancellationToken cancellationToken = default);

    /// <summary>Inclut le score de compatibilité par candidature si le module Compatibilité est actif pour ce tenant.</summary>
    Task<IReadOnlyList<CandidatureView>> GetCandidaturesAsync(int posteId, CancellationToken cancellationToken = default);
    Task SetCandidatureStatutAsync(int candidatureId, CandidatureStatut statut, CancellationToken cancellationToken = default);

    // --- Côté candidat (traverse tous les tenants) ---
    Task<IReadOnlyList<PosteOuvertView>> GetPostesOuvertsAsync(int candidateProfileId, CancellationToken cancellationToken = default);
    Task PostulerAsync(int companyId, int posteId, int candidateProfileId, CancellationToken cancellationToken = default);
}
