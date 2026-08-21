using Spectrometre.Modules.JeunesPrestataires.Entities;

namespace Spectrometre.Modules.JeunesPrestataires.Catalog;

public sealed record AutoObservationPartieNav(int PartNumber, IReadOnlyList<AutoObservationSectionDef> Sections);

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

    /// <summary>
    /// Ordre canonique du catalogue (Part0 → Part1 → Part2). Conservé pour la lookup
    /// et les tests de contenu — la navigation UI utilise <see cref="GetSectionsOrdonnees"/>.
    /// </summary>
    public static IReadOnlyList<AutoObservationSectionDef> AllSections =>
        _allSections ??= Part0Sections.Concat(Part1Sections).Concat(Part2Sections).ToList();

    /// <summary>
    /// Ordre d'affichage selon le profil d'accompagnement (sans dupliquer les définitions) :
    /// <see cref="ProfilAccompagnement.SansExperience"/> → Part2, Part1, Part0 (opérationnel d'abord) ;
    /// <see cref="ProfilAccompagnement.Autonome"/> → Part0, Part2, Part1 (orientation d'abord).
    /// </summary>
    public static IReadOnlyList<AutoObservationSectionDef> GetSectionsOrdonnees(ProfilAccompagnement profil) =>
        GetPartiesOrdonnees(profil).SelectMany(p => p.Sections).ToList();

    public static IReadOnlyList<AutoObservationPartieNav> GetPartiesOrdonnees(ProfilAccompagnement profil) =>
        profil == ProfilAccompagnement.Autonome
            ? [new(0, Part0Sections), new(2, Part2Sections), new(1, Part1Sections)]
            : [new(2, Part2Sections), new(1, Part1Sections), new(0, Part0Sections)];

    public static AutoObservationSectionDef? TryGetSection(string sectionKey) =>
        AllSections.FirstOrDefault(s => s.Key == sectionKey);

    public static AutoObservationQuestionDef? TryGetQuestion(string questionKey) =>
        AllSections.SelectMany(s => s.Questions).FirstOrDefault(q => q.Key == questionKey);
}
