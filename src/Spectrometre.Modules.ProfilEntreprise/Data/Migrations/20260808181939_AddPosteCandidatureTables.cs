using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Modules.ProfilEntreprise.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPosteCandidatureTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Candidatures",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PosteId = table.Column<int>(type: "integer", nullable: false),
                    CandidateProfileId = table.Column<int>(type: "integer", nullable: false),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    EstPreselectionne = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Candidatures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CriteresEvaluation",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PosteId = table.Column<int>(type: "integer", nullable: false),
                    Categorie = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Libelle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NiveauRequis = table.Column<int>(type: "integer", nullable: false),
                    OrdreAffichage = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriteresEvaluation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Postes",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Titre = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Departement = table.Column<string>(type: "text", nullable: true),
                    TachesDescription = table.Column<string>(type: "text", nullable: true),
                    Salaire = table.Column<string>(type: "text", nullable: true),
                    Avantages = table.Column<string>(type: "text", nullable: true),
                    DateCloture = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Postes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationsCriteresCandidature",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CandidatureId = table.Column<int>(type: "integer", nullable: false),
                    CritereId = table.Column<int>(type: "integer", nullable: false),
                    NiveauFinal = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationsCriteresCandidature", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluationsCriteresCandidature_Candidatures_CandidatureId",
                        column: x => x.CandidatureId,
                        principalSchema: "public",
                        principalTable: "Candidatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EvaluationsCriteresCandidature_CriteresEvaluation_CritereId",
                        column: x => x.CritereId,
                        principalSchema: "public",
                        principalTable: "CriteresEvaluation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GenerationsCriteresIaPoste",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PosteId = table.Column<int>(type: "integer", nullable: false),
                    HashContexte = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    GenereeLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GenereeParIa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenerationsCriteresIaPoste", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GenerationsCriteresIaPoste_Postes_PosteId",
                        column: x => x.PosteId,
                        principalSchema: "public",
                        principalTable: "Postes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Candidatures_PosteId_CandidateProfileId",
                schema: "public",
                table: "Candidatures",
                columns: new[] { "PosteId", "CandidateProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CriteresEvaluation_PosteId_OrdreAffichage",
                schema: "public",
                table: "CriteresEvaluation",
                columns: new[] { "PosteId", "OrdreAffichage" });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationsCriteresCandidature_CandidatureId_CritereId",
                schema: "public",
                table: "EvaluationsCriteresCandidature",
                columns: new[] { "CandidatureId", "CritereId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationsCriteresCandidature_CritereId",
                schema: "public",
                table: "EvaluationsCriteresCandidature",
                column: "CritereId");

            migrationBuilder.CreateIndex(
                name: "IX_GenerationsCriteresIaPoste_PosteId",
                schema: "public",
                table: "GenerationsCriteresIaPoste",
                column: "PosteId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvaluationsCriteresCandidature",
                schema: "public");

            migrationBuilder.DropTable(
                name: "GenerationsCriteresIaPoste",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Candidatures",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CriteresEvaluation",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Postes",
                schema: "public");
        }
    }
}
