namespace Spectrometre.Modules.Missions.Entities;

public sealed class Mission
{
    public int Id { get; set; }
    public int ParticulierProfileId { get; set; }
    public required string Titre { get; set; }
    public required string Description { get; set; }
    public string? Lieu { get; set; }
    /// <summary>Durée estimée (texte libre — ex. « 2 h », « une demi-journée »).</summary>
    public string? DureeEstimee { get; set; }
    public MissionDifficulte Difficulte { get; set; }
    public decimal? RemunerationMontant { get; set; }
    public string? CompetencesTravaillees { get; set; }
    public MissionStatut Statut { get; set; } = MissionStatut.Disponible;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<MissionAcceptation> Acceptations { get; set; } = [];
}
