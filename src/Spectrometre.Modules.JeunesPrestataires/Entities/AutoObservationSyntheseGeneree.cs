namespace Spectrometre.Modules.JeunesPrestataires.Entities;

/// <summary>
/// Synthèse section 13 générée par règles (pas d'IA v1) — régénérable à la demande.
/// </summary>
public sealed class AutoObservationSyntheseGeneree
{
    public int Id { get; set; }

    public int JeuneProfileId { get; set; }

    public required string Contenu { get; set; }

    public DateTimeOffset GenereeLe { get; set; } = DateTimeOffset.UtcNow;
}
