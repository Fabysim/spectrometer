namespace Spectrometre.Modules.Missions.Entities;

public enum MissionAcceptationStatut
{
    EnAttenteValidationCoach = 0,
    ValideeParCoach = 1,
    RefuseeParCoach = 2,
    /// <summary>
    /// Le jeune retire sa candidature avant décision du coach. Distinct de
    /// <see cref="RefuseeParCoach"/> (même effet : mission redevient <c>Disponible</c>).
    /// Valeur 3 pour ne pas réordonner les statuts déjà persistés (0–2) — pas de migration.
    /// </summary>
    RetireeParJeune = 3,
}

/// <summary>
/// <see cref="JeuneProfileId"/> est une clé logique vers <c>jeunes_prestataires.JeuneProfiles</c> — pas de FK EF cross-DbContext.
/// </summary>
public sealed class MissionAcceptation
{
    public int Id { get; set; }
    public int MissionId { get; set; }
    public Mission Mission { get; set; } = null!;
    public int JeuneProfileId { get; set; }
    public DateTimeOffset AccepteeLe { get; set; }
    public MissionAcceptationStatut Statut { get; set; }
    public string? CoachUserId { get; set; }
    public DateTimeOffset? DecideeLe { get; set; }
}
