namespace Spectrometre.Modules.ProfilCandidat.Entities;

/// <summary>
/// Les 6 thèmes du questionnaire « Connaissance de soi et de son potentiel » (sections A à F du document
/// source). La section G (« Synthèse personnelle ») n'est volontairement pas reprise comme thème
/// interrogeable : son contenu sert de guide à la synthèse auto-générée plutôt qu'à un 7e thème
/// de questions — voir l'écart documenté dans le résumé de livraison.
/// </summary>
public enum CandidateTheme
{
    ActivitesInterets = 0,
    TalentsNaturels = 1,
    CompetencesAcquises = 2,
    ValeursTravail = 3,
    EnvironnementsFavorables = 4,
    SignauxAlerte = 5,
}

public static class CandidateThemeLabels
{
    public static string Label(CandidateTheme theme) => theme switch
    {
        CandidateTheme.ActivitesInterets => "A. Activités qui suscitent l'intérêt et l'énergie",
        CandidateTheme.TalentsNaturels => "B. Talents naturels et aptitudes personnelles",
        CandidateTheme.CompetencesAcquises => "C. Compétences acquises par les études, la pratique et la vie quotidienne",
        CandidateTheme.ValeursTravail => "D. Valeurs personnelles et motivations profondes",
        CandidateTheme.EnvironnementsFavorables => "E. Environnements et contextes de travail favorables",
        CandidateTheme.SignauxAlerte => "F. Situations qui stimulent ou diminuent la motivation ou créent un malaise",
        _ => theme.ToString(),
    };

    public static string ShortLabel(CandidateTheme theme) => theme switch
    {
        CandidateTheme.ActivitesInterets => "Activités & intérêts",
        CandidateTheme.TalentsNaturels => "Talents naturels",
        CandidateTheme.CompetencesAcquises => "Compétences acquises",
        CandidateTheme.ValeursTravail => "Valeurs au travail",
        CandidateTheme.EnvironnementsFavorables => "Environnement favorable",
        CandidateTheme.SignauxAlerte => "Signaux d'alerte",
        _ => theme.ToString(),
    };
}
