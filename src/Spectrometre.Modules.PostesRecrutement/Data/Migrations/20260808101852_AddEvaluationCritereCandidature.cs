using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Modules.PostesRecrutement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluationCritereCandidature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvaluationsCriteresCandidature",
                schema: "public");
        }
    }
}
