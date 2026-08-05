namespace Spectrometre.Modules.ProfilCandidat.Entities;

/// <summary>Catégorie de la synthèse auto-générée — reprend exactement les catégories citées dans le cahier des charges.</summary>
public enum SynthesisCategory
{
    Interet = 0,
    Talent = 1,
    Competence = 2,
    Valeur = 3,
    EnvironnementFavorable = 4,
    SignalAlerte = 5,
}

/// <summary>Un tag de la synthèse de profil (ex. « Créativité », « Autonomie »), régénéré à chaque appel de <c>GenerateSynthesisAsync</c>.</summary>
public sealed class CandidateSynthesisTag
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }
    public SynthesisCategory Category { get; set; }
    public required string Label { get; set; }
}
