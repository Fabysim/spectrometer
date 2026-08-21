namespace Spectrometre.Modules.JeunesPrestataires.Catalog;

/// <summary>
/// Contenu enrichi du document « Suite Réflexion consciente chez le jeune » — ajout, pas remplacement.
/// Catégorie A (A.1–A.5) prolonge <see cref="AutoObservationCatalog.Part2Sections"/> (employabilité).
/// Catégorie B (B.1–B.5) prolonge <see cref="AutoObservationCatalog.Part0Sections"/> (orientation).
/// Clés nouvelles ; hors synthèse structurée (cycle 1).
/// </summary>
public static partial class AutoObservationCatalog
{
    public static readonly string CategorieAIntro =
        "Cette partie devrait être présentée comme un parcours simple, rassurant et concret. Elle vise à identifier les missions accessibles, les besoins d'encadrement et les habitudes à développer progressivement.";

    public static readonly string CategorieBIntro =
        "Cette partie devrait aider le jeune à relire ses expériences, à identifier ses intérêts profonds, ses valeurs, ses conditions de réussite et les pistes d'études ou de carrière à explorer concrètement.";

    public static IReadOnlyList<AutoObservationSectionDef> CategorieASections { get; } =
    [
        new(
            "p2.s14",
            2,
            14,
            "A.1 Missions que je me sens capable d'essayer",
            CategorieAIntro,
            [
                Multi(
                    "p2.s14.essayer",
                    "Parmi les petits travaux suivants, lesquels aurais-tu envie d'essayer ?",
                    "Jardinage", "Rangement", "Nettoyage", "Aide aux courses", "Montage simple",
                    "Peinture simple", "Déménagement léger", "Lavage de voiture", "Autre"),
                Open("p2.s14.essayer.autre", "Autre (missions à essayer)"),
                Open("p2.s14.faciles", "Quelles missions te semblent faciles à commencer avec un peu d'explication ?"),
                Open("p2.s14.accompagne", "Quelles missions voudrais-tu essayer seulement si quelqu'un t'accompagne au début ?"),
                Open("p2.s14.eviter", "Y a-t-il des missions que tu préfères éviter pour le moment ? Lesquelles ?"),
                Open("p2.s14.confiance", "Quelle petite mission te donnerait le plus confiance si tu la réussissais ?"),
            ]),
        new(
            "p2.s15",
            2,
            15,
            "A.2 Habitudes de travail à développer",
            null,
            [
                Single("p2.s15.ponctuel", "Est-ce que tu arrives généralement à l'heure à un rendez-vous ou à une activité ?",
                    "Oui", "Non", "Parfois"),
                Open("p2.s15.aide_ponctualite", "Qu'est-ce qui pourrait t'aider à être ponctuel : rappel téléphone, message du coach, préparation la veille, trajet accompagné, autre ?"),
                Single("p2.s15.consigne_forme", "Quand tu reçois une consigne, préfères-tu qu'elle soit donnée oralement, par écrit, par démonstration ou étape par étape ?",
                    "oralement", "par écrit", "par démonstration", "étape par étape"),
                Single("p2.s15.demander_precision", "Est-ce que tu oses demander une précision si tu n'as pas compris ?",
                    "Oui", "Non", "Cela dépend"),
                Single("p2.s15.ranger", "Après une tâche, penses-tu à ranger le matériel et à laisser l'endroit propre ?",
                    "Oui", "Non", "À apprendre"),
                Multi(
                    "p2.s15.habitudes",
                    "Quelles habitudes veux-tu améliorer en priorité :",
                    "ponctualité", "Discipline", "politesse", "ordre", "propreté",
                    "soin de soi et bonne tenue", "autonomie", "communication respectueuse",
                    "persévérance", "Assiduité", "engagement", "responsabilité", "Autre"),
                Open("p2.s15.habitudes.autre", "Autre (habitudes)"),
            ]),
        new(
            "p2.s16",
            2,
            16,
            "A.3 Besoin d'encadrement et de sécurité",
            null,
            [
                Multi(
                    "p2.s16.besoin_depart",
                    "Pour commencer une mission, de quoi as-tu besoin :",
                    "une explication et des consignes claires",
                    "une démonstration pratique",
                    "une checklist",
                    "un accompagnement",
                    "une personne référence à qui je peux poser des questions"),
                Single("p2.s16.accompagnement", "Préfères-tu être accompagné :",
                    "pendant toute la mission", "seulement au début", "être autonome après les explications"),
                Open("p2.s16.rassurer", "Qu'est-ce qui pourrait te rassurer avant d'aller chez un particulier pour une première mission ?"),
                Open("p2.s16.contacter_coach", "Dans quelles situations aimerais-tu pouvoir contacter rapidement le coach ?"),
                Multi(
                    "p2.s16.retour",
                    "De quel type de retour as-tu besoin après une mission :",
                    "encouragement", "correction précise", "conseils pratiques", "bilan avec le coach"),
            ]),
        new(
            "p2.s17",
            2,
            17,
            "A.4 Préférences de contexte et limites actuelles",
            null,
            [
                Single("p2.s17.lieu", "Préfères-tu travailler :",
                    "à l'intérieur", "à l'extérieur", "cela dépend de la mission"),
                Single("p2.s17.avec_qui", "Préfères-tu travailler :",
                    "seul", "avec une autre personne", "avec une présence rassurante au début",
                    "en alternance entre le travail solitaire et le travail en équipe"),
                Multi(
                    "p2.s17.difficulte",
                    "Quelles situations pourraient te mettre en difficulté :",
                    "parler à un inconnu", "entrer dans un domicile", "être observé pendant le travail",
                    "avoir des remarques sur mon travail", "ne pas savoir quoi faire"),
                Open("p2.s17.eviter", "Quelles missions ou situations devrions-nous éviter pour le moment ? Pourquoi ?"),
                Open("p2.s17.conditions", "Quelles conditions rendraient une première mission plus facile et plus sécurisante pour toi ? Pourquoi ?"),
            ]),
        new(
            "p2.s18",
            2,
            18,
            "A.5 Motivation immédiate et progression",
            null,
            [
                Multi(
                    "p2.s18.pourquoi",
                    "Pourquoi souhaites-tu commencer par de petits travaux ponctuels :",
                    "gagner un revenu", "prendre confiance", "apprendre", "rendre service",
                    "construire ton CV", "financer un projet"),
                Open("p2.s18.premiere_mission", "Quelle première mission te semble réaliste dans les prochaines semaines ? Pourquoi ?"),
                Open("p2.s18.habitude_premier", "Quelle habitude de travail veux-tu développer en premier ?"),
                Multi(
                    "p2.s18.progres",
                    "Comment sauras-tu que tu as progressé après une ou deux missions ?",
                    "Je me fixe des objectifs à atteindre",
                    "Je compare ce que je savais faire avant et ce que je sais faire maintenant",
                    "Je remarque que je comprends mieux les consignes",
                    "Je fais moins d'erreurs qu'au début",
                    "Je termine la mission avec plus d'autonomie",
                    "Je respecte mieux les horaires et les étapes prévues",
                    "Je demande de l'aide seulement quand c'est nécessaire",
                    "Je prends davantage d'initiative",
                    "Je me sens plus à l'aise avec la personne chez qui je travaille",
                    "Je range le matériel et laisse l'endroit propre sans qu'on me le rappelle",
                    "Je reçois un retour positif du coach, du particulier ou de l'équipe",
                    "Je suis capable d'expliquer ce que j'ai appris après la mission",
                    "Je sais identifier ce que je dois encore améliorer",
                    "Je me sens plus confiant pour refaire une mission similaire",
                    "Autre"),
                Open("p2.s18.progres.autre", "Autre (progression)"),
                Multi(
                    "p2.s18.cv",
                    "Quelles qualités, aptitudes aimerais-tu pouvoir écrire dans ton CV, comme après tes apprentissages pour augmenter ta confiance en toi ?",
                    "Ponctualité", "Assiduité", "Fiabilité", "Sens des responsabilités",
                    "Respect des consignes", "Politesse et bonne présentation", "Communication respectueuse",
                    "Capacité à demander de l'aide au bon moment", "Autonomie progressive", "Esprit d'équipe",
                    "Entraide et collaboration", "Sens du service", "Persévérance", "Motivation", "Engagement",
                    "Capacité d'adaptation", "Organisation", "Méthode de travail", "Rigueur", "Attention aux détails",
                    "Soin du matériel", "Respect de l'environnement de travail", "Ordre et propreté",
                    "Gestion du temps", "Respect des délais", "Initiative", "Curiosité et envie d'apprendre",
                    "Capacité à apprendre par la pratique", "Capacité à accepter les remarques constructives",
                    "Capacité à progresser après un retour", "Confiance en soi", "Patience", "Concentration",
                    "Résolution de petits problèmes", "Travail soigné", "Travail en sécurité",
                    "Capacité à terminer une tâche commencée",
                    "Capacité à travailler chez un particulier avec respect", "Sens de l'effort", "Autre"),
                Open("p2.s18.cv.autre", "Autre (qualités CV)"),
            ]),
    ];

