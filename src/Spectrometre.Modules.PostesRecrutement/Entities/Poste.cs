namespace Spectrometre.Modules.PostesRecrutement.Entities;

public enum PosteStatut
{
    Ouvert = 0,
    Ferme = 1,
}

/// <summary>
/// Un poste à pourvoir, rattaché à l'entreprise active (schéma tenant — voir <c>ITenantScopedDbContext</c>,
/// même principe que Profil Entreprise). Périmètre volontairement minimal pour ce cycle : pas encore de
/// lien avec les compétences/exigences structurées de Profil Entreprise, ni de grille d'exigences dédiée.
/// </summary>
public sealed class Poste
{
    public int Id { get; set; }
    public required string Titre { get; set; }
    public string? Description { get; set; }

    /// <summary>Texte libre pour ce cycle — un référentiel de départements viendra avec un module RH plus complet.</summary>
    public string? Departement { get; set; }

    public PosteStatut Statut { get; set; } = PosteStatut.Ouvert;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
