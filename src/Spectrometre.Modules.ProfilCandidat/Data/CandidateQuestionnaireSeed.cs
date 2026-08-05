using Spectrometre.Modules.ProfilCandidat.Entities;

namespace Spectrometre.Modules.ProfilCandidat.Data;

/// <summary>
/// Catalogue des 26 questions du questionnaire « Connaissance de soi et de son potentiel » (sections A à F),
/// texte repris tel quel du document « Questionnaire de profil socioprofessionnel de l'entreprise ».
/// </summary>
internal static class CandidateQuestionnaireSeed
{
    public sealed record SeedQuestion(int Number, CandidateTheme Theme, string Text, string[] Examples);

    public static readonly SeedQuestion[] Questions =
    [
        new(1, CandidateTheme.ActivitesInterets, "Quelles activités aimez-vous faire, même lorsqu'elles demandent des efforts ?",
        [
            "J'aime organiser des activités, même si cela demande beaucoup de préparation.",
            "J'aime réparer des objets, chercher la panne et trouver une solution.",
            "J'aime expliquer quelque chose à une autre personne jusqu'à ce qu'elle comprenne.",
            "J'aime écrire, lire, analyser ou résumer des informations.",
            "J'aime vendre, négocier ou convaincre une personne d'adhérer à une idée.",
        ]),
        new(2, CandidateTheme.ActivitesInterets, "Quelles tâches vous donnent de l'énergie au lieu de vous fatiguer rapidement ?",
        [
            "Travailler avec des enfants, des jeunes ou des personnes en difficulté.",
            "Résoudre des problèmes techniques ou pratiques.",
            "Créer des affiches, des contenus, des plans ou des présentations.",
            "Accueillir des clients et répondre à leurs besoins.",
            "Coordonner une équipe ou répartir les tâches.",
        ]),
        new(3, CandidateTheme.ActivitesInterets, "Dans quelles activités perdez-vous parfois la notion du temps ?",
        [
            "Lorsque je dessine, crée, bricole ou conçois quelque chose.",
            "Lorsque j'effectue des recherches sur Internet pour comprendre un sujet.",
            "Lorsque je cuisine, jardine ou réalise une activité manuelle.",
            "Lorsque je programme, calcule ou travaille sur un problème logique.",
            "Lorsque je discute avec quelqu'un pour l'écouter ou le conseiller.",
        ]),
        new(4, CandidateTheme.ActivitesInterets, "Quels sujets, domaines ou problèmes attirent naturellement votre curiosité ?",
        [
            "La santé, le bien-être et l'aide aux personnes.",
            "Les machines, l'électricité, l'informatique ou la technologie.",
            "L'éducation, la psychologie, la communication ou les relations humaines.",
            "Le commerce, l'entrepreneuriat, la gestion ou l'argent.",
            "L'environnement, l'agriculture, l'alimentation ou le développement durable.",
        ]),

        new(5, CandidateTheme.TalentsNaturels, "Quelles choses apprenez-vous plus facilement que d'autres personnes ?",
        [
            "J'apprends facilement les langues.",
            "Je comprends vite les chiffres, les calculs ou les tableaux.",
            "Je retiens facilement les consignes pratiques.",
            "Je comprends rapidement le fonctionnement des appareils ou des outils.",
            "J'apprends facilement en observant les autres faire.",
        ]),
        new(6, CandidateTheme.TalentsNaturels, "Pour quelles activités les autres vous demandent-ils souvent de l'aide ?",
        [
            "Pour rédiger un texte, corriger un document ou préparer une présentation.",
            "Pour réparer un téléphone, un appareil ou résoudre un problème informatique.",
            "Pour organiser une fête, une réunion ou une activité.",
            "Pour expliquer une leçon, un devoir ou une procédure.",
            "Pour écouter un problème personnel et prodiguer un conseil.",
        ]),
        new(7, CandidateTheme.TalentsNaturels, "Quelles qualités reviennent souvent dans les compliments que l'on vous fait ?",
        [
            "On me dit souvent que je suis patient.",
            "On me dit que je suis sérieux et fiable.",
            "On me reconnaît comme quelqu'un de créatif.",
            "On dit que je communique facilement avec les autres.",
            "On me dit que je suis courageux et persévérant.",
        ]),
        new(8, CandidateTheme.TalentsNaturels, "Quels talents utilisez-vous spontanément, sans toujours les considérer comme importants ?",
        [
            "Je remarque rapidement lorsqu'une personne ne va pas bien.",
            "Je trouve facilement des solutions simples à des problèmes pratiques.",
            "Je mets de l'ordre dans les choses sans qu'on me le demande.",
            "Je motive les autres lorsqu'ils se découragent.",
            "Je mémorise facilement les détails importants.",
        ]),

        new(9, CandidateTheme.CompetencesAcquises, "Quelles compétences avez-vous acquises par les études ?",
        [
            "Lire, comprendre et résumer un texte.",
            "Utiliser un ordinateur, un logiciel de traitement de texte ou un tableur.",
            "Faire des calculs, gérer des données ou interpréter des résultats.",
            "Présenter un exposé devant un groupe.",
            "Comprendre les bases d'un domaine comme la comptabilité, l'électricité, la mécanique, la santé, l'enseignement.",
        ]),
        new(10, CandidateTheme.CompetencesAcquises, "Quelles compétences avez-vous acquises par les stages, emplois ou expériences pratiques ?",
        [
            "Accueillir des clients et répondre à leurs demandes.",
            "Respecter des horaires et des consignes de travail.",
            "Travailler en équipe avec des collègues.",
            "Utiliser des outils, machines, logiciels ou équipements professionnels.",
            "Gérer une petite caisse, un stock, une commande ou un planning.",
        ]),
        new(11, CandidateTheme.CompetencesAcquises, "Quelles compétences avez-vous acquises dans la famille, la communauté, les loisirs ou le bénévolat ?",
        [
            "Encadrer des enfants ou des jeunes.",
            "Préparer des repas, organiser une activité ou gérer une petite équipe.",
            "Participer à des actions communautaires, religieuses, sportives ou sociales.",
            "Prendre soin d'une personne âgée, malade ou vulnérable.",
            "Gérer un petit commerce familial ou aider dans une activité agricole.",
        ]),
        new(12, CandidateTheme.CompetencesAcquises, "Quelles compétences souhaitez-vous encore développer ?",
        [
            "Améliorer mon expression orale.",
            "Apprendre une langue étrangère.",
            "Maîtriser davantage l'informatique.",
            "Développer mes compétences en gestion ou en comptabilité.",
            "Apprendre à mieux gérer le stress et les priorités.",
        ]),

        new(13, CandidateTheme.ValeursTravail, "Qu'est-ce qui est le plus important pour vous dans un travail ?",
        [
            "Avoir un travail stable et sécurisant.",
            "Aider les autres et me sentir utile.",
            "Avoir un bon revenu pour soutenir ma famille.",
            "Apprendre constamment et progresser.",
            "Avoir de l'autonomie et pouvoir prendre des initiatives.",
            "Travailler dans un environnement respectueux.",
        ]),
        new(14, CandidateTheme.ValeursTravail, "Quelles causes, missions ou types de problèmes aimeriez-vous contribuer à résoudre ?",
        [
            "L'éducation des enfants et des jeunes.",
            "L'accès aux soins de santé.",
            "La pauvreté, le chômage ou l'exclusion sociale.",
            "La protection de l'environnement.",
            "L'amélioration du service aux clients.",
            "La sécurité, l'ordre ou la bonne organisation dans une entreprise.",
        ]),
        new(15, CandidateTheme.ValeursTravail, "Quel type de reconnaissance vous motive le plus ?",
        [
            "Être encouragé verbalement lorsque j'effectue bien mon travail.",
            "Recevoir plus de responsabilités.",
            "Voir concrètement les résultats de mes efforts.",
            "Progresser vers un meilleur poste.",
            "Sentir que je contribue à la réussite d'une équipe.",
            "Recevoir une rémunération juste pour le travail accompli.",
        ]),

        new(16, CandidateTheme.EnvironnementsFavorables, "Préférez-vous travailler seul, en équipe ou dans un mélange des deux ?",
        [
            "Je préfère travailler en équipe, car j'aime échanger avec les autres.",
            "Je préfère travailler seul lorsque je dois me concentrer.",
            "J'aime un mélange des deux : travailler seul pour avancer, puis échanger avec l'équipe.",
            "Je suis plus efficace lorsque les rôles de chacun sont bien définis.",
            "Je préfère un environnement où l'on s'entraide.",
        ]),
        new(17, CandidateTheme.EnvironnementsFavorables, "Préférez-vous un travail très structuré ou un travail qui laisse de la liberté d'organisation ?",
        [
            "Je préfère un travail structuré avec des consignes claires.",
            "J'aime avoir une certaine liberté pour organiser ma journée.",
            "Je suis à l'aise lorsque les objectifs sont clairs, même si la méthode reste flexible.",
            "J'ai besoin d'un cadre précis pour éviter la confusion.",
            "Je préfère les environnements où l'on peut proposer des idées nouvelles.",
        ]),
        new(18, CandidateTheme.EnvironnementsFavorables, "Préférez-vous un environnement calme, dynamique, compétitif, créatif, social, technique ou administratif ?",
        [
            "Je préfère un environnement calme et organisé.",
            "Je suis motivé par un environnement dynamique avec beaucoup d'activités.",
            "J'aime les milieux sociaux où l'on rencontre beaucoup de personnes.",
            "Je préfère un environnement technique où l'on résout des problèmes concrets.",
            "Je suis à l'aise dans un cadre administratif avec des documents, des dossiers et des procédures.",
            "J'aime les environnements créatifs où l'on peut imaginer et construire quelque chose de nouveau.",
        ]),
        new(19, CandidateTheme.EnvironnementsFavorables, "Quel rythme de travail vous convient le mieux ?",
        [
            "Un rythme régulier avec des horaires stables.",
            "Un rythme rapide avec des défis quotidiens.",
            "Un rythme flexible qui me permet de m'organiser.",
            "Un travail avec déplacements, car j'aime bouger.",
            "Un travail sans déplacements fréquents, car je préfère rester dans un même lieu.",
            "Un rythme saisonnier ou variable, si les périodes chargées sont bien organisées.",
        ]),

        new(20, CandidateTheme.SignauxAlerte, "Quels types de tâches évitez-vous ou vous épuisent rapidement ?",
        [
            "Les tâches très répétitives sans variation.",
            "Les tâches qui demandent de parler constamment en public.",
            "Les travaux physiques très lourds.",
            "Les tâches administratives longues et sans contact humain.",
            "Les activités sans objectifs clairs.",
            "Les tâches où je dois agir trop vite sans comprendre ce qu'on attend de moi.",
        ]),
        new(21, CandidateTheme.SignauxAlerte, "Quelles conditions de travail vous stressent fortement ?",
        [
            "Les changements de dernière minute sans explication.",
            "Le manque de respect ou les conflits fréquents.",
            "Les délais trop courts et mal organisés.",
            "Le bruit permanent ou l'agitation excessive.",
            "Le manque de matériel pour bien effectuer le travail.",
            "L'absence de soutien lorsqu'il y a des difficultés.",
        ]),
        new(22, CandidateTheme.SignauxAlerte, "Quels comportements ou styles de gestion vous démotivent ?",
        [
            "Les critiques humiliantes ou publiques.",
            "Le favoritisme.",
            "Le manque de communication.",
            "Les ordres contradictoires.",
            "L'absence de reconnaissance.",
            "Le contrôle excessif sans confiance.",
        ]),
        new(23, CandidateTheme.SignauxAlerte, "Quels domaines ou postes vous intéressent, mais pourraient ne pas correspondre à votre personnalité ?",
        [
            "La vente m'intéresse, mais je ne suis pas encore à l'aise avec la pression des objectifs.",
            "Le travail médical m'attire, mais je dois réfléchir à ma capacité à gérer la souffrance.",
            "La gestion d'équipe m'intéresse, mais je dois développer mon autorité et ma communication.",
            "L'entrepreneuriat m'attire, mais je dois mieux comprendre les risques financiers.",
            "L'informatique m'intéresse, mais je dois vérifier si j'aime vraiment travailler longtemps seul devant un écran.",
        ]),
        new(24, CandidateTheme.SignauxAlerte,
            "Quelles activités ou situations vous placent dans un état d'esprit de réalisation, d'acceptation, d'accommodation, de collaboration, d'opposition, de contrainte ou de frustration ?",
        [
            "Je me sens en réalisation lorsque je termine une tâche utile ou visible.",
            "Je me sens en collaboration lorsque les rôles sont clairs et que chacun respecte l'autre.",
            "Je ressens de la frustration lorsque je dois travailler sans explication, sans moyens suffisants ou dans un climat de tension.",
        ]),
        new(25, CandidateTheme.SignauxAlerte, "Quelles situations ou activités provoquent chez vous des émotions de joie, de colère ou de peine/tristesse ?",
        [
            "Je ressens de la joie lorsque mon travail aide quelqu'un ou produit un résultat concret.",
            "Je ressens de la colère face à l'injustice ou au manque de respect.",
            "Je ressens de la tristesse lorsque mes efforts ne sont pas reconnus.",
        ]),
        new(26, CandidateTheme.SignauxAlerte, "Quelles activités ou situations vous procurent des sentiments de satisfaction ou d'insatisfaction ?",
        [
            "Je ressens de la satisfaction quand mon travail a du sens et un résultat visible.",
            "Je ressens de l'insatisfaction lorsque mes efforts ne sont pas reconnus ou lorsque mes tâches n'ont pas de sens pour moi.",
        ]),
    ];
}
