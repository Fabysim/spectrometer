namespace Spectrometre.Modules.ProfilCandidat.Entities;

/// <summary>Section 7 du formulaire de CV (« Références professionnelles ») — une ligne par référence, plusieurs par candidat.</summary>
public sealed class CvReference
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }

    public string? NomPrenom { get; set; }
    public string? Fonction { get; set; }
    public string? EntrepriseOrganisation { get; set; }
    public string? TelephoneOuEmail { get; set; }
    public string? LienAvecPostulant { get; set; }

    public int DisplayOrder { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
