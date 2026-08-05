using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Spectrometre.Modules.ProfilCandidat.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "profil_candidat");

            migrationBuilder.CreateTable(
                name: "CandidateAnswers",
                schema: "profil_candidat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CandidateProfileId = table.Column<int>(type: "integer", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    AnswerText = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateAnswers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CandidateCompatibilityCriteria",
                schema: "profil_candidat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CandidateProfileId = table.Column<int>(type: "integer", nullable: false),
                    TechniqueText = table.Column<string>(type: "text", nullable: true),
                    ComportementaleText = table.Column<string>(type: "text", nullable: true),
                    CulturelleText = table.Column<string>(type: "text", nullable: true),
                    OrganisationnelleText = table.Column<string>(type: "text", nullable: true),
                    MotivationnelleText = table.Column<string>(type: "text", nullable: true),
                    PointsVigilanceText = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateCompatibilityCriteria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CandidateProfiles",
                schema: "profil_candidat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CandidateQuestions",
                schema: "profil_candidat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Theme = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateQuestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CandidateSynthesisTags",
                schema: "profil_candidat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CandidateProfileId = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateSynthesisTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CandidateQuestionExamples",
                schema: "profil_candidat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateQuestionExamples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateQuestionExamples_CandidateQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalSchema: "profil_candidat",
                        principalTable: "CandidateQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                columns: new[] { "Id", "Number", "Text", "Theme" },
                values: new object[,]
                {
                    { 1, 1, "Quelles activités aimez-vous faire, même lorsqu'elles demandent des efforts ?", 0 },
                    { 2, 2, "Quelles tâches vous donnent de l'énergie au lieu de vous fatiguer rapidement ?", 0 },
                    { 3, 3, "Dans quelles activités perdez-vous parfois la notion du temps ?", 0 },
                    { 4, 4, "Quels sujets, domaines ou problèmes attirent naturellement votre curiosité ?", 0 },
                    { 5, 5, "Quelles choses apprenez-vous plus facilement que d'autres personnes ?", 1 },
                    { 6, 6, "Pour quelles activités les autres vous demandent-ils souvent de l'aide ?", 1 },
                    { 7, 7, "Quelles qualités reviennent souvent dans les compliments que l'on vous fait ?", 1 },
                    { 8, 8, "Quels talents utilisez-vous spontanément, sans toujours les considérer comme importants ?", 1 },
                    { 9, 9, "Quelles compétences avez-vous acquises par les études ?", 2 },
                    { 10, 10, "Quelles compétences avez-vous acquises par les stages, emplois ou expériences pratiques ?", 2 },
                    { 11, 11, "Quelles compétences avez-vous acquises dans la famille, la communauté, les loisirs ou le bénévolat ?", 2 },
                    { 12, 12, "Quelles compétences souhaitez-vous encore développer ?", 2 },
                    { 13, 13, "Qu'est-ce qui est le plus important pour vous dans un travail ?", 3 },
                    { 14, 14, "Quelles causes, missions ou types de problèmes aimeriez-vous contribuer à résoudre ?", 3 },
                    { 15, 15, "Quel type de reconnaissance vous motive le plus ?", 3 },
                    { 16, 16, "Préférez-vous travailler seul, en équipe ou dans un mélange des deux ?", 4 },
                    { 17, 17, "Préférez-vous un travail très structuré ou un travail qui laisse de la liberté d'organisation ?", 4 },
                    { 18, 18, "Préférez-vous un environnement calme, dynamique, compétitif, créatif, social, technique ou administratif ?", 4 },
                    { 19, 19, "Quel rythme de travail vous convient le mieux ?", 4 },
                    { 20, 20, "Quels types de tâches évitez-vous ou vous épuisent rapidement ?", 5 },
                    { 21, 21, "Quelles conditions de travail vous stressent fortement ?", 5 },
                    { 22, 22, "Quels comportements ou styles de gestion vous démotivent ?", 5 },
                    { 23, 23, "Quels domaines ou postes vous intéressent, mais pourraient ne pas correspondre à votre personnalité ?", 5 },
                    { 24, 24, "Quelles activités ou situations vous placent dans un état d'esprit de réalisation, d'acceptation, d'accommodation, de collaboration, d'opposition, de contrainte ou de frustration ?", 5 },
                    { 25, 25, "Quelles situations ou activités provoquent chez vous des émotions de joie, de colère ou de peine/tristesse ?", 5 },
                    { 26, 26, "Quelles activités ou situations vous procurent des sentiments de satisfaction ou d'insatisfaction ?", 5 }
                });

            migrationBuilder.InsertData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                columns: new[] { "Id", "DisplayOrder", "QuestionId", "Text" },
                values: new object[,]
                {
                    { 1, 0, 1, "J'aime organiser des activités, même si cela demande beaucoup de préparation." },
                    { 2, 1, 1, "J'aime réparer des objets, chercher la panne et trouver une solution." },
                    { 3, 2, 1, "J'aime expliquer quelque chose à une autre personne jusqu'à ce qu'elle comprenne." },
                    { 4, 3, 1, "J'aime écrire, lire, analyser ou résumer des informations." },
                    { 5, 4, 1, "J'aime vendre, négocier ou convaincre une personne d'adhérer à une idée." },
                    { 6, 0, 2, "Travailler avec des enfants, des jeunes ou des personnes en difficulté." },
                    { 7, 1, 2, "Résoudre des problèmes techniques ou pratiques." },
                    { 8, 2, 2, "Créer des affiches, des contenus, des plans ou des présentations." },
                    { 9, 3, 2, "Accueillir des clients et répondre à leurs besoins." },
                    { 10, 4, 2, "Coordonner une équipe ou répartir les tâches." },
                    { 11, 0, 3, "Lorsque je dessine, crée, bricole ou conçois quelque chose." },
                    { 12, 1, 3, "Lorsque j'effectue des recherches sur Internet pour comprendre un sujet." },
                    { 13, 2, 3, "Lorsque je cuisine, jardine ou réalise une activité manuelle." },
                    { 14, 3, 3, "Lorsque je programme, calcule ou travaille sur un problème logique." },
                    { 15, 4, 3, "Lorsque je discute avec quelqu'un pour l'écouter ou le conseiller." },
                    { 16, 0, 4, "La santé, le bien-être et l'aide aux personnes." },
                    { 17, 1, 4, "Les machines, l'électricité, l'informatique ou la technologie." },
                    { 18, 2, 4, "L'éducation, la psychologie, la communication ou les relations humaines." },
                    { 19, 3, 4, "Le commerce, l'entrepreneuriat, la gestion ou l'argent." },
                    { 20, 4, 4, "L'environnement, l'agriculture, l'alimentation ou le développement durable." },
                    { 21, 0, 5, "J'apprends facilement les langues." },
                    { 22, 1, 5, "Je comprends vite les chiffres, les calculs ou les tableaux." },
                    { 23, 2, 5, "Je retiens facilement les consignes pratiques." },
                    { 24, 3, 5, "Je comprends rapidement le fonctionnement des appareils ou des outils." },
                    { 25, 4, 5, "J'apprends facilement en observant les autres faire." },
                    { 26, 0, 6, "Pour rédiger un texte, corriger un document ou préparer une présentation." },
                    { 27, 1, 6, "Pour réparer un téléphone, un appareil ou résoudre un problème informatique." },
                    { 28, 2, 6, "Pour organiser une fête, une réunion ou une activité." },
                    { 29, 3, 6, "Pour expliquer une leçon, un devoir ou une procédure." },
                    { 30, 4, 6, "Pour écouter un problème personnel et prodiguer un conseil." },
                    { 31, 0, 7, "On me dit souvent que je suis patient." },
                    { 32, 1, 7, "On me dit que je suis sérieux et fiable." },
                    { 33, 2, 7, "On me reconnaît comme quelqu'un de créatif." },
                    { 34, 3, 7, "On dit que je communique facilement avec les autres." },
                    { 35, 4, 7, "On me dit que je suis courageux et persévérant." },
                    { 36, 0, 8, "Je remarque rapidement lorsqu'une personne ne va pas bien." },
                    { 37, 1, 8, "Je trouve facilement des solutions simples à des problèmes pratiques." },
                    { 38, 2, 8, "Je mets de l'ordre dans les choses sans qu'on me le demande." },
                    { 39, 3, 8, "Je motive les autres lorsqu'ils se découragent." },
                    { 40, 4, 8, "Je mémorise facilement les détails importants." },
                    { 41, 0, 9, "Lire, comprendre et résumer un texte." },
                    { 42, 1, 9, "Utiliser un ordinateur, un logiciel de traitement de texte ou un tableur." },
                    { 43, 2, 9, "Faire des calculs, gérer des données ou interpréter des résultats." },
                    { 44, 3, 9, "Présenter un exposé devant un groupe." },
                    { 45, 4, 9, "Comprendre les bases d'un domaine comme la comptabilité, l'électricité, la mécanique, la santé, l'enseignement." },
                    { 46, 0, 10, "Accueillir des clients et répondre à leurs demandes." },
                    { 47, 1, 10, "Respecter des horaires et des consignes de travail." },
                    { 48, 2, 10, "Travailler en équipe avec des collègues." },
                    { 49, 3, 10, "Utiliser des outils, machines, logiciels ou équipements professionnels." },
                    { 50, 4, 10, "Gérer une petite caisse, un stock, une commande ou un planning." },
                    { 51, 0, 11, "Encadrer des enfants ou des jeunes." },
                    { 52, 1, 11, "Préparer des repas, organiser une activité ou gérer une petite équipe." },
                    { 53, 2, 11, "Participer à des actions communautaires, religieuses, sportives ou sociales." },
                    { 54, 3, 11, "Prendre soin d'une personne âgée, malade ou vulnérable." },
                    { 55, 4, 11, "Gérer un petit commerce familial ou aider dans une activité agricole." },
                    { 56, 0, 12, "Améliorer mon expression orale." },
                    { 57, 1, 12, "Apprendre une langue étrangère." },
                    { 58, 2, 12, "Maîtriser davantage l'informatique." },
                    { 59, 3, 12, "Développer mes compétences en gestion ou en comptabilité." },
                    { 60, 4, 12, "Apprendre à mieux gérer le stress et les priorités." },
                    { 61, 0, 13, "Avoir un travail stable et sécurisant." },
                    { 62, 1, 13, "Aider les autres et me sentir utile." },
                    { 63, 2, 13, "Avoir un bon revenu pour soutenir ma famille." },
                    { 64, 3, 13, "Apprendre constamment et progresser." },
                    { 65, 4, 13, "Avoir de l'autonomie et pouvoir prendre des initiatives." },
                    { 66, 5, 13, "Travailler dans un environnement respectueux." },
                    { 67, 0, 14, "L'éducation des enfants et des jeunes." },
                    { 68, 1, 14, "L'accès aux soins de santé." },
                    { 69, 2, 14, "La pauvreté, le chômage ou l'exclusion sociale." },
                    { 70, 3, 14, "La protection de l'environnement." },
                    { 71, 4, 14, "L'amélioration du service aux clients." },
                    { 72, 5, 14, "La sécurité, l'ordre ou la bonne organisation dans une entreprise." },
                    { 73, 0, 15, "Être encouragé verbalement lorsque j'effectue bien mon travail." },
                    { 74, 1, 15, "Recevoir plus de responsabilités." },
                    { 75, 2, 15, "Voir concrètement les résultats de mes efforts." },
                    { 76, 3, 15, "Progresser vers un meilleur poste." },
                    { 77, 4, 15, "Sentir que je contribue à la réussite d'une équipe." },
                    { 78, 5, 15, "Recevoir une rémunération juste pour le travail accompli." },
                    { 79, 0, 16, "Je préfère travailler en équipe, car j'aime échanger avec les autres." },
                    { 80, 1, 16, "Je préfère travailler seul lorsque je dois me concentrer." },
                    { 81, 2, 16, "J'aime un mélange des deux : travailler seul pour avancer, puis échanger avec l'équipe." },
                    { 82, 3, 16, "Je suis plus efficace lorsque les rôles de chacun sont bien définis." },
                    { 83, 4, 16, "Je préfère un environnement où l'on s'entraide." },
                    { 84, 0, 17, "Je préfère un travail structuré avec des consignes claires." },
                    { 85, 1, 17, "J'aime avoir une certaine liberté pour organiser ma journée." },
                    { 86, 2, 17, "Je suis à l'aise lorsque les objectifs sont clairs, même si la méthode reste flexible." },
                    { 87, 3, 17, "J'ai besoin d'un cadre précis pour éviter la confusion." },
                    { 88, 4, 17, "Je préfère les environnements où l'on peut proposer des idées nouvelles." },
                    { 89, 0, 18, "Je préfère un environnement calme et organisé." },
                    { 90, 1, 18, "Je suis motivé par un environnement dynamique avec beaucoup d'activités." },
                    { 91, 2, 18, "J'aime les milieux sociaux où l'on rencontre beaucoup de personnes." },
                    { 92, 3, 18, "Je préfère un environnement technique où l'on résout des problèmes concrets." },
                    { 93, 4, 18, "Je suis à l'aise dans un cadre administratif avec des documents, des dossiers et des procédures." },
                    { 94, 5, 18, "J'aime les environnements créatifs où l'on peut imaginer et construire quelque chose de nouveau." },
                    { 95, 0, 19, "Un rythme régulier avec des horaires stables." },
                    { 96, 1, 19, "Un rythme rapide avec des défis quotidiens." },
                    { 97, 2, 19, "Un rythme flexible qui me permet de m'organiser." },
                    { 98, 3, 19, "Un travail avec déplacements, car j'aime bouger." },
                    { 99, 4, 19, "Un travail sans déplacements fréquents, car je préfère rester dans un même lieu." },
                    { 100, 5, 19, "Un rythme saisonnier ou variable, si les périodes chargées sont bien organisées." },
                    { 101, 0, 20, "Les tâches très répétitives sans variation." },
                    { 102, 1, 20, "Les tâches qui demandent de parler constamment en public." },
                    { 103, 2, 20, "Les travaux physiques très lourds." },
                    { 104, 3, 20, "Les tâches administratives longues et sans contact humain." },
                    { 105, 4, 20, "Les activités sans objectifs clairs." },
                    { 106, 5, 20, "Les tâches où je dois agir trop vite sans comprendre ce qu'on attend de moi." },
                    { 107, 0, 21, "Les changements de dernière minute sans explication." },
                    { 108, 1, 21, "Le manque de respect ou les conflits fréquents." },
                    { 109, 2, 21, "Les délais trop courts et mal organisés." },
                    { 110, 3, 21, "Le bruit permanent ou l'agitation excessive." },
                    { 111, 4, 21, "Le manque de matériel pour bien effectuer le travail." },
                    { 112, 5, 21, "L'absence de soutien lorsqu'il y a des difficultés." },
                    { 113, 0, 22, "Les critiques humiliantes ou publiques." },
                    { 114, 1, 22, "Le favoritisme." },
                    { 115, 2, 22, "Le manque de communication." },
                    { 116, 3, 22, "Les ordres contradictoires." },
                    { 117, 4, 22, "L'absence de reconnaissance." },
                    { 118, 5, 22, "Le contrôle excessif sans confiance." },
                    { 119, 0, 23, "La vente m'intéresse, mais je ne suis pas encore à l'aise avec la pression des objectifs." },
                    { 120, 1, 23, "Le travail médical m'attire, mais je dois réfléchir à ma capacité à gérer la souffrance." },
                    { 121, 2, 23, "La gestion d'équipe m'intéresse, mais je dois développer mon autorité et ma communication." },
                    { 122, 3, 23, "L'entrepreneuriat m'attire, mais je dois mieux comprendre les risques financiers." },
                    { 123, 4, 23, "L'informatique m'intéresse, mais je dois vérifier si j'aime vraiment travailler longtemps seul devant un écran." },
                    { 124, 0, 24, "Je me sens en réalisation lorsque je termine une tâche utile ou visible." },
                    { 125, 1, 24, "Je me sens en collaboration lorsque les rôles sont clairs et que chacun respecte l'autre." },
                    { 126, 2, 24, "Je ressens de la frustration lorsque je dois travailler sans explication, sans moyens suffisants ou dans un climat de tension." },
                    { 127, 0, 25, "Je ressens de la joie lorsque mon travail aide quelqu'un ou produit un résultat concret." },
                    { 128, 1, 25, "Je ressens de la colère face à l'injustice ou au manque de respect." },
                    { 129, 2, 25, "Je ressens de la tristesse lorsque mes efforts ne sont pas reconnus." },
                    { 130, 0, 26, "Je ressens de la satisfaction quand mon travail a du sens et un résultat visible." },
                    { 131, 1, 26, "Je ressens de l'insatisfaction lorsque mes efforts ne sont pas reconnus ou lorsque mes tâches n'ont pas de sens pour moi." }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateAnswers_CandidateProfileId_QuestionId",
                schema: "profil_candidat",
                table: "CandidateAnswers",
                columns: new[] { "CandidateProfileId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateCompatibilityCriteria_CandidateProfileId",
                schema: "profil_candidat",
                table: "CandidateCompatibilityCriteria",
                column: "CandidateProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProfiles_UserId",
                schema: "profil_candidat",
                table: "CandidateProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateQuestionExamples_QuestionId",
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateQuestions_Number",
                schema: "profil_candidat",
                table: "CandidateQuestions",
                column: "Number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidateAnswers",
                schema: "profil_candidat");

            migrationBuilder.DropTable(
                name: "CandidateCompatibilityCriteria",
                schema: "profil_candidat");

            migrationBuilder.DropTable(
                name: "CandidateProfiles",
                schema: "profil_candidat");

            migrationBuilder.DropTable(
                name: "CandidateQuestionExamples",
                schema: "profil_candidat");

            migrationBuilder.DropTable(
                name: "CandidateSynthesisTags",
                schema: "profil_candidat");

            migrationBuilder.DropTable(
                name: "CandidateQuestions",
                schema: "profil_candidat");
        }
    }
}
