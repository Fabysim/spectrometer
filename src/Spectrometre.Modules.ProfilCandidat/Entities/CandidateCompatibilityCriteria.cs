namespace Spectrometre.Modules.ProfilCandidat.Entities;

/// <summary>
/// Grille complémentaire de compatibilité candidat (section H du document source) : une réponse libre
/// par axe, exploitée par le Moteur de Compatibilité via <c>ICandidateProfileService</c> — jamais lue
/// directement par le module Compatibilité.
/// </summary>
public sealed class CandidateCompatibilityCriteria
{
    public int Id { get; set; }
    public int CandidateProfileId { get; set; }

    public string? TechniqueText { get; set; }
    public string? ComportementaleText { get; set; }
    public string? CulturelleText { get; set; }
    public string? OrganisationnelleText { get; set; }
    public string? MotivationnelleText { get; set; }
    public string? PointsVigilanceText { get; set; }

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
