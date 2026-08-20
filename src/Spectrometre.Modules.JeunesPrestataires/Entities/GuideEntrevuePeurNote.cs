namespace Spectrometre.Modules.JeunesPrestataires.Entities;

/// <summary>
/// Note coach sur une ligne de la grille des peurs (<see cref="PeurKey"/> = clé catalogue fixe).
/// </summary>
public sealed class GuideEntrevuePeurNote
{
    public int Id { get; set; }
    public int GuideEntrevueId { get; set; }
    public GuideEntrevue GuideEntrevue { get; set; } = null!;

    public required string PeurKey { get; set; }
    public string? NoteCoach { get; set; }
}
