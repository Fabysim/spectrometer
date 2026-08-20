namespace Spectrometre.Modules.JeunesPrestataires.Entities;

/// <summary>Dernière sauvegarde brouillon par section — permet la reprise progressive.</summary>
public sealed class AutoObservationSectionProgress
{
    public int Id { get; set; }

    public int JeuneProfileId { get; set; }

    public required string SectionKey { get; set; }

    public DateTimeOffset SavedAt { get; set; } = DateTimeOffset.UtcNow;
}
