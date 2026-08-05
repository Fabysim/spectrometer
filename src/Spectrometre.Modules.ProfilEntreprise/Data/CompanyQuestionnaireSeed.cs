using Spectrometre.Modules.ProfilEntreprise.Entities;

namespace Spectrometre.Modules.ProfilEntreprise.Data;

/// <summary>
/// Catalogue des 55 questions des sections A à J, texte repris tel quel du document
/// « Questionnaire de profil socioprofessionnel de l'entreprise ». La section K est gérée séparément
/// (<see cref="CompanyCompatibilityCriteria"/>), elle n'est pas une liste de questions numérotées.
/// </summary>
internal static class CompanyQuestionnaireSeed
{
    public sealed record SeedQuestion(int Number, CompanyTheme Theme, string Text);

    public static readonly SeedQuestion[] Questions =
    [
        // A. Identification générale de l'entreprise
        new(1, CompanyTheme.Identification, "Nom de l'entreprise ou de l'organisation"),
        new(2, CompanyTheme.Identification, "Secteur d'activité principal"),
        new(3, CompanyTheme.Identification, "Localisation principale et zones d'intervention"),
        new(4, CompanyTheme.Identification, "Taille de l'entreprise : petite, moyenne, grande, groupe, association, institution"),
        new(5, CompanyTheme.Identification, "Ancienneté ou année de création"),
        new(6, CompanyTheme.Identification, "Types de postes généralement proposés"),

        // B. Mission, vision et utilité sociale de l'entreprise
        new(7, CompanyTheme.MissionVision, "Quelle est la mission principale de l'entreprise ?"),
        new(8, CompanyTheme.MissionVision, "À quel besoin de la société, du marché ou de la communauté l'entreprise cherche-t-elle à répondre ?"),
        new(9, CompanyTheme.MissionVision, "Quelle vision l'entreprise poursuit-elle à moyen ou long terme ?"),
        new(10, CompanyTheme.MissionVision, "Qu'est-ce qui rend l'entreprise utile, différente ou importante dans son secteur ?"),
        new(11, CompanyTheme.MissionVision, "Quels problèmes l'entreprise souhaite-t-elle contribuer à résoudre ?"),

        // C. Valeurs déclarées et valeurs réellement pratiquées
        new(12, CompanyTheme.Valeurs, "Quelles sont les trois à cinq valeurs principales que l'entreprise souhaite incarner ?"),
        new(13, CompanyTheme.Valeurs, "Comment ces valeurs se traduisent-elles dans les décisions quotidiennes ?"),
        new(14, CompanyTheme.Valeurs, "Quelles valeurs sont attendues chez les collaborateurs ?"),
        new(15, CompanyTheme.Valeurs, "Quels comportements sont encouragés parce qu'ils correspondent à la culture de l'entreprise ?"),
        new(16, CompanyTheme.Valeurs, "Quels comportements sont incompatibles avec l'esprit de l'entreprise ?"),

        // D. Culture de travail et climat organisationnel
        new(17, CompanyTheme.CultureClimat, "Comment décririez-vous le climat général de travail : calme, dynamique, exigeant, familial, compétitif, créatif, structuré, flexible ?"),
        new(18, CompanyTheme.CultureClimat, "Le travail est-il plutôt individuel, collectif ou mixte ?"),
        new(19, CompanyTheme.CultureClimat, "Quelle place occupent les règles, procédures et consignes dans l'organisation du travail ?"),
        new(20, CompanyTheme.CultureClimat, "Quelle place est laissée à l'initiative, à l'autonomie et à la créativité ?"),
        new(21, CompanyTheme.CultureClimat, "Comment l'entreprise gère-t-elle les périodes de pression, d'urgence ou de forte activité ?"),

        // E. Mode de leadership et style de gestion
        new(22, CompanyTheme.Leadership, "Quel style de leadership domine dans l'entreprise : directif, participatif, collaboratif, transformationnel, paternaliste, délégatif, orienté résultats ?"),
        new(23, CompanyTheme.Leadership, "Comment les responsables donnent-ils les consignes et suivent-ils le travail ?"),
        new(24, CompanyTheme.Leadership, "Quelle place les employés ont-ils dans la prise de décision ?"),
        new(25, CompanyTheme.Leadership, "Comment les responsables accompagnent-ils les nouveaux collaborateurs ?"),
        new(26, CompanyTheme.Leadership, "Comment les erreurs sont-elles traitées : sanction, apprentissage, correction, accompagnement, discussion ?"),
        new(27, CompanyTheme.Leadership, "Quel type de collaborateur réussit le mieux sous ce mode de leadership ?"),

        // F. Mode relationnel et communication interne
        new(28, CompanyTheme.ModeRelationnel, "Comment les collaborateurs communiquent-ils entre eux : oralement, par écrit, en réunions, par messagerie, par rapports ?"),
        new(29, CompanyTheme.ModeRelationnel, "Le climat relationnel est-il plutôt formel, informel, hiérarchique, familial, direct, diplomatique ou réservé ?"),
        new(30, CompanyTheme.ModeRelationnel, "Comment les désaccords ou conflits sont-ils généralement gérés ?"),
        new(31, CompanyTheme.ModeRelationnel, "Quelle importance l'entreprise accorde-t-elle à l'écoute, au respect, à la politesse et à la coopération ?"),
        new(32, CompanyTheme.ModeRelationnel, "Quels comportements relationnels sont particulièrement appréciés dans l'entreprise ?"),
        new(33, CompanyTheme.ModeRelationnel, "Quels comportements relationnels créent des difficultés dans l'entreprise ?"),

        // G. Reconnaissance, motivation et évolution professionnelle
        new(34, CompanyTheme.Reconnaissance, "Comment l'entreprise reconnaît-elle les efforts et les bons résultats ?"),
        new(35, CompanyTheme.Reconnaissance, "Quels types de motivation sont les plus présents : salaire, responsabilité, reconnaissance, progression, stabilité, autonomie, esprit d'équipe ?"),
        new(36, CompanyTheme.Reconnaissance, "Quelles possibilités d'apprentissage, de formation ou d'évolution sont proposées ?"),
        new(37, CompanyTheme.Reconnaissance, "Comment l'entreprise accompagne-t-elle les personnes qui veulent progresser ?"),
        new(38, CompanyTheme.Reconnaissance, "Quels signes montrent qu'un collaborateur est bien intégré et apprécié dans l'entreprise ?"),

        // H. Conditions de travail et exigences du poste
        new(39, CompanyTheme.ConditionsTravail, "Quels sont les horaires habituels et le rythme de travail ?"),
        new(40, CompanyTheme.ConditionsTravail, "Le travail exige-t-il des déplacements, de la mobilité, une disponibilité particulière ou des horaires variables ?"),
        new(41, CompanyTheme.ConditionsTravail, "Quelles sont les principales contraintes physiques, psychologiques, relationnelles ou organisationnelles ?"),
        new(42, CompanyTheme.ConditionsTravail, "Quel niveau d'autonomie est attendu du collaborateur ?"),
        new(43, CompanyTheme.ConditionsTravail, "Quel niveau de pression, de rapidité ou de précision le travail demande-t-il ?"),
        new(44, CompanyTheme.ConditionsTravail, "Quels moyens l'entreprise met-elle à disposition pour permettre de bien travailler ?"),

        // I. Profil des collaborateurs qui s'épanouissent dans l'entreprise
        new(45, CompanyTheme.ProfilCollaborateurs, "Quelles qualités personnelles permettent de bien réussir dans l'entreprise ?"),
        new(46, CompanyTheme.ProfilCollaborateurs, "Quelles compétences techniques ou professionnelles sont les plus recherchées ?"),
        new(47, CompanyTheme.ProfilCollaborateurs, "Quel type de personnalité s'intègre facilement dans l'équipe ?"),
        new(48, CompanyTheme.ProfilCollaborateurs, "Quel type de candidat pourrait rencontrer des difficultés dans cet environnement ?"),
        new(49, CompanyTheme.ProfilCollaborateurs, "Quelles valeurs personnelles du candidat doivent être compatibles avec celles de l'entreprise ?"),

        // J. Synthèse du profil socioprofessionnel de l'entreprise
        new(50, CompanyTheme.Synthese, "Notre entreprise est principalement caractérisée par..."),
        new(51, CompanyTheme.Synthese, "Nos trois valeurs les plus importantes sont..."),
        new(52, CompanyTheme.Synthese, "Notre style de leadership est plutôt..."),
        new(53, CompanyTheme.Synthese, "Notre mode relationnel est plutôt..."),
        new(54, CompanyTheme.Synthese, "Les collaborateurs qui réussissent le mieux chez nous sont ceux qui..."),
        new(55, CompanyTheme.Synthese, "Les candidats doivent être particulièrement attentifs à..."),
    ];
}
