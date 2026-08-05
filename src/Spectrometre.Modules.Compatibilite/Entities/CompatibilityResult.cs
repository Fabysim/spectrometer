namespace Spectrometre.Modules.Compatibilite.Entities;

/// <summary>
/// Résultat d'un calcul de compatibilité entre un candidat et l'entreprise active. Stocké dans le
/// schéma de l'entreprise (un résultat de compatibilité est une donnée de recrutement de cette
/// entreprise) — <see cref="CandidateProfileId"/> est une simple référence par identifiant vers le
/// module Profil Candidat, jamais une contrainte de clé étrangère inter-schéma.
/// </summary>
public sealed class CompatibilityResult
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }
    public int CompanyProfileId { get; set; }

    public int ScoreTechnique { get; set; }
    public int ScoreComportementale { get; set; }
    public int ScoreCulturelle { get; set; }
    public int ScoreOrganisationnelle { get; set; }
    public int ScoreMotivationnelle { get; set; }
    public int ScoreGlobal { get; set; }

    public DateTimeOffset CalculatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<CompatibilityVigilancePoint> VigilancePoints { get; set; } = new List<CompatibilityVigilancePoint>();
}

public sealed class CompatibilityVigilancePoint
{
    public int Id { get; set; }
    public int CompatibilityResultId { get; set; }
    public required string Text { get; set; }
}
