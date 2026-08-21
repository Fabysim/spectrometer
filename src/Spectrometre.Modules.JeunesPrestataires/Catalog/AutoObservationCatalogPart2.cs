namespace Spectrometre.Modules.JeunesPrestataires.Catalog;

public static partial class AutoObservationCatalog
{
    private static readonly string[] SituationActuelleOptions =
    [
        "École", "Formation", "Recherche d'emploi", "Stage", "Travail étudiant", "Autre"
    ];

    private static readonly string[] MotivationOptions =
    [
        "avoir un revenu", "Apprendre", "Aider", "Me tester", "Construire mon CV",
        "Prendre confiance", "financer mes projets", "Autre"
    ];

    private static readonly string[] TachesRealiseesOptions =
    [
        "Jardinage", "Nettoyage", "Rangement", "Peinture simple", "Montage de meubles",
        "Aide aux courses", "Déménagement léger", "Lavage de voiture", "faire les courses",
        "Faire à manger", "promenade des animaux", "Garde d'enfants si qualifié", "Autre"
    ];

    private static readonly string[] ExperienceOptions =
    [
        "Travail", "Stage", "Bénévolat", "Service rendu à un voisin", "Aide familiale", "Aucune encore"
    ];

    private static readonly string[] QualitesOptions =
    [
        "Calme", "Énergique", "Patient", "Précis", "Rapide", "Créatif", "Pratique", "À l'écoute",
        "Courageux", "Persévérant", "Fiable", "Aide à la personne", "Autre"
    ];

    private static readonly string[] TypeTravailOptions =
    [
        "Travail intellectuel", "Travail manuel", "Travail artistique", "Travail relationnel",
        "Travail pratique", "Autre"
    ];

    private static readonly string[] GestesManuelsOptions =
    [
        "Réparer", "Construire", "Assembler", "Démonter", "Nettoyer", "Transformer",
        "Porter / déplacer", "Utiliser des outils", "Autre"
    ];

    private static readonly string[] ConsignesFormeOptions =
    [
        "Oralement", "Par démonstration", "Par écrit", "Étape par étape", "Avec image ou exemple"
    ];

    private static readonly string[] StressTendanceOptions =
    [
        "Parler moins", "Parler vite", "M'énerver", "Me fermer", "Demander de l'aide",
        "Ne pas savoir quoi faire", "Autre"
    ];

    private static readonly string[] AttitudesConfianceOptions =
    [
        "Calme", "Clarté", "Patience", "Encouragement", "Humour", "Respect",
        "Explications précises", "Autre"
    ];

    private static readonly string[] AttitudesDifficulteOptions =
    [
        "Ton sec", "Impatience", "Critique directe", "Pression", "Manque d'explication",
        "Méfiance", "Autre"
    ];

    private static readonly string[] RetardCausesOptions =
    [
        "Transport", "Oubli", "Mauvaise estimation du temps", "Stress",
        "Manque d'organisation", "Autre"
    ];

    private static readonly string[] OutilsOrganisationOptions =
    [
        "Agenda", "Rappels téléphone", "Aide d'un proche", "Messages du coach", "Aucun pour l'instant"
    ];

    private static readonly string[] AideOrganisationOptions =
    [
        "Checklist", "Rappel", "Consigne écrite", "Démonstration",
        "Accompagnement au départ", "Débriefing après mission"
    ];

    private static readonly string[] ValeursOptions =
    [
        "Respect", "Confiance", "Honnêteté", "Justice", "Entraide", "Autonomie",
        "Reconnaissance", "Utilité", "Travail bien fait", "Autre"
    ];

    private static readonly string[] MotivationProfondeOptions =
    [
        "Argent", "Apprendre", "Être utile", "Rencontrer des gens", "Prouver que j'en suis capable",
        "Construire mon CV", "Aider ma famille", "être fier de moi",
        "éprouver le sentiment de satisfaction", "Autre"
    ];

    private static readonly string[] JoursOptions =
    [
        "Lundi", "Mardi", "Mercredi", "Jeudi", "Vendredi", "Samedi", "Dimanche"
    ];

    private static readonly string[] PlagesOptions =
    [
        "Matin", "Après-midi", "Soir", "Vacances scolaires", "Occasionnellement"
    ];

