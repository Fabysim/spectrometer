namespace Spectrometre.Modules.Missions.Entities;

public sealed class ParticulierProfile
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public required string Nom { get; set; }
    public required string Prenoms { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
