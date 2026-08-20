namespace Spectrometre.Modules.JeunesPrestataires.Entities;

/// <summary>
/// Guide d'entrevue coach — un enregistrement mutable par jeune (même schéma que <see cref="ConsentementParental"/>).
/// Contenu 100 % confidentiel : jamais exposé côté jeune.
/// </summary>
public sealed class GuideEntrevue
{
    public int Id { get; set; }
    public int JeuneProfileId { get; set; }

    public string? Motivations { get; set; }
    public string? Freins { get; set; }
    public string? MissionsAdaptees { get; set; }
    public string? NotesConfidentielles { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<GuideEntrevuePeurNote> PeurNotes { get; set; } = [];
}
