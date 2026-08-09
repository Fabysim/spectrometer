namespace Spectrometre.Core.Tenancy;

/// <summary>
/// Table de liaison utilisateur ↔ entreprises : un compte peut posséder ou gérer plusieurs entreprises
/// (schéma partagé <c>core</c>, contrairement aux données métier qui vivent dans le schéma de chaque entreprise).
/// </summary>
public sealed class UserCompanyLink
{
    public int Id { get; set; }

    public required string UserId { get; set; }

    public int CompanyId { get; set; }
    public Company? Company { get; set; }

    public CompanyRole Role { get; set; } = CompanyRole.Proprietaire;

    /// <summary>
    /// Poste actuellement occupé dans cette entreprise. Clé logique vers
    /// <c>ProfilEntreprise.Poste.Id</c> du schéma tenant de <see cref="CompanyId"/> — pas de FK EF
    /// cross-schéma (même pattern que Recrutement → PosteId).
    /// </summary>
    /// <remarks>
    /// Choix : colonne nullable ici plutôt qu'une table d'historique — le besoin actuel est
    /// l'affectation courante (un employé ↔ un poste à la fois), gérée depuis
    /// <c>/entreprise/employes</c> indépendamment de l'activation du module SuiviEmployes.
    /// Un historique de mutations pourra vivre plus tard dans le module SuiviEmployes si besoin.
    /// </remarks>
    public int? PosteId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
