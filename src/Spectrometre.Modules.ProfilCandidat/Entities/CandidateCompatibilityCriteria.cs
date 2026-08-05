namespace Spectrometre.Modules.ProfilCandidat.Entities;

/// <summary>
/// Grille complémentaire de compatibilité candidat (section H du document source). Critères structurés
/// et comparables (tags choisis dans <c>Spectrometre.Core.Compatibility.CompatibilityVocabulary</c>,
/// partagé avec le profil entreprise, + une échelle 1-5 pour le rythme) : voir l'audit qui a motivé
/// l'abandon du texte libre pour le scoring — deux personnes décrivant la même réalité avec des mots
/// différents obtenaient un recouvrement lexical quasi nul. Les champs <c>*Notes</c> (anciennement
/// <c>*Text</c>) restent en texte libre pour la nuance humaine, mais ne sont plus utilisés dans le calcul.
/// </summary>
public sealed class CandidateCompatibilityCriteria
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }

    public List<string> TechniqueTags { get; set; } = [];
    public List<string> ComportementaleTags { get; set; } = [];
    public List<string> CulturelleTags { get; set; } = [];

    /// <summary>Rythme de travail toléré par le candidat, 1 (calme) à 5 (très intense). Voir <c>CompatibilityVocabulary.RythmeTravailLabels</c>.</summary>
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
