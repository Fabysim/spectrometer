using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Spectrometre.Modules.Entretien.Data.Migrations
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
                name: "InterviewGenerationSettings",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeuilAxeFaiblePercent = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewGenerationSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuestionTemplates",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Axis = table.Column<int>(type: "integer", nullable: true),
                    Sens = table.Column<int>(type: "integer", nullable: false),
                    Gabarit = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionTemplates", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "public",
                table: "InterviewGenerationSettings",
                columns: new[] { "Id", "SeuilAxeFaiblePercent" },
                values: new object[] { 1, 60 });

            migrationBuilder.InsertData(
                schema: "public",
                table: "QuestionTemplates",
                columns: new[] { "Id", "Axis", "DisplayOrder", "Gabarit", "Sens", "Type" },
                values: new object[,]
                {
                    { 1, 0, 0, "Quelles sont, selon vous, les compétences techniques qui vous manquent encore pour être pleinement opérationnel sur ce poste ?", 0, 0 },
                    { 2, 0, 0, "Quel accompagnement ou quelle formation l'entreprise prévoit-elle pour combler un éventuel écart de compétences techniques ?", 1, 0 },
                    { 3, 1, 0, "Pouvez-vous décrire une situation récente où votre façon de travailler au quotidien a été mise à l'épreuve ?", 0, 0 },
                    { 4, 1, 0, "Quels comportements professionnels l'entreprise valorise-t-elle le plus au quotidien, au-delà de ce qui est affiché ?", 1, 0 },
                    { 5, 2, 0, "Qu'est-ce qui, dans la culture d'une entreprise, vous a déjà mis mal à l'aise par le passé ?", 0, 0 },
                    { 6, 2, 0, "Comment la culture d'entreprise affichée se traduit-elle concrètement dans les décisions du quotidien ?", 1, 0 },
                    { 7, 3, 0, "Vous avez indiqué tolérer un rythme « {rythmeCandidat} », alors que ce poste est annoncé avec un rythme « {rythmeEntreprise} » — comment envisagez-vous cet écart au quotidien ?", 0, 0 },
                    { 8, 3, 0, "Le poste est annoncé avec un rythme « {rythmeEntreprise} » — à quoi ressemble concrètement une semaine type pour quelqu'un qui tolère plutôt un rythme « {rythmeCandidat} » ?", 1, 0 },
                    { 9, 4, 0, "Qu'est-ce qui vous ferait perdre votre motivation le plus rapidement dans ce poste ?", 0, 0 },
                    { 10, 4, 0, "Qu'est-ce que l'entreprise met concrètement en place pour nourrir la motivation de ses équipes ?", 1, 0 },
                    { 11, null, 0, "Vous avez signalé « {tag} » comme un point de vigilance potentiel — pouvez-vous préciser ce que cela représente concrètement pour vous, et comment vous l'avez géré par le passé ?", 0, 1 },
                    { 12, null, 0, "L'entreprise a également identifié « {tag} » comme un point de vigilance — comment cela se manifeste-t-il concrètement au quotidien dans l'équipe ?", 1, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionTemplates_Type_Axis_Sens_DisplayOrder",
                schema: "public",
                table: "QuestionTemplates",
                columns: new[] { "Type", "Axis", "Sens", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterviewGenerationSettings",
                schema: "public");

            migrationBuilder.DropTable(
                name: "QuestionTemplates",
                schema: "public");
        }
    }
}
