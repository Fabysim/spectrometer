namespace Spectrometre.Modules.JeunesPrestataires.Entities;

/// <summary>
/// Synthèse section 13 générée par règles (pas d'IA). <see cref="Contenu"/> est un JSON
/// <c>AutoObservationSyntheseDocument</c> (version 2). Les anciens textes markdown sont
/// régénérés à la lecture, pas une migration SQL.
/// </summary>
public sealed class AutoObservationSyntheseGeneree
{
    public int Id { get; set; }

    public int JeuneProfileId { get; set; }

    public required string Contenu { get; set; }

    public DateTimeOffset GenereeLe { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Confirmation coach que les tableaux 1/2A/2B auto-générés ont été relus. Pas une édition du
    /// texte (toujours dérivé des réponses). Complément / nuance = <see cref="PlanActionAutoObservation"/>
    /// et le guide d'entrevue. Effacé à chaque régénération.
    /// </summary>
    public DateTimeOffset? ValideeLe { get; set; }

    public string? ValideeParCoachUserId { get; set; }
}
