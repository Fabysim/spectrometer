namespace Spectrometre.Modules.ProfilEntreprise.Entities;

/// <summary>Sections A à J du « Questionnaire de profil socioprofessionnel de l'entreprise ». La section K (grille de compatibilité) est modélisée séparément (<see cref="CompanyCompatibilityCriteria"/>).</summary>
public enum CompanyTheme
{
    Identification = 0,
    MissionVision = 1,
    Valeurs = 2,
    CultureClimat = 3,
    Leadership = 4,
    ModeRelationnel = 5,
    Reconnaissance = 6,
    ConditionsTravail = 7,
    ProfilCollaborateurs = 8,
    Synthese = 9,
}

public static class CompanyThemeLabels
{
    public static string Label(CompanyTheme theme) => theme switch
    {
        CompanyTheme.Identification => "A. Identification générale de l'entreprise",
        CompanyTheme.MissionVision => "B. Mission, vision et utilité sociale de l'entreprise",
        CompanyTheme.Valeurs => "C. Valeurs déclarées et valeurs réellement pratiquées",
        CompanyTheme.CultureClimat => "D. Culture de travail et climat organisationnel",
        CompanyTheme.Leadership => "E. Mode de leadership et style de gestion",
        CompanyTheme.ModeRelationnel => "F. Mode relationnel et communication interne",
        CompanyTheme.Reconnaissance => "G. Reconnaissance, motivation et évolution professionnelle",
        CompanyTheme.ConditionsTravail => "H. Conditions de travail et exigences du poste",
        CompanyTheme.ProfilCollaborateurs => "I. Profil des collaborateurs qui s'épanouissent dans l'entreprise",
        CompanyTheme.Synthese => "J. Synthèse du profil socioprofessionnel de l'entreprise",
        _ => theme.ToString(),
    };

    public static string TabLabel(CompanyTheme theme) => theme switch
    {
        CompanyTheme.Identification => "A. Identité",
        CompanyTheme.MissionVision => "B. Mission",
        CompanyTheme.Valeurs => "C. Valeurs",
        CompanyTheme.CultureClimat => "D. Culture",
        CompanyTheme.Leadership => "E. Leadership",
        CompanyTheme.ModeRelationnel => "F. Relationnel",
        CompanyTheme.Reconnaissance => "G. Reconnaissance",
        CompanyTheme.ConditionsTravail => "H. Conditions",
        CompanyTheme.ProfilCollaborateurs => "I. Profil recherché",
        CompanyTheme.Synthese => "J. Synthèse",
        _ => theme.ToString(),
    };

    /// <summary>Traduction automatique pour l'instant (bilinguisme, cycle contenu métier) — à affiner plus tard.</summary>
    public static string LabelEn(CompanyTheme theme) => theme switch
    {
        CompanyTheme.Identification => "A. General identification of the company",
        CompanyTheme.MissionVision => "B. Mission, vision and social purpose of the company",
        CompanyTheme.Valeurs => "C. Stated values and values actually practiced",
        CompanyTheme.CultureClimat => "D. Work culture and organizational climate",
        CompanyTheme.Leadership => "E. Leadership style and management approach",
        CompanyTheme.ModeRelationnel => "F. Relational style and internal communication",
        CompanyTheme.Reconnaissance => "G. Recognition, motivation and career progression",
        CompanyTheme.ConditionsTravail => "H. Working conditions and job requirements",
        CompanyTheme.ProfilCollaborateurs => "I. Profile of employees who thrive at the company",
        CompanyTheme.Synthese => "J. Synthesis of the company's socio-professional profile",
        _ => theme.ToString(),
    };

    /// <summary>Traduction automatique pour l'instant (bilinguisme, cycle contenu métier) — à affiner plus tard.</summary>
    public static string TabLabelEn(CompanyTheme theme) => theme switch
    {
        CompanyTheme.Identification => "A. Identity",
        CompanyTheme.MissionVision => "B. Mission",
        CompanyTheme.Valeurs => "C. Values",
        CompanyTheme.CultureClimat => "D. Culture",
        CompanyTheme.Leadership => "E. Leadership",
        CompanyTheme.ModeRelationnel => "F. Relational",
        CompanyTheme.Reconnaissance => "G. Recognition",
        CompanyTheme.ConditionsTravail => "H. Conditions",
        CompanyTheme.ProfilCollaborateurs => "I. Desired profile",
        CompanyTheme.Synthese => "J. Synthesis",
        _ => theme.ToString(),
    };

    public static string Label(CompanyTheme theme, bool english) => english ? LabelEn(theme) : Label(theme);
    public static string TabLabel(CompanyTheme theme, bool english) => english ? TabLabelEn(theme) : TabLabel(theme);
}
