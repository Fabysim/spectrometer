namespace Spectrometre.Modules.GestionDuTemps.Services;

public sealed record TypeDeTempsView(int Id, string Cle, string Libelle, TimeOnly HeureDebut, TimeOnly HeureFin, string RecurrenceJours, int OrdreAffichage, int? CompanyId);

public sealed record ActiviteView(int Id, int TypeDeTempsId, string TypeLibelle, string TypeCouleur, string Nom, DateOnly DateActivite, TimeOnly HeureDebut, int DureeMinutes, int? CompanyId, string Statut);

/// <summary>
/// Point d'entrée public du module Gestion du temps. Toutes les méthodes prennent <c>userId</c> en
/// paramètre explicite (résolu par la page depuis <c>AuthenticationState</c>, même pattern que
/// <c>ICandidateProfileService.GetOrCreateProfileIdAsync</c>) plutôt qu'un tenant ambiant : ce module n'a
/// pas de notion d'entreprise active, son scope d'autorisation est simplement "cet utilisateur".
/// </summary>
public interface IGestionDuTempsService
{
    /// <summary>Crée les 6 catégories par défaut (reprises de mvp) au premier accès si l'utilisateur n'a encore aucun type de temps.</summary>
    Task<IReadOnlyList<TypeDeTempsView>> GetTypesDeTempsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Crée (id == null) ou modifie un type de temps. Si <paramref name="companyId"/> est renseigné, doit
    /// correspondre à une entreprise que l'utilisateur gère réellement (<c>UserCompanyLink</c>) — sinon
    /// <see cref="InvalidOperationException"/>.
    /// </summary>
    Task UpsertTypeDeTempsAsync(string userId, int? id, string cle, string libelle, TimeOnly heureDebut, TimeOnly heureFin, string recurrenceJours, int ordreAffichage, int? companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rappels de l'utilisateur. <paramref name="companyId"/> filtre sur une entreprise précise ;
    /// <paramref name="personnelUniquement"/> filtre sur les rappels sans entreprise ; les deux <c>false</c>/<c>null</c>
    /// retournent tout — le filtre est un confort d'affichage, jamais une restriction d'accès (toujours les
    /// rappels de l'utilisateur connecté, quel que soit le filtre).
    /// </summary>
    Task<IReadOnlyList<ActiviteView>> GetActivitesAsync(string userId, int? companyId, bool personnelUniquement, CancellationToken cancellationToken = default);

    Task<int> CreateActiviteAsync(string userId, int typeDeTempsId, string nom, DateOnly dateActivite, TimeOnly heureDebut, int dureeMinutes, int? companyId, CancellationToken cancellationToken = default);

    Task UpdateActiviteAsync(string userId, int activiteId, string nom, DateOnly dateActivite, TimeOnly heureDebut, int dureeMinutes, int? companyId, CancellationToken cancellationToken = default);

    Task DeleteActiviteAsync(string userId, int activiteId, CancellationToken cancellationToken = default);

    Task SetActiviteStatutAsync(string userId, int activiteId, string statut, CancellationToken cancellationToken = default);
}
