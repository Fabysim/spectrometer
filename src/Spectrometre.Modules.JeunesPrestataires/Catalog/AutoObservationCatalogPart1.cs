namespace Spectrometre.Modules.JeunesPrestataires.Catalog;

public static partial class AutoObservationCatalog
{
    public static readonly string PageIntro =
        "Ce questionnaire dans l'appli appuie concrètement le jeune en questionnement sur son profil socio professionnel pour mieux orienter sa carrière, sur le développement son employabilité pour être plus attractif sur le marché de l'emploi, à construire un CV compatible pour travailler dans un environnement et un contexte approprié.";

    public static readonly string PageAccroche =
        "Les éléments surlignés en jaune devraient apparaître comme éléments d'accroche";

    public static readonly string PageAideIntro =
        "Ce questionnaire peut être complété seul ou avec le coach en cliquant sur le bouton besoin d'aide, afin de choisir des missions adaptées et de fixer des objectifs de progression réalistes.";

    public static readonly string Part2Intro =
        "Le formulaire qui suit est destiné à être intégré dans l'application lors de l'inscription du jeune. Il permet de recueillir une première auto-perception de son profil : compétences, aptitudes, communication, relation aux autres, organisation, soin, valeurs, disponibilités, mobilité, préférences de missions et limites personnelles. Les réponses ne constituent pas un jugement : elles servent de point de départ pour construire un programme d'accompagnement adapté avec le coach.";

    private static AutoObservationQuestionDef Open(string key, string label) =>
        new(key, label, AutoObservationFieldType.OpenText);

    private static AutoObservationQuestionDef Multi(string key, string label, params string[] options) =>
        new(key, label, AutoObservationFieldType.MultiCheckbox, options, $"{key}.autre");

    private static AutoObservationQuestionDef Single(string key, string label, params string[] options) =>
        new(key, label, AutoObservationFieldType.SingleChoice, options);

    private static AutoObservationQuestionDef Scale5(string key, string label) =>
        new(key, label, AutoObservationFieldType.Scale1To5);

    private static AutoObservationQuestionDef Scale4(string key, string label) =>
        new(key, label, AutoObservationFieldType.Scale1To4);

    public static IReadOnlyList<AutoObservationSectionDef> Part1Sections { get; } =
    [
        new(
            "p1.s1",
            1,
            1,
            "1. Activités qui me donnent de l'énergie ou du plaisir",
            null,
            [
                Open("p1.s1.q1", "Quelles activités me donnent naturellement envie de commencer, même si elles demandent un effort ?"),
                Open("p1.s1.q2", "Quelles tâches me procurent un sentiment de satisfaction une fois terminées ?"),
                Open("p1.s1.q3", "Dans quelles activités ai-je l'impression de voir rapidement le résultat de mon travail ?"),
                Open("p1.s1.q4", "Quelles tâches me font dire : « ça, je pourrais le refaire » ?"),
                Open("p1.s1.q5", "Parmi les missions possibles, lesquelles me semblent les plus attirantes : jardinage, rangement, nettoyage, aide aux courses, montage simple, peinture, aide au déménagement léger, autre ?"),
            ]),
        new(
            "p1.s2",
            1,
            2,
            "2. Activités qui me fatiguent ou me découragent",
            null,
            [
                Open("p1.s2.q1", "Quelles activités me fatiguent, rien qu'à y penser ?"),
                Open("p1.s2.q2", "Quelles tâches ai-je tendance à repousser ou à éviter ?"),
                Open("p1.s2.q3", "Quelles activités me donnent l'impression d'être dépassé ou de ne pas savoir par où commencer ?"),
                Open("p1.s2.q4", "Quelles tâches me demandent beaucoup d'énergie mentale, même si elles semblent simples pour les autres ?"),
                Open("p1.s2.q5", "Y a-t-il des types de missions que je préfère éviter pour le moment, ou accepter seulement si je suis accompagné ?"),
            ]),
        new(
            "p1.s3",
            1,
            3,
            "3. Contextes dans lesquels je me sens à l'aise",
            null,
            [
                Open("p1.s3.q1", "Dans quel type d'ambiance est-ce que je travaille le mieux : calme, dynamique, avec peu d'interruptions, avec des explications régulières, seul ou avec quelqu'un ?"),
                Open("p1.s3.q2", "Quel type de communication m'aide à bien comprendre : consignes écrites, démonstration, explication orale, étapes courtes, répétition ?"),
                Open("p1.s3.q3", "Avec quel type de personne est-ce que je me sens le plus à l'aise : personne patiente, claire, bienveillante, directe, discrète, présente sans être envahissante ?"),
                Open("p1.s3.q4", "Qu'est-ce qui me met en confiance au début d'une mission ?"),
                Open("p1.s3.q5", "De quoi ai-je besoin pour oser poser une question ou demander de l'aide ?"),
            ]),
        new(
            "p1.s4",
            1,
            4,
            "4. Contextes qui me stressent ou me mettent en difficulté",
            null,
            [
                Open("p1.s4.q1", "Quelles situations de communication me stressent : recevoir trop d'informations, devoir répondre vite, ne pas comprendre une consigne, parler à une personne inconnue, être observé ?"),
                Open("p1.s4.q2", "Quelles attitudes chez l'autre me bloquent ou me mettent mal à l'aise : impatience, ton sec, critique directe, manque de clarté, pression sur la rapidité ?"),
                Open("p1.s4.q3", "Quels signes montrent que je commence à être stressé ou dépassé ?"),
                Open("p1.s4.q4", "Dans quelles situations ai-je besoin que le coach soit disponible ou qu'un cadre soit rappelé ?"),
                Open("p1.s4.q5", "Quelles conditions rendraient une mission plus rassurante pour moi ?"),
            ]),
        new(
            "p1.s5",
            1,
            5,
            "5. Synthèse à compléter avec le coach",
            "Le jeune doit répondre à ces questions afin qu'il puisse identifier dans le tableau de synthèse certains éléments utiles pour définir son profil socioprofessionnel.",
            [
                Open("p1.s5.energie", "Activités qui me donnent de l'énergie"),
                Open("p1.s5.satisfaction", "Activités qui me procurent de la satisfaction"),
                Open("p1.s5.fatigue", "Activités qui me fatiguent ou me découragent"),
                Open("p1.s5.missions_privilegier", "Types de missions à privilégier"),
                Open("p1.s5.missions_eviter", "Types de missions à éviter ou à préparer"),
                Open("p1.s5.contextes_favorables", "Contextes relationnels favorables"),
                Open("p1.s5.contextes_anxieux", "Contextes relationnels qui me rendent anxieux"),
                Open("p1.s5.conditions_reussite", "Conditions utiles pour réussir une mission"),
                Open("p1.s5.points_progression", "Points à travailler progressivement"),
            ],
            CoachCanEditAnswers: true),
    ];
}
