namespace Spectrometre.Modules.JeunesPrestataires.Catalog;

public sealed record AutoObservationQuestionDef(
    string Key,
    string Label,
    AutoObservationFieldType FieldType,
    IReadOnlyList<string>? Options = null,
    string? AutreKey = null);

public sealed record AutoObservationSectionDef(
    string Key,
    int PartNumber,
    int SectionNumber,
    string Title,
    string? Intro,
    IReadOnlyList<AutoObservationQuestionDef> Questions,
    bool CoachCanEditAnswers = false,
    bool JeuneCanEditAnswers = true,
    bool IsSynthesisDisplayOnly = false);

public static partial class AutoObservationCatalog
{
    private static IReadOnlyList<AutoObservationSectionDef>? _allSections;

    public static IReadOnlyList<AutoObservationSectionDef> AllSections =>
        _allSections ??= Part1Sections.Concat(Part2Sections).ToList();

    public static AutoObservationSectionDef? TryGetSection(string sectionKey) =>
        AllSections.FirstOrDefault(s => s.Key == sectionKey);

    public static AutoObservationQuestionDef? TryGetQuestion(string questionKey) =>
        AllSections.SelectMany(s => s.Questions).FirstOrDefault(q => q.Key == questionKey);
}
