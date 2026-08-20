namespace Spectrometre.Modules.JeunesPrestataires.Services;

public sealed record GuideEntrevuePeurNoteView(string PeurKey, string? NoteCoach);

public sealed record GuideEntrevueView(
    int? Id,
    int JeuneProfileId,
    string JeuneNom,
    string JeunePrenoms,
    string? Motivations,
    string? Freins,
    string? MissionsAdaptees,
    string? NotesConfidentielles,
    IReadOnlyList<GuideEntrevuePeurNoteView> Peurs,
    DateTimeOffset? UpdatedAt);

public sealed record GuideEntrevuePeurNoteInput(string PeurKey, string? NoteCoach);
