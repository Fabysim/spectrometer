namespace Spectrometre.Modules.JeunesPrestataires.Catalog;

/// <summary>
/// Partie 0 — Questionnaire générique d'exploration socioprofessionnelle (document Bouchra
/// « avec et sans expérience de travail »), placée en amont des parties 1 et 2 existantes.
/// </summary>
public static partial class AutoObservationCatalog
{
    public static readonly string Part0Intro =
        "Ce questionnaire s'adresse à un jeune adulte qui cherche à mieux comprendre son identité socioprofessionnelle. Il vise à favoriser une réflexion consciente sur son parcours, ses intérêts, ses besoins de cadre, son rapport aux autres, ses zones de confiance et ses conditions de réussite. Il ne sert pas à poser un diagnostic, mais à mettre des mots sur ce qui aide, ce qui freine et ce qui donne envie d'avancer.";

    public static readonly string Part0Consigne =
        "Consigne proposée : répondre avec sincérité, sans chercher la bonne réponse. Le jeune peut cocher plusieurs propositions, ajouter des exemples personnels ou utiliser une échelle de 1 à 5 : 1 = pas du tout, 2 = plutôt non, 3 = parfois, 4 = plutôt oui, 5 = tout à fait.";

    public static IReadOnlyList<AutoObservationSectionDef> Part0Sections { get; } =
    [
        new(
            "p0.s1",
            0,
            1,
            "1. Mon parcours et ce que j'en retiens",
            Part0Consigne,
            [
                Multi(
                    "p0.s1.experiences",
                    "Jusqu'à présent, quelles expériences m'ont marqué ?",
                    "de formation",
                    "de stage",
                    "de travail",
                    "de bénévolat",
                    "d'aide aux autres"),
            ]),
        new(
            "p0.s2",
            0,
            2,
            "2. Mon rapport au cadre, aux consignes et à l'autonomie",
            null,
            [
                Multi(
                    "p0.s2.ressenti_consigne",
                    "Quand je reçois une consigne, qu'est-ce que je ressens le plus souvent ?",
                    "confiance",
                    "pression",
                    "résistance",
                    "besoin de comprendre",
                    "peur de mal faire"),
                Multi(
                    "p0.s2.aide_regle",
                    "Qu'est-ce qui m'aide à accepter une règle ou une méthode ?",
                    "une explication claire",
                    "une démonstration",
                    "une consigne écrite",
                    "un temps pour poser mes questions"),
                Multi(
                    "p0.s2.niveau_autonomie",
                    "De quel niveau d'autonomie ai-je besoin pour bien travailler ?",
                    "être guidé au début",
                    "travailler seul ensuite recevoir des points de suivi réguliers",
                    "organiser ma méthode librement"),
                Multi(
                    "p0.s2.ma_facon",
                    "Dans quelles situations ai-je tendance à vouloir faire les choses à ma façon ?",
                    "Quand je pense avoir trouvé une méthode plus rapide ou plus efficace",
                    "Quand la consigne ne me paraît pas logique ou pas assez expliquée",
                    "Quand je me sens contrôlé, surveillé ou limité dans ma liberté",
                    "Quand j'ai déjà l'habitude de faire autrement",
                    "Quand je veux montrer que je suis capable de me débrouiller seul",
                    "Quand je crains de faire une erreur si je suis obligé de suivre une méthode que je ne comprends pas",
                    "Quand je préfère tester par moi-même avant de demander de l'aide",
                    "Quand je ressens le besoin de personnaliser le travail pour qu'il me ressemble davantage",
                    "Quand je ne sais pas si la règle est vraiment obligatoire ou seulement une façon de faire parmi d'autres",
                    "Autre situation"),
                Multi(
                    "p0.s2.initiative_utile",
                    "Comment puis-je distinguer une initiative utile d'un refus de suivre le cadre ?",
                    "Je propose une autre méthode après avoir compris la consigne de départ",
                    "Je vérifie d'abord si la règle est obligatoire ou si elle peut être adaptée",
                    "J'explique pourquoi ma proposition pourrait améliorer le résultat, le temps ou la qualité",
                    "Je demande l'accord avant de changer la méthode demandée",
                    "J'accepte d'essayer la méthode proposée avant de conclure qu'elle ne me convient pas",
                    "Je distingue ce qui est non négociable de ce qui peut être organisé autrement",
                    "Je reconnais que suivre le cadre peut être nécessaire même si je préfère une autre façon de faire",
                    "Je remarque si mon envie de faire autrement vient d'une idée utile ou d'une réaction à l'autorité",
                    "Je peux expliquer ma proposition calmement sans me braquer si elle n'est pas acceptée",
                    "Autre repère personnel"),
            ]),
        new(
            "p0.s3",
            0,
            3,
            "3. Mon rapport au regard des autres, à l'erreur et à la confiance",
            null,
            [
                Multi(
                    "p0.s3.peur_observe",
                    "Dans quelles situations ai-je peur d'être observé pendant que je travaille ?",
                    "Quand quelqu'un reste près de moi pendant que je réalise une tâche",
                    "Quand je dois apprendre quelque chose de nouveau devant une autre personne",
                    "Quand un adulte, un coach, un collègue ou un responsable regarde ma manière de faire",
                    "Quand je dois travailler rapidement alors qu'on attend un résultat",
                    "Quand je ne suis pas sûr d'avoir bien compris la consigne",
                    "Quand je dois montrer un travail avant qu'il soit terminé",
                    "Quand je sens que l'on compare ma manière de faire à celle des autres",
                    "Quand j'ai peur que l'on remarque mes hésitations ou mes erreurs",
                    "Autre situation"),
                Multi(
                    "p0.s3.pensees_autres",
                    "Qu'est-ce que j'imagine que les autres pensent de moi lorsque je débute, hésite ou me trompe ?",
                    "Ils pensent que je ne suis pas capable",
                    "Ils pensent que je ne suis pas assez rapide",
                    "Ils pensent que je n'ai pas écouté ou pas compris",
                    "Ils pensent que je ne fais pas assez d'efforts",
                    "Ils pensent que je ne suis pas fiable",
                    "Ils vont se moquer de moi ou me juger",
                    "Ils vont perdre patience",
                    "Ils vont préférer demander à quelqu'un d'autre",
                    "Je ne sais pas vraiment ce qu'ils pensent, mais je l'imagine négativement",
                    "Autre pensée"),
                Multi(
                    "p0.s3.erreurs_normales",
                    "Quelles erreurs puis-je accepter comme normales dans un apprentissage ?",
                    "Ne pas réussir du premier coup",
                    "Avoir besoin qu'on me répète ou reformule une consigne",
                    "Aller plus lentement au début pour bien faire",
                    "Me tromper dans l'ordre des étapes au départ",
                    "Poser une question parce que je ne suis pas sûr d'avoir compris",
                    "Oublier un détail non essentiel et le corriger ensuite",
                    "Demander une démonstration avant d'essayer seul",
                    "Avoir besoin de plusieurs essais avant d'être à l'aise",
                    "Autre erreur normale"),
                Multi(
                    "p0.s3.erreurs_difficiles",
                    "Quelles erreurs me paraissent plus difficiles à accepter ?",
                    "Refaire plusieurs fois la même erreur après explication",
                    "Faire perdre du temps à quelqu'un",
                    "Abîmer du matériel, un objet ou le travail d'une autre personne",
                    "Être corrigé devant les autres",
                    "Donner l'impression que je n'ai pas écouté",
                    "Donner l'impression que je ne fais pas d'efforts",
                    "Être jugé comme incapable, pas sérieux ou pas fiable",
                    "Me tromper alors que je voulais vraiment bien faire",
                    "Autre erreur difficile"),
                Multi(
                    "p0.s3.preuves",
                    "Quelles preuves concrètes ai-je déjà que je suis capable d'apprendre, de réussir ou de m'adapter ?",
                    "J'ai déjà terminé une formation, une année, un stage ou une mission",
                    "J'ai déjà appris une tâche que je ne savais pas faire au départ",
                    "J'ai déjà été félicité ou remercié pour quelque chose que j'ai fait",
                    "J'ai déjà aidé quelqu'un de manière utile",
                    "J'ai déjà réussi à m'organiser pour respecter un engagement",
                    "J'ai déjà surmonté une difficulté sans abandonner immédiatement",
                    "J'ai déjà changé de méthode après avoir compris ce qui ne fonctionnait pas",
                    "J'ai déjà pris une responsabilité, même petite",
                    "Autre preuve personnelle"),
                Multi(
                    "p0.s3.retrouver_confiance",
                    "Qu'est-ce qui m'aide à retrouver confiance après une erreur ou une remarque ?",
                    "Comprendre précisément ce qui n'a pas fonctionné",
                    "Recevoir une explication calme plutôt qu'une critique sèche",
                    "Avoir le droit de recommencer",
                    "Être encouragé pour l'effort fourni, même si le résultat n'est pas parfait",
                    "Voir que l'erreur peut être corrigée",
                    "Prendre quelques minutes pour respirer ou me calmer",
                    "Demander un exemple concret de ce qui est attendu",
                    "Me rappeler une situation où j'ai déjà réussi",
                    "Parler avec une personne de confiance ou un coach",
                    "Autre aide possible"),
            ]),
        new(
            "p0.s4",
            0,
            4,
            "4. Mes expériences de travail et mes besoins professionnels",
            null,
            [
                Multi(
                    "p0.s4.moments_energie",
                    "Quels moments de ma journée de travail me donnent de l'énergie ?",
                    "Quand je commence une tâche avec un objectif clair",
                    "Quand je peux bouger, agir ou faire quelque chose de concret",
                    "Quand je vois rapidement le résultat de mon travail",
                    "Quand je me sens utile pour quelqu'un",
                    "Quand je peux organiser ma façon de faire",
                    "Quand je progresse ou que je réussis mieux qu'avant",
                    "Quand je reçois un merci, un encouragement ou une reconnaissance",
                    "Autre moment"),
                Multi(
                    "p0.s4.moments_fatigue",
                    "Quels moments me fatiguent ou me vident ?",
                    "Quand je reçois trop d'informations en même temps",
                    "Quand je ne comprends pas bien ce qu'on attend de moi",
                    "Quand je me sens observé, pressé ou jugé",
                    "Quand je dois rester longtemps concentré sans pause",
                    "Quand je dois refaire une tâche sans comprendre pourquoi",
                    "Quand je travaille dans le bruit, le désordre ou la tension",
                    "Quand je termine sans avoir l'impression d'avoir appris ou avancé",
                    "Autre moment"),
                Multi(
                    "p0.s4.contextes",
                    "Dans quels contextes est-ce que je travaille le mieux ?",
                    "Seul",
                    "En duo",
                    "En petite équipe",
                    "Dehors",
                    "À l'intérieur",
                    "Avec des gestes concrets ou manuels",
                    "Avec des idées, de la réflexion ou de la création",
                    "Avec des contacts réguliers",
                    "Avec peu de contacts et plus d'autonomie",
                    "Autre contexte"),
                Multi(
                    "p0.s4.manque",
                    "Qu'est-ce qui me manque dans mes activités actuelles ?",
                    "Plus de créativité",
                    "Plus de reconnaissance",
                    "Plus de liberté",
                    "Plus de sécurité",
                    "Plus de progression ou d'apprentissage",
                    "Plus d'utilité pour les autres",
                    "Plus d'appartenance à une équipe ou à un projet",
                    "Plus de responsabilités",
                    "Plus de variété dans les tâches",
                    "Autre besoin"),
                Multi(
                    "p0.s4.activite_ideale",
                    "Si je pouvais garder les avantages d'une activité actuelle tout en ajoutant ce qui me manque, quel type d'activité imaginerais-je ?",
                    "Une activité avec de l'autonomie mais un cadre clair",
                    "Une activité concrète où je vois le résultat de mon travail",
                    "Une activité utile aux autres",
                    "Une activité créative ou technique",
                    "Une activité qui me permet de bouger et de ne pas rester toujours au même endroit",
                    "Une activité où je travaille souvent seul, mais avec un soutien disponible",
                    "Une activité qui me permet d'apprendre progressivement",
                    "Une activité liée à un projet personnel ou entrepreneurial",
                    "Autre idée"),
            ]),
        new(
            "p0.s5",
            0,
            5,
            "5. Mes intérêts profonds, mes valeurs et mes pistes possibles",
            null,
            [
                Multi(
                    "p0.s5.envie_apprendre",
                    "Quelles activités me donnent envie d'apprendre davantage ?",
                    "Créer, dessiner, imaginer ou concevoir",
                    "Réparer, construire, assembler ou manipuler des outils",
                    "Aider les autres ou rendre un service concret",
                    "Organiser, planifier ou trouver des solutions pratiques",
                    "Utiliser le numérique, les applications ou les outils informatiques",
                    "Travailler dehors, bouger ou réaliser des tâches physiques",
                    "Comprendre comment fonctionne un métier, une machine ou un système",
                    "Autre activité"),
                Multi(
                    "p0.s5.utile_place",
                    "Quelles activités me donnent le sentiment d'être utile ou à ma place ?",
                    "Quand j'aide quelqu'un à résoudre un problème",
                    "Quand je rends un service concret",
                    "Quand je participe à un projet collectif",
                    "Quand je crée quelque chose qui peut servir à d'autres",
                    "Quand je prends une responsabilité et que je vais jusqu'au bout",
                    "Quand mes efforts sont visibles ou reconnus",
                    "Quand je peux utiliser mes qualités personnelles",
                    "Autre situation"),
                Multi(
                    "p0.s5.valeurs",
                    "Quelles valeurs sont importantes pour moi dans le travail ?",
                    "Autonomie",
                    "Sécurité",
                    "Respect",
                    "Justice",
                    "Créativité",
                    "Reconnaissance",
                    "Entraide",
                    "Progression",
                    "Liberté",
                    "Travail bien fait",
                    "Autre valeur"),
                Multi(
                    "p0.s5.pistes",
                    "Quelles pistes professionnelles ou activités ai-je déjà envisagées ?",
                    "Un métier manuel ou technique",
                    "Un métier créatif ou artistique",
                    "Un métier avec du contact humain",
                    "Un métier plus autonome ou indépendant",
                    "Un métier dans une entreprise",
                    "Une formation ou un apprentissage",
                    "Un projet personnel, numérique ou entrepreneurial",
                    "Une mission temporaire pour tester une piste",
                    "Autre piste"),
                Multi(
                    "p0.s5.ressenti_pistes",
                    "Parmi ces pistes, lesquelles me donnent de l'énergie, me rassurent ou me font peur ?",
                    "Cette piste me donne envie d'essayer",
                    "Cette piste me rassure parce qu'elle paraît stable",
                    "Cette piste me fait peur parce qu'elle demande de sortir de ma zone de confort",
                    "Cette piste me motive mais je ne sais pas par où commencer",
                    "Cette piste correspond à mes valeurs",
                    "Cette piste semble difficile mais intéressante",
                    "Cette piste me paraît possible si je suis accompagné au début",
                    "Autre ressenti"),
                Multi(
                    "p0.s5.piste_tester",
                    "Quelle piste mérite d'être testée concrètement avant de prendre une décision définitive ?",
                    "Celle qui me donne le plus d'énergie quand j'en parle",
                    "Celle qui correspond le mieux à mes valeurs",
                    "Celle qui me permet d'apprendre progressivement",
                    "Celle qui peut être testée par un stage, une mission ou une rencontre",
                    "Celle qui combine autonomie et cadre clair",
                    "Celle qui me fait peur mais que j'ai envie d'explorer",
                    "Celle qui peut m'aider à construire mon CV ou mon expérience",
                    "Autre piste à tester"),
            ]),
        new(
            "p0.s6",
            0,
            6,
            "6. Mes conditions de réussite",
            null,
            [
                Multi(
                    "p0.s6.besoins_depart",
                    "Pour réussir dans une activité, de quoi ai-je besoin au départ ?",
                    "Des consignes claires",
                    "Une démonstration concrète",
                    "Une consigne écrite ou une checklist",
                    "Le droit de poser des questions",
                    "Le droit de faire des erreurs au début",
                    "Un accompagnement au départ",
                    "Une autonomie progressive",
                    "Un retour calme sur ce qui est réussi et ce qui est à améliorer",
                    "Autre besoin"),
            ]),
        new(
            "p0.s7",
            0,
            7,
            "7. Grille d'aide à la décision",
            "Évaluez chaque piste de 1 à 5 pour chaque critère (1 = pas du tout, 5 = tout à fait).",
            BuildGrilleAideDecision()),
        new(
            "p0.s8",
            0,
            8,
            "8. Conclusion personnelle et premier plan d'action",
            null,
            [
                Open("p0.s8.piste_prioritaire", "La piste que je souhaite explorer en priorité est :"),
                Open("p0.s8.raison_choix", "La raison principale de ce choix est :"),
                Open("p0.s8.valeurs_besoins", "Ce que cette piste dit de mes valeurs ou de mes besoins est :"),
                Open("p0.s8.obstacle", "Le principal obstacle que je dois anticiper est :"),
                Open("p0.s8.action_30j", "La première action concrète à réaliser dans les 30 prochains jours est :"),
                Open("p0.s8.personne_aide", "La personne qui peut m'aider à garder le cap est :"),
                Open("p0.s8.apprendre_soi", "Ce que je veux apprendre sur moi-même pendant cette étape est :"),
            ]),
        ..CategorieBSections,
    ];

    /// <summary>
    /// 6 critères × 4 pistes = 24 questions <see cref="AutoObservationFieldType.Scale1To5"/> —
    /// représentation tabulaire du document sans nouveau type de champ.
    /// </summary>
    private static IReadOnlyList<AutoObservationQuestionDef> BuildGrilleAideDecision()
    {
        (string Suffix, string Critere)[] criteres =
        [
            ("motivation", "Motivation personnelle"),
            ("valeurs", "Compatibilité avec mes valeurs"),
            ("autonomie", "Compatibilité avec mon besoin d'autonomie"),
            ("apprendre", "Possibilité d'apprendre et de progresser"),
            ("stress", "Risque de stress ou d'évitement"),
            ("utilite", "Sentiment d'utilité et de réalisation"),
        ];

        var questions = new List<AutoObservationQuestionDef>(24);
        for (var piste = 1; piste <= 4; piste++)
        {
            foreach (var (suffix, critere) in criteres)
            {
                questions.Add(Scale5(
                    $"p0.s7.piste{piste}.{suffix}",
                    $"Piste {piste} — {critere}"));
            }
        }

        return questions;
    }
}
