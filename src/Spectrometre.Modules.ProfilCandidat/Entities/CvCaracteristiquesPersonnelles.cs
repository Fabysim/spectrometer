namespace Spectrometre.Modules.ProfilCandidat.Entities;

/// <summary>Section 5 du formulaire de CV (« Caractéristiques personnelles ») — une par candidat.</summary>
public sealed class CvCaracteristiquesPersonnelles
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }

    public string? QualitesPersonnelles { get; set; }
    public string? AptitudesProfessionnelles { get; set; }
    public string? AttitudesRelationnelles { get; set; }
    public string? CapaciteSousPression { get; set; }
    public string? DisponibiliteMobilite { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