    private static readonly string[] TransportOptions =
    [
        "À pied", "Vélo", "Bus", "Train", "Voiture familiale", "Autre"
    ];

    private static readonly string[] ContraintesOptions =
    [
        "École", "Formation", "Santé", "Famille", "Transport", "Autorisation parentale", "Autre"
    ];

    private static readonly string[] MissionsPrioriteOptions =
    [
        "Jardinage", "Rangement", "Nettoyage", "Aide aux courses", "Montage simple",
        "Peinture simple", "Déménagement léger", "Lavage de voiture", "Autre"
    ];

    private static readonly string[] ContextesPrefOptions =
    [
        "Extérieur", "Intérieur", "Calme", "Actif", "Seul", "Avec quelqu'un",
        "Consignes précises", "Autonomie progressive"
    ];

    private static readonly string[] SituationsDifficulteOptions =
    [
        "Parler à un inconnu", "Entrer dans un domicile", "Être pressé", "Porter des charges",
        "Travailler longtemps", "Recevoir une critique", "Ne pas savoir quoi faire", "Autre"
    ];

    public static IReadOnlyList<AutoObservationSectionDef> Part2Sections { get; } =
    [
        new(
            "p2.s1",
            2,
            1,
            "1. Identification et situation actuelle",
            null,
            [
                Open("p2.s1.commune", "Commune de résidence"),
                Open("p2.s1.telephone", "Téléphone"),
                Open("p2.s1.email", "E-mail"),
                Multi("p2.s1.situation", "Situation actuelle", SituationActuelleOptions),
                Open("p2.s1.situation.autre", "Autre (situation actuelle)"),
                Multi("p2.s1.motivation", "Motivation principale", MotivationOptions),
                Open("p2.s1.motivation.autre", "Autre (motivation principale)"),
                Open("p2.s1.coach_savoir", "Ce que j'aimerais que le coach sache"),
            ]),
        new(
            "p2.s2",
            2,
            2,
            "2. Compétences techniques et expériences pratiques",
            null,
            [
                Multi("p2.s2.taches", "Quelles tâches ai-je déjà réalisées ?", TachesRealiseesOptions),
                Open("p2.s2.taches.autre", "Autre (tâches réalisées)"),
                Open("p2.s2.capable", "Dans quelles tâches je me sens capable d'exécuter aujourd'hui ?"),
                Open("p2.s2.apprendre", "Quelles tâches ai-je envie d'apprendre ?"),
                Open("p2.s2.outils", "Quels outils ou matériels sais-je utiliser ?"),
                Multi("p2.s2.experience", "Ai-je déjà eu une expérience utile ?", ExperienceOptions),
                Open("p2.s2.experience_appris", "Ce que cette expérience m'a appris"),
            ]),
        new(
            "p2.s3",
            2,
            3,
            "3. Aptitudes naturelles, talents et qualités personnelles",
            null,
            [
                Multi("p2.s3.qualites", "Qualités que je me reconnais", QualitesOptions),
                Open("p2.s3.qualites.autre", "Autre (qualités)"),
                Open("p2.s3.autres_disent", "Ce que les autres disent souvent que je fais bien"),
                Open("p2.s3.utile", "Dans quelles situations je me sens utile"),
                Open("p2.s3.renforcer", "Qualité que je voudrais renforcer"),
                Scale5("p2.s3.progresser", "J'ai le désir de progresser et d'apprendre"),
            ]),
        new(
            "p2.s4",
            2,
            4,
            "4. Préférences de type de travail : intellectuel, manuel, artistique et créatif",
            null,
            [
                Multi("p2.s4.types", "Quels types de travail m'attirent le plus ?", TypeTravailOptions),
                Open("p2.s4.types.autre", "Autre (types de travail)"),
                Scale5("p2.s4.reflechir", "J'aime réfléchir, analyser, comprendre ou résoudre un problème."),
                Scale5("p2.s4.concevoir", "J'aime concevoir, inventer, imaginer ou proposer une idée nouvelle."),
                Scale5("p2.s4.creer_visible", "J'aime créer quelque chose de visible : objet, dessin, décoration, aménagement, solution pratique."),
                Multi("p2.s4.gestes", "Si j'aime le travail manuel, quels gestes ou activités m'attirent le plus ?", GestesManuelsOptions),
                Open("p2.s4.gestes.autre", "Autre (gestes manuels)"),
                Single("p2.s4.mains_preference", "Quand je fais quelque chose avec mes mains, je préfère :",
                    "Suivre un modèle précis", "Trouver ma propre méthode",
                    "Être guidé au début puis essayer seul", "Travailler avec quelqu'un"),
                Open("p2.s4.exemple_activite", "Exemple d'activité où je me sens bien parce que je réfléchis, crée ou utilise mes mains"),
            ]),
        new(
            "p2.s5",
            2,
            5,
            "5. Communication et compréhension des consignes",
            null,
            [
                Scale4("p2.s5.poser_question", "Quand je ne comprends pas, j'ose poser une question."),
                Multi("p2.s5.consignes_forme", "Je préfère recevoir les consignes sous cette forme", ConsignesFormeOptions),
                Single("p2.s5.consignes_moment", "Je préfère recevoir les consignes",
                    "Toutes au début", "Petit à petit"),
                Scale4("p2.s5.reformuler", "Je sais reformuler ce que j'ai compris avant de commencer."),
                Scale4("p2.s5.prevenir", "En cas de retard ou problème, je sais prévenir."),
                Multi("p2.s5.stress_tendance", "Quand je suis stressé, j'ai tendance à...", StressTendanceOptions),
                Open("p2.s5.stress_tendance.autre", "Autre (quand je suis stressé)"),
                Open("p2.s5.aide_communiquer", "Ce qui m'aide à bien communiquer"),
            ]),
        new(
            "p2.s6",
            2,
            6,
            "6. Mode relationnel et rapport aux autres",
            null,
            [
                Single("p2.s6.travail_preference", "Je préfère travailler",
                    "Seul", "Avec une autre personne", "En petit groupe", "Cela dépend"),
                Scale5("p2.s6.presence_rassurante", "J'ai besoin d'une présence rassurante au début d'une mission."),
                Scale5("p2.s6.etre_observe", "Je supporte d'être observé pendant que je travaille."),
                Multi("p2.s6.attitudes_confiance", "Les attitudes qui me mettent en confiance", AttitudesConfianceOptions),
                Open("p2.s6.attitudes_confiance.autre", "Autre (attitudes confiance)"),
                Multi("p2.s6.attitudes_difficulte", "Les attitudes qui me mettent en difficulté", AttitudesDifficulteOptions),
                Open("p2.s6.attitudes_difficulte.autre", "Autre (attitudes difficulté)"),
                Scale5("p2.s6.politesse", "Je reste poli même quand je suis contrarié."),
                Open("p2.s6.meilleur_relation", "Dans quel type de relation je donne le meilleur de moi-même"),
            ]),
        new(
            "p2.s7",
            2,
            7,
            "7. Sens de l'ordre, de la propreté et du soin",
            null,
            [
                Scale5("p2.s7.ranger", "Je range le matériel après l'avoir utilisé."),
                Scale5("p2.s7.remarquer", "Je remarque facilement ce qui est sale, mal rangé ou à terminer."),
                Scale5("p2.s7.precisions", "J'ai besoin qu'on me dise précisément ce qui est attendu."),
                Scale5("p2.s7.verifier", "Je vérifie le résultat avant de dire que c'est fini."),
                Scale5("p2.s7.attention", "Je fais attention aux objets, aux lieux et au matériel des autres."),
                Open("p2.s7.soiigneux", "Tâches dans lesquelles je suis soigneux"),
                Open("p2.s7.progresser", "Tâches dans lesquelles je dois progresser"),
            ]),
        new(
            "p2.s8",
            2,
            8,
            "8. Organisation, planification et gestion du temps",
            null,
            [
                Scale5("p2.s8.preparer", "Je sais préparer ce dont j'ai besoin avant une mission."),
                Scale5("p2.s8.ponctualite", "J'arrive généralement à l'heure."),
                Multi("p2.s8.retard_causes", "Ce qui peut me faire arriver en retard", RetardCausesOptions),
                Open("p2.s8.retard_causes.autre", "Autre (retard)"),
                Multi("p2.s8.outils", "J'utilise des outils pour ne pas oublier mes engagements", OutilsOrganisationOptions),
                Scale5("p2.s8.etapes", "Quand une tâche a plusieurs étapes, je sais par quoi commencer."),
                Multi("p2.s8.aide_org", "J'ai besoin d'aide pour m'organiser sous forme de", AideOrganisationOptions),
            ]),
        new(
            "p2.s9",
            2,
            9,
            "9. Présentation de soi et posture professionnelle",
            null,
            [
                Scale5("p2.s9.presenter", "Je sais me présenter simplement."),
                Scale5("p2.s9.tenue", "Je sais adapter ma tenue à une mission."),
                Scale5("p2.s9.langage", "Je fais attention à mon langage et à mon ton de voix."),
                Scale5("p2.s9.premiere_impression", "Je pense donner une bonne première impression."),
                Open("p2.s9.bonne_impression", "Ce qui peut donner une bonne impression de moi"),
                Open("p2.s9.ameliorer", "Ce que je voudrais améliorer dans ma présentation"),
            ]),
        new(
            "p2.s10",
            2,
            10,
            "10. Valeurs, motivation et rapport au cadre",
            null,
            [
                Multi("p2.s10.valeurs", "Valeurs importantes pour moi", ValeursOptions),
                Open("p2.s10.valeurs.autre", "Autre (valeurs)"),
                Multi("p2.s10.motive", "Ce qui me motive le plus", MotivationProfondeOptions),
                Open("p2.s10.motive.autre", "Autre (motivation)"),
                Scale5("p2.s10.regles", "J'accepte les règles lorsqu'elles sont expliquées clairement."),
                Scale5("p2.s10.erreur", "Je peux reconnaître une erreur ou un oubli."),
                Open("p2.s10.engage", "Ce qui m'aide à rester engagé jusqu'au bout"),
            ]),
        new(
            "p2.s11",
            2,
            11,
            "11. Disponibilités, mobilité et conditions pratiques",
            null,
            [
                Multi("p2.s11.jours", "Jours disponibles", JoursOptions),
                Multi("p2.s11.plages", "Plages horaires possibles", PlagesOptions),
                Single("p2.s11.duree", "Durée maximale confortable",
                    "Moins de 1 h", "1 à 2 h", "2 à 4 h", "Plus de 4 h", "À voir avec le coach"),
                Multi("p2.s11.transport", "Moyens de transport", TransportOptions),
                Open("p2.s11.transport.autre", "Autre (transport)"),
                Single("p2.s11.distance", "Distance maximale acceptée",
                    "Moins de 2 km", "2 à 5 km", "5 à 10 km", "Plus de 10 km", "À définir"),
                Single("p2.s11.accompagnement_trajet", "Besoin d'accompagnement pour le premier trajet",
                    "Oui", "Non", "Peut-être"),
                Multi("p2.s11.contraintes", "Contraintes à signaler", ContraintesOptions),
                Open("p2.s11.contraintes.autre", "Autre (contraintes)"),
            ]),
        new(
            "p2.s12",
            2,
            12,
            "12. Préférences de missions, contextes et limites personnelles",
            null,
            [
                Multi("p2.s12.missions_priorite", "Missions que j'ai envie d'essayer en priorité", MissionsPrioriteOptions),
                Open("p2.s12.missions_priorite.autre", "Autre (missions priorité)"),
                Open("p2.s12.missions_accompagnement", "Missions que j'accepterais avec préparation ou accompagnement"),
                Open("p2.s12.missions_refus", "Missions que je refuse pour le moment"),
                Multi("p2.s12.contextes", "Contextes que je préfère", ContextesPrefOptions),
                Multi("p2.s12.situations_difficulte", "Situations qui peuvent me mettre en difficulté", SituationsDifficulteOptions),
                Open("p2.s12.situations_difficulte.autre", "Autre (situations difficulté)"),
                Open("p2.s12.signes_fatigue", "Signes que je suis fatigué, stressé ou dépassé"),
                Open("p2.s12.aide_coach", "Ce que le coach peut faire pour m'aider"),
            ]),
        ..CategorieASections,
        new(
            "p2.s13",
            2,
            13,
            "13. Synthèse automatique ou semi-automatique pour le coach",
            "Lecture générée à partir des réponses cochées — première version par règles (sans IA). Une synthèse IA pourrait affiner la nuance plus tard, sur validation métier.",
            [],
            JeuneCanEditAnswers: false,
            IsSynthesisDisplayOnly: true),
    ];
}
