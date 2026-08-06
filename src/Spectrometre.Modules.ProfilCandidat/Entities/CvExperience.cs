namespace Spectrometre.Modules.ProfilCandidat.Entities;

/// <summary>Section 4 du formulaire de CV (« Compétences acquises par les expériences pratiques ») — une ligne par expérience, plusieurs par candidat.</summary>
public sealed class CvExperience
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }

    public string? Periode { get; set; }
    public string? EntrepriseOrganisationOuStage { get; set; }
    public string? FonctionOuActiviteExercee { get; set; }
    public string? CompetencesDeveloppees { get; set; }

    public int DisplayOrder { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
