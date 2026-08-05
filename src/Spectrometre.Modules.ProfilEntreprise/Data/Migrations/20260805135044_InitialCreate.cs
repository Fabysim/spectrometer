using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Spectrometre.Modules.ProfilEntreprise.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "CompanyAnswers",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyProfileId = table.Column<int>(type: "integer", nullable: false),
                    QuestionId = table.Column<int>(type: "integer", nullable: false),
                    AnswerText = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyAnswers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyCompatibilityCriteria",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyProfileId = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_CompanyCompatibilityCriteria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyProfiles",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyQuestions",
                schema: "public",
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
                    table.PrimaryKey("PK_CompanyQuestions", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "public",
                table: "CompanyQuestions",
                columns: new[] { "Id", "Number", "Text", "Theme" },
                values: new object[,]
                {
                    { 1, 1, "Nom de l'entreprise ou de l'organisation", 0 },
                    { 2, 2, "Secteur d'activité principal", 0 },
                    { 3, 3, "Localisation principale et zones d'intervention", 0 },
                    { 4, 4, "Taille de l'entreprise : petite, moyenne, grande, groupe, association, institution", 0 },
                    { 5, 5, "Ancienneté ou année de création", 0 },
                    { 6, 6, "Types de postes généralement proposés", 0 },
                    { 7, 7, "Quelle est la mission principale de l'entreprise ?", 1 },
                    { 8, 8, "À quel besoin de la société, du marché ou de la communauté l'entreprise cherche-t-elle à répondre ?", 1 },
                    { 9, 9, "Quelle vision l'entreprise poursuit-elle à moyen ou long terme ?", 1 },
                    { 10, 10, "Qu'est-ce qui rend l'entreprise utile, différente ou importante dans son secteur ?", 1 },
                    { 11, 11, "Quels problèmes l'entreprise souhaite-t-elle contribuer à résoudre ?", 1 },
                    { 12, 12, "Quelles sont les trois à cinq valeurs principales que l'entreprise souhaite incarner ?", 2 },
                    { 13, 13, "Comment ces valeurs se traduisent-elles dans les décisions quotidiennes ?", 2 },
                    { 14, 14, "Quelles valeurs sont attendues chez les collaborateurs ?", 2 },
                    { 15, 15, "Quels comportements sont encouragés parce qu'ils correspondent à la culture de l'entreprise ?", 2 },
                    { 16, 16, "Quels comportements sont incompatibles avec l'esprit de l'entreprise ?", 2 },
                    { 17, 17, "Comment décririez-vous le climat général de travail : calme, dynamique, exigeant, familial, compétitif, créatif, structuré, flexible ?", 3 },
                    { 18, 18, "Le travail est-il plutôt individuel, collectif ou mixte ?", 3 },
                    { 19, 19, "Quelle place occupent les règles, procédures et consignes dans l'organisation du travail ?", 3 },
                    { 20, 20, "Quelle place est laissée à l'initiative, à l'autonomie et à la créativité ?", 3 },
                    { 21, 21, "Comment l'entreprise gère-t-elle les périodes de pression, d'urgence ou de forte activité ?", 3 },
                    { 22, 22, "Quel style de leadership domine dans l'entreprise : directif, participatif, collaboratif, transformationnel, paternaliste, délégatif, orienté résultats ?", 4 },
                    { 23, 23, "Comment les responsables donnent-ils les consignes et suivent-ils le travail ?", 4 },
                    { 24, 24, "Quelle place les employés ont-ils dans la prise de décision ?", 4 },
                    { 25, 25, "Comment les responsables accompagnent-ils les nouveaux collaborateurs ?", 4 },
                    { 26, 26, "Comment les erreurs sont-elles traitées : sanction, apprentissage, correction, accompagnement, discussion ?", 4 },
                    { 27, 27, "Quel type de collaborateur réussit le mieux sous ce mode de leadership ?", 4 },
                    { 28, 28, "Comment les collaborateurs communiquent-ils entre eux : oralement, par écrit, en réunions, par messagerie, par rapports ?", 5 },
                    { 29, 29, "Le climat relationnel est-il plutôt formel, informel, hiérarchique, familial, direct, diplomatique ou réservé ?", 5 },
                    { 30, 30, "Comment les désaccords ou conflits sont-ils généralement gérés ?", 5 },
                    { 31, 31, "Quelle importance l'entreprise accorde-t-elle à l'écoute, au respect, à la politesse et à la coopération ?", 5 },
                    { 32, 32, "Quels comportements relationnels sont particulièrement appréciés dans l'entreprise ?", 5 },
                    { 33, 33, "Quels comportements relationnels créent des difficultés dans l'entreprise ?", 5 },
                    { 34, 34, "Comment l'entreprise reconnaît-elle les efforts et les bons résultats ?", 6 },
                    { 35, 35, "Quels types de motivation sont les plus présents : salaire, responsabilité, reconnaissance, progression, stabilité, autonomie, esprit d'équipe ?", 6 },
                    { 36, 36, "Quelles possibilités d'apprentissage, de formation ou d'évolution sont proposées ?", 6 },
                    { 37, 37, "Comment l'entreprise accompagne-t-elle les personnes qui veulent progresser ?", 6 },
                    { 38, 38, "Quels signes montrent qu'un collaborateur est bien intégré et apprécié dans l'entreprise ?", 6 },
                    { 39, 39, "Quels sont les horaires habituels et le rythme de travail ?", 7 },
                    { 40, 40, "Le travail exige-t-il des déplacements, de la mobilité, une disponibilité particulière ou des horaires variables ?", 7 },
                    { 41, 41, "Quelles sont les principales contraintes physiques, psychologiques, relationnelles ou organisationnelles ?", 7 },
                    { 42, 42, "Quel niveau d'autonomie est attendu du collaborateur ?", 7 },
                    { 43, 43, "Quel niveau de pression, de rapidité ou de précision le travail demande-t-il ?", 7 },
                    { 44, 44, "Quels moyens l'entreprise met-elle à disposition pour permettre de bien travailler ?", 7 },
                    { 45, 45, "Quelles qualités personnelles permettent de bien réussir dans l'entreprise ?", 8 },
                    { 46, 46, "Quelles compétences techniques ou professionnelles sont les plus recherchées ?", 8 },
                    { 47, 47, "Quel type de personnalité s'intègre facilement dans l'équipe ?", 8 },
                    { 48, 48, "Quel type de candidat pourrait rencontrer des difficultés dans cet environnement ?", 8 },
                    { 49, 49, "Quelles valeurs personnelles du candidat doivent être compatibles avec celles de l'entreprise ?", 8 },
                    { 50, 50, "Notre entreprise est principalement caractérisée par...", 9 },
                    { 51, 51, "Nos trois valeurs les plus importantes sont...", 9 },
                    { 52, 52, "Notre style de leadership est plutôt...", 9 },
                    { 53, 53, "Notre mode relationnel est plutôt...", 9 },
                    { 54, 54, "Les collaborateurs qui réussissent le mieux chez nous sont ceux qui...", 9 },
                    { 55, 55, "Les candidats doivent être particulièrement attentifs à...", 9 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyAnswers_CompanyProfileId_QuestionId",
                schema: "public",
                table: "CompanyAnswers",
                columns: new[] { "CompanyProfileId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyCompatibilityCriteria_CompanyProfileId",
                schema: "public",
                table: "CompanyCompatibilityCriteria",
                column: "CompanyProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyQuestions_Number",
                schema: "public",
                table: "CompanyQuestions",
                column: "Number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyAnswers",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CompanyCompatibilityCriteria",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CompanyProfiles",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CompanyQuestions",
                schema: "public");
        }
    }
}
