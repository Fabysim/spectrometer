namespace Spectrometre.Modules.JeunesPrestataires.Entities;

/// <summary>
/// Réponse structurée à une question du questionnaire d'auto-observation — une ligne par
/// (<see cref="JeuneProfileId"/>, <see cref="QuestionKey"/>).
/// </summary>
public sealed class AutoObservationReponse
{
    public int Id { get; set; }

    public int JeuneProfileId { get; set; }

    /// <summary>Clé stable définie dans <see cref="Catalog.AutoObservationCatalog"/>.</summary>
    public required string QuestionKey { get; set; }

    /// <summary>Texte libre, choix unique, ou valeurs cases à cocher (séparateur <c>|</c>).</summary>
    public string? TextValue { get; set; }

    /// <summary>Échelles 1 à 4 ou 1 à 5.</summary>
    public int? NumericValue { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
