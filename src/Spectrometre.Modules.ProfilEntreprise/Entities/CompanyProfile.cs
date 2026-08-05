namespace Spectrometre.Modules.ProfilEntreprise.Entities;

/// <summary>
/// Profil d'entreprise. Une seule ligne par schéma tenant : le schéma Postgres actif EST déjà
/// l'entreprise (voir <c>ITenantScopedDbContext</c>), donc pas de clé étrangère vers une table
/// « Company » ici — celle-ci vit dans le schéma partagé du noyau.
/// </summary>
public sealed class CompanyProfile
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CompanyQuestion
{
    public int Id { get; set; }
    public CompanyTheme Theme { get; set; }
    public int Number { get; set; }
    public required string Text { get; set; }
}

public sealed class CompanyAnswer
{
    public int Id { get; set; }
    public int CompanyProfileId { get; set; }
    public int QuestionId { get; set; }
    public string? AnswerText { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Grille complémentaire des critères pour le moteur de compatibilité (section K du document source).
/// Critères structurés (tags du vocabulaire partagé + échelle 1-5), voir le commentaire détaillé sur
/// <c>CandidateCompatibilityCriteria</c> (module Profil Candidat) pour la justification du changement.
/// </summary>
public sealed class CompanyCompatibilityCriteria
{
    public int Id { get; set; }
    public int CompanyProfileId { get; set; }

    public List<string> TechniqueTags { get; set; } = [];
    public List<string> ComportementaleTags { get; set; } = [];
    public List<string> CulturelleTags { get; set; } = [];

    /// <summary>Rythme de travail exigé par le poste, 1 (calme) à 5 (très intense). Voir <c>CompatibilityVocabulary.RythmeTravailLabels</c>.</summary>
    public int? RythmeTravail { get; set; }

    public List<string> MotivationnelleTags { get; set; } = [];
    public List<string> PointsVigilanceTags { get; set; } = [];

    public string? TechniqueNotes { get; set; }
    public string? ComportementaleNotes { get; set; }
    public string? CulturelleNotes { get; set; }
    public string? OrganisationnelleNotes { get; set; }
    public string? MotivationnelleNotes { get; set; }
    public string? PointsVigilanceNotes { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