    public static IReadOnlyList<AutoObservationSectionDef> CategorieBSections { get; } =
    [
        new(
            "p0.s9",
            0,
            9,
            "B.1 Expériences passées et apprentissages",
            CategorieBIntro,
            [
                Multi(
                    "p0.s9.experiences",
                    "Quelles expériences as-tu déjà vécues :",
                    "travail", "stage", "bénévolat", "formation", "aide familiale",
                    "service rendu à des particuliers"),
                Multi(
                    "p0.s9.ressenti",
                    "Dans quelle expérience t'es-tu senti",
                    "le plus utile", "le plus compétent"),
                Open("p0.s9.ressenti_pourquoi", "Pourquoi ?"),
                Open("p0.s9.plu", "Qu'est-ce qui t'a plu dans tes expériences précédentes ?"),
                Open("p0.s9.fatigue", "Qu'est-ce qui t'a fatigué, stressé ou découragé ?"),
                Open("p0.s9.appris_travailler", "Qu'as-tu appris sur ta manière de travailler ?"),
                Open("p0.s9.appris_communiquer", "Qu'as-tu appris sur ta manière de communiquer ?"),
                Open("p0.s9.appris_organiser", "Qu'as-tu appris sur ta manière de t'organiser ?"),
                Open("p0.s9.eviter", "Quelles expériences aimerais-tu éviter de reproduire ?"),
            ]),
        new(
            "p0.s10",
            0,
            10,
            "B.2 Intérêts, préférences et domaines d'énergie",
            null,
            [
                Multi(
                    "p0.s10.activites",
                    "Quels types d'activités te donnent envie d'apprendre davantage :",
                    "manuel", "Aide", "soins", "technique", "intellectuel", "artistique",
                    "créatif", "relationnel", "numérique", "entrepreneurial", "managérial"),
                Multi(
                    "p0.s10.notion_temps",
                    "Dans quelles activités perds-tu la notion du temps parce que tu es intéressé ?",
                    "Intellectuel", "Manuel", "Technique", "Routinier"),
                Multi(
                    "p0.s10.preferes",
                    "Préfères-tu :",
                    "résoudre des problèmes humains", "résoudre des problèmes mécaniques",
                    "créer", "aider", "organiser", "réparer", "construire", "communiquer",
                    "rechercher", "analyser", "enquêter", "assembler"),
                Open("p0.s10.energie", "Quelles tâches te donnent de l'énergie même si elles demandent un effort ?"),
                Multi(
                    "p0.s10.satisfaction",
                    "Quelles situations te procurent un sentiment de satisfaction ?",
                    "Le travail achevé",
                    "L'effort fourni pour accomplir une tâche que j'aime faire",
                    "Quand l'accomplissement ou la réalisation d'une tâche me procure un sentiment d'utilité",
                    "Quand je vois un résultat concret de mon travail",
                    "Quand quelqu'un reconnaît la qualité de ce que j'ai fait",
                    "Quand j'ai réussi à dépasser une difficulté",
                    "Quand j'ai appris quelque chose de nouveau en faisant la tâche",
                    "Quand j'ai pu aider quelqu'un ou rendre service",
                    "Quand j'ai travaillé avec sérieux jusqu'au bout",
                    "Quand je me sens fier de mes progrès",
                    "Quand mes compétences ont été utiles dans une situation réelle",
                    "Quand j'ai respecté un délai ou une consigne importante",
                    "Quand j'ai contribué à un projet collectif",
                    "Autre"),
                Open("p0.s10.satisfaction.autre", "Autre (satisfaction)"),
            ]),
        new(
            "p0.s11",
            0,
            11,
            "B.3 Valeurs professionnelles et conditions de réussite",
            null,
            [
                Multi(
                    "p0.s11.valeurs",
                    "Quelles valeurs sont les plus importantes pour toi dans le travail :",
                    "autonomie", "Engagement", "Humilité", "respect de soi", "Estime de soi",
                    "Justice", "Honnêteté", "Intégrité", "Liberté", "Dignité", "Crédibilité",
                    "Responsabilité", "Ordre et propreté"),
                Multi(
                    "p0.s11.environnement",
                    "Quel type d'environnement te permet de donner le meilleur de toi-même ?",
                    "Entraide et collaboration",
                    "Cadre clair avec des consignes précises",
                    "Ambiance calme et respectueuse",
                    "Équipe bienveillante où l'on peut poser des questions",
                    "Responsabilités bien définies",
                    "Équilibre entre autonomie et accompagnement",
                    "Objectifs concrets et réalistes",
                    "Possibilité d'apprendre progressivement",
                    "Reconnaissance des efforts et du travail accompli",
                    "Communication simple, directe et constructive",
                    "Environnement propre, ordonné et sécurisant",
                    "Rythme de travail adapté, sans pression excessive",
                    "Possibilité de travailler seul par moments et en équipe à d'autres moments",
                    "Autre"),
                Open("p0.s11.environnement.autre", "Autre (environnement)"),
                Multi(
                    "p0.s11.motivation_long_terme",
                    "Quelles conditions doivent être présentes pour que tu puisses rester motivé à long terme ?",
                    "Comprendre le sens et l'utilité de ce que je fais",
                    "Avoir des objectifs clairs, réalistes et progressifs",
                    "Voir mes progrès au fil du temps",
                    "Recevoir des encouragements et des retours constructifs",
                    "Me sentir respecté et écouté",
                    "Travailler dans un environnement stable, structuré et rassurant",
                    "Avoir un bon équilibre entre autonomie et accompagnement",
                    "Pouvoir apprendre de nouvelles choses régulièrement",
                    "Avoir des tâches variées, mais pas trop dispersées",
                    "Sentir que mes efforts sont reconnus",
                    "Pouvoir contribuer à quelque chose d'utile ou de concret",
                    "Avoir une relation de confiance avec le responsable, le coach ou l'équipe",
                    "Disposer de consignes claires et d'un suivi adapté",
                    "Pouvoir poser des questions sans me sentir jugé",
                    "Avoir un rythme de travail soutenable dans la durée",
                    "Me sentir en sécurité, physiquement et émotionnellement",
                    "Avoir des perspectives d'évolution, de formation ou de responsabilité",
                    "Autre"),
                Open("p0.s11.motivation_long_terme.autre", "Autre (motivation à long terme)"),
            ]),
        new(
            "p0.s12",
            0,
            12,
            "B.4 Pistes d'études, de métiers ou de carrière",
            null,
            [
                Open("p0.s12.envisagees", "Quelles formations, études ou métiers as-tu déjà envisagés ?"),
                Open("p0.s12.energie", "Quelle piste te donne le plus d'énergie quand tu en parles ?"),
                Open("p0.s12.realiste", "Quelle piste te paraît réaliste si tu es accompagné au début ?"),
                Open("p0.s12.peur", "Quelle piste te fait peur mais t'attire quand même ?"),
                Open("p0.s12.valeurs", "Quelle piste correspond le mieux à tes valeurs et à tes forces ?"),
                Open("p0.s12.tester", "Quelle piste pourrait être testée par une mission, un stage, une rencontre métier ou une courte formation ?"),
            ]),
        new(
            "p0.s13",
            0,
            13,
            "B.5 Décision, expérimentation et plan d'action",
            null,
            [
                Open("p0.s13.priorite", "Parmi les pistes identifiées, laquelle souhaites-tu explorer en priorité ?"),
                Open("p0.s13.action_30j", "Quelle première action concrète pourrais-tu faire dans les 30 prochains jours ?"),
                Open("p0.s13.informations", "De quelles informations as-tu encore besoin avant de décider ?"),
                Open("p0.s13.aide", "Qui pourrait t'aider à confirmer ou nuancer cette piste : coach, professionnel, enseignant, parent, ancien employeur ?"),
                Open("p0.s13.signe", "Quel signe te montrera que cette piste mérite d'être poursuivie ?"),
            ]),
    ];
}
