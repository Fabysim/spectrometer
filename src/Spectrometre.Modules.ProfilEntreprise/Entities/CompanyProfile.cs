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

/// <summary>Grille complémentaire des critères pour le moteur de compatibilité (section K du document source).</summary>
public sealed class CompanyCompatibilityCriteria
{
    public int Id { get; set; }
    public int CompanyProfileId { get; set; }

    public string? TechniqueText { get; set; }
    public string? ComportementaleText { get; set; }
    public string? CulturelleText { get; set; }
    public string? OrganisationnelleText { get; set; }
    public string? MotivationnelleText { get; set; }
    public string? PointsVigilanceText { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
