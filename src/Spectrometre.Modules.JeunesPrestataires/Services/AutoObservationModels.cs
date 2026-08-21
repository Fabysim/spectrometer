namespace Spectrometre.Modules.JeunesPrestataires.Services;

public enum AutoObservationAccessMode
{
    None,
    Jeune,
    Coach,
}

public sealed record AutoObservationAnswerInput(string QuestionKey, string? TextValue, int? NumericValue);

public sealed record AutoObservationAnswerView(
    string QuestionKey,
    string? TextValue,
    int? NumericValue,
    DateTimeOffset? UpdatedAt);

public sealed record AutoObservationSectionProgressView(string SectionKey, DateTimeOffset SavedAt);

public sealed record AutoObservationPageView(
    AutoObservationAccessMode AccessMode,
    JeuneProfileView JeuneProfile,
    AutoObservationSyntheseDocument? Synthese,
    DateTimeOffset? SyntheseGenereeLe,
    IReadOnlyList<AutoObservationSectionProgressView> SectionProgress,
    bool OrientationAFaire);

public sealed record AutoObservationSectionView(
    AutoObservationAccessMode AccessMode,
    JeuneProfileView JeuneProfile,
    string SectionKey,
    IReadOnlyList<AutoObservationAnswerView> Answers,
    DateTimeOffset? SavedAt,
    bool CanEdit);
