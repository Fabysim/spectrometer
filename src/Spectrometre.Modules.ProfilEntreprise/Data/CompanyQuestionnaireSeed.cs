using Spectrometre.Modules.ProfilEntreprise.Entities;

namespace Spectrometre.Modules.ProfilEntreprise.Data;

/// <summary>
/// Catalogue des 55 questions des sections A à J, texte repris tel quel du document
/// « Questionnaire de profil socioprofessionnel de l'entreprise ». La section K est gérée séparément
/// (<see cref="CompanyCompatibilityCriteria"/>), elle n'est pas une liste de questions numérotées.
/// </summary>
/// <remarks>
/// <c>TextEn</c> (bilinguisme, cycle contenu métier) : traduction automatique pour l'instant, à affiner
/// par une relecture humaine plus tard.
/// </remarks>
internal static class CompanyQuestionnaireSeed
{
    public sealed record SeedQuestion(int Number, CompanyTheme Theme, string Text, string TextEn);

    public static readonly SeedQuestion[] Questions =
    [
        // A. Identification générale de l'entreprise
        new(1, CompanyTheme.Identification, "Nom de l'entreprise ou de l'organisation", "Company or organization name"),
        new(2, CompanyTheme.Identification, "Secteur d'activité principal", "Main sector of activity"),
        new(3, CompanyTheme.Identification, "Localisation principale et zones d'intervention", "Main location and areas of operation"),
        new(4, CompanyTheme.Identification, "Taille de l'entreprise : petite, moyenne, grande, groupe, association, institution", "Company size: small, medium, large, group, association, institution"),
        new(5, CompanyTheme.Identification, "Ancienneté ou année de création", "Years in operation or founding year"),
        new(6, CompanyTheme.Identification, "Types de postes généralement proposés", "Types of positions generally offered"),

        // B. Mission, vision et utilité sociale de l'entreprise
        new(7, CompanyTheme.MissionVision, "Quelle est la mission principale de l'entreprise ?", "What is the company's main mission?"),
        new(8, CompanyTheme.MissionVision, "À quel besoin de la société, du marché ou de la communauté l'entreprise cherche-t-elle à répondre ?", "What need of society, the market or the community is the company trying to meet?"),
        new(9, CompanyTheme.MissionVision, "Quelle vision l'entreprise poursuit-elle à moyen ou long terme ?", "What vision is the company pursuing in the medium or long term?"),
        new(10, CompanyTheme.MissionVision, "Qu'est-ce qui rend l'entreprise utile, différente ou importante dans son secteur ?", "What makes the company useful, different or important in its sector?"),
        new(11, CompanyTheme.MissionVision, "Quels problèmes l'entreprise souhaite-t-elle contribuer à résoudre ?", "What problems does the company want to help solve?"),

        // C. Valeurs déclarées et valeurs réellement pratiquées
        new(12, CompanyTheme.Valeurs, "Quelles sont les trois à cinq valeurs principales que l'entreprise souhaite incarner ?", "What are the three to five main values the company wants to embody?"),
        new(13, CompanyTheme.Valeurs, "Comment ces valeurs se traduisent-elles dans les décisions quotidiennes ?", "How do these values translate into everyday decisions?"),
        new(14, CompanyTheme.Valeurs, "Quelles valeurs sont attendues chez les collaborateurs ?", "What values are expected of employees?"),
        new(15, CompanyTheme.Valeurs, "Quels comportements sont encouragés parce qu'ils correspondent à la culture de l'entreprise ?", "What behaviors are encouraged because they match the company's culture?"),
        new(16, CompanyTheme.Valeurs, "Quels comportements sont incompatibles avec l'esprit de l'entreprise ?", "What behaviors are incompatible with the company's spirit?"),

        // D. Culture de travail et climat organisationnel
        new(17, CompanyTheme.CultureClimat, "Comment décririez-vous le climat général de travail : calme, dynamique, exigeant, familial, compétitif, créatif, structuré, flexible ?", "How would you describe the overall work climate: calm, dynamic, demanding, family-like, competitive, creative, structured, flexible?"),
        new(18, CompanyTheme.CultureClimat, "Le travail est-il plutôt individuel, collectif ou mixte ?", "Is the work mostly individual, collective or a mix of both?"),
        new(19, CompanyTheme.CultureClimat, "Quelle place occupent les règles, procédures et consignes dans l'organisation du travail ?", "What role do rules, procedures and instructions play in how work is organized?"),
        new(20, CompanyTheme.CultureClimat, "Quelle place est laissée à l'initiative, à l'autonomie et à la créativité ?", "How much room is given to initiative, autonomy and creativity?"),
        new(21, CompanyTheme.CultureClimat, "Comment l'entreprise gère-t-elle les périodes de pression, d'urgence ou de forte activité ?", "How does the company handle periods of pressure, urgency or high activity?"),

        // E. Mode de leadership et style de gestion
        new(22, CompanyTheme.Leadership, "Quel style de leadership domine dans l'entreprise : directif, participatif, collaboratif, transformationnel, paternaliste, délégatif, orienté résultats ?", "What leadership style dominates in the company: directive, participative, collaborative, transformational, paternalistic, delegative, results-oriented?"),
        new(23, CompanyTheme.Leadership, "Comment les responsables donnent-ils les consignes et suivent-ils le travail ?", "How do managers give instructions and follow up on work?"),
        new(24, CompanyTheme.Leadership, "Quelle place les employés ont-ils dans la prise de décision ?", "What role do employees have in decision-making?"),
        new(25, CompanyTheme.Leadership, "Comment les responsables accompagnent-ils les nouveaux collaborateurs ?", "How do managers support new employees?"),
        new(26, CompanyTheme.Leadership, "Comment les erreurs sont-elles traitées : sanction, apprentissage, correction, accompagnement, discussion ?", "How are mistakes handled: sanction, learning, correction, support, discussion?"),
        new(27, CompanyTheme.Leadership, "Quel type de collaborateur réussit le mieux sous ce mode de leadership ?", "What type of employee thrives best under this leadership style?"),

        // F. Mode relationnel et communication interne
        new(28, CompanyTheme.ModeRelationnel, "Comment les collaborateurs communiquent-ils entre eux : oralement, par écrit, en réunions, par messagerie, par rapports ?", "How do employees communicate with each other: verbally, in writing, in meetings, by messaging, through reports?"),
        new(29, CompanyTheme.ModeRelationnel, "Le climat relationnel est-il plutôt formel, informel, hiérarchique, familial, direct, diplomatique ou réservé ?", "Is the relational climate mostly formal, informal, hierarchical, family-like, direct, diplomatic or reserved?"),
        new(30, CompanyTheme.ModeRelationnel, "Comment les désaccords ou conflits sont-ils généralement gérés ?", "How are disagreements or conflicts usually handled?"),
        new(31, CompanyTheme.ModeRelationnel, "Quelle importance l'entreprise accorde-t-elle à l'écoute, au respect, à la politesse et à la coopération ?", "How much importance does the company place on listening, respect, politeness and cooperation?"),
        new(32, CompanyTheme.ModeRelationnel, "Quels comportements relationnels sont particulièrement appréciés dans l'entreprise ?", "What relational behaviors are particularly valued in the company?"),
        new(33, CompanyTheme.ModeRelationnel, "Quels comportements relationnels créent des difficultés dans l'entreprise ?", "What relational behaviors create difficulties in the company?"),

        // G. Reconnaissance, motivation et évolution professionnelle
        new(34, CompanyTheme.Reconnaissance, "Comment l'entreprise reconnaît-elle les efforts et les bons résultats ?", "How does the company recognize effort and good results?"),
        new(35, CompanyTheme.Reconnaissance, "Quels types de motivation sont les plus présents : salaire, responsabilité, reconnaissance, progression, stabilité, autonomie, esprit d'équipe ?", "What types of motivation are most present: salary, responsibility, recognition, progression, stability, autonomy, team spirit?"),
        new(36, CompanyTheme.Reconnaissance, "Quelles possibilités d'apprentissage, de formation ou d'évolution sont proposées ?", "What learning, training or advancement opportunities are offered?"),
        new(37, CompanyTheme.Reconnaissance, "Comment l'entreprise accompagne-t-elle les personnes qui veulent progresser ?", "How does the company support people who want to advance?"),
        new(38, CompanyTheme.Reconnaissance, "Quels signes montrent qu'un collaborateur est bien intégré et apprécié dans l'entreprise ?", "What signs show that an employee is well integrated and valued in the company?"),

        // H. Conditions de travail et exigences du poste
        new(39, CompanyTheme.ConditionsTravail, "Quels sont les horaires habituels et le rythme de travail ?", "What are the usual hours and work pace?"),
        new(40, CompanyTheme.ConditionsTravail, "Le travail exige-t-il des déplacements, de la mobilité, une disponibilité particulière ou des horaires variables ?", "Does the job require travel, mobility, special availability or variable hours?"),
        new(41, CompanyTheme.ConditionsTravail, "Quelles sont les principales contraintes physiques, psychologiques, relationnelles ou organisationnelles ?", "What are the main physical, psychological, relational or organizational constraints?"),
        new(42, CompanyTheme.ConditionsTravail, "Quel niveau d'autonomie est attendu du collaborateur ?", "What level of autonomy is expected of the employee?"),
        new(43, CompanyTheme.ConditionsTravail, "Quel niveau de pression, de rapidité ou de précision le travail demande-t-il ?", "What level of pressure, speed or precision does the job require?"),
        new(44, CompanyTheme.ConditionsTravail, "Quels moyens l'entreprise met-elle à disposition pour permettre de bien travailler ?", "What resources does the company provide to enable good work?"),

        // I. Profil des collaborateurs qui s'épanouissent dans l'entreprise
        new(45, CompanyTheme.ProfilCollaborateurs, "Quelles qualités personnelles permettent de bien réussir dans l'entreprise ?", "What personal qualities help someone succeed in the company?"),
        new(46, CompanyTheme.ProfilCollaborateurs, "Quelles compétences techniques ou professionnelles sont les plus recherchées ?", "What technical or professional skills are most sought after?"),
        new(47, CompanyTheme.ProfilCollaborateurs, "Quel type de personnalité s'intègre facilement dans l'équipe ?", "What type of personality fits easily into the team?"),
        new(48, CompanyTheme.ProfilCollaborateurs, "Quel type de candidat pourrait rencontrer des difficultés dans cet environnement ?", "What type of candidate might struggle in this environment?"),
        new(49, CompanyTheme.ProfilCollaborateurs, "Quelles valeurs personnelles du candidat doivent être compatibles avec celles de l'entreprise ?", "What personal values of the candidate must be compatible with the company's?"),

        // J. Synthèse du profil socioprofessionnel de l'entreprise
        new(50, CompanyTheme.Synthese, "Notre entreprise est principalement caractérisée par...", "Our company is mainly characterized by..."),
        new(51, CompanyTheme.Synthese, "Nos trois valeurs les plus importantes sont...", "Our three most important values are..."),
        new(52, CompanyTheme.Synthese, "Notre style de leadership est plutôt...", "Our leadership style is more..."),
        new(53, CompanyTheme.Synthese, "Notre mode relationnel est plutôt...", "Our relational style is more..."),
        new(54, CompanyTheme.Synthese, "Les collaborateurs qui réussissent le mieux chez nous sont ceux qui...", "The employees who succeed best here are those who..."),
        new(55, CompanyTheme.Synthese, "Les candidats doivent être particulièrement attentifs à...", "Candidates should pay particular attention to..."),
    ];
}
