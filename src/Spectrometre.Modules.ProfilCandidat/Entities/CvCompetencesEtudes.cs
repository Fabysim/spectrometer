namespace Spectrometre.Modules.ProfilCandidat.Entities;

/// <summary>Section 3 du formulaire de CV (« Spécialités et compétences acquises par les études ») — une par candidat.</summary>
public sealed class CvCompetencesEtudes
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }

    public string? SpecialitePrincipale { get; set; }
    public string? CompetencesTechniques { get; set; }
    public string? ConnaissancesTheoriques { get; set; }
    public string? LanguesMaitrisees { get; set; }
    public string? OutilsLogicielsMethodes { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
