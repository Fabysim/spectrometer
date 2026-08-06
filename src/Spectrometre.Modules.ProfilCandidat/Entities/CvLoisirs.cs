namespace Spectrometre.Modules.ProfilCandidat.Entities;

/// <summary>Section 6 du formulaire de CV (« Loisirs et centres d'intérêt ») — une par candidat.</summary>
public sealed class CvLoisirs
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }

    public string? LoisirsPreferes { get; set; }
    public string? ActivitesSportivesCulturelles { get; set; }
    public string? EngagementsAssociatifs { get; set; }
    public string? AutresCentresInteret { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
