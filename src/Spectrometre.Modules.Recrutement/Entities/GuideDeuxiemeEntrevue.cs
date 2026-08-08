namespace Spectrometre.Modules.Recrutement.Entities;

/// <summary>
/// Guide structuré de 2ème entrevue, rattaché au poste (un seul enregistrement par
/// <see cref="PosteId"/>) — même principe que <c>SecondInterviewGuide</c> du MVP après retrait
/// de CandidateId. Champs volontairement agrégés (texte libre) par rapport au MVP plus granulaire.
/// </summary>
public sealed class GuideDeuxiemeEntrevue
{
    public int Id { get; set; }
    public int PosteId { get; set; }

    public string? MissionLivrables { get; set; }
    public string? SituationQuantitative { get; set; }
    public string? SituationQualitative { get; set; }
    public string? Objectifs { get; set; }
    public string? Suivi { get; set; }
    public string? Echeances { get; set; }
    public string? AutoriteResponsabilite { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
