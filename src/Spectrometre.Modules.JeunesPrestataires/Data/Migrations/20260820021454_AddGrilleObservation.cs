using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Modules.JeunesPrestataires.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGrilleObservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GrilleObservationEvaluations",
                schema: "jeunes_prestataires",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JeuneProfileId = table.Column<int>(type: "integer", nullable: false),
                    CoachUserId = table.Column<string>(type: "text", nullable: false),
                    EvalueeLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CommentaireGeneral = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrilleObservationEvaluations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GrilleObservationCriteres",
                schema: "jeunes_prestataires",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EvaluationId = table.Column<int>(type: "integer", nullable: false),
                    CritereKey = table.Column<string>(type: "text", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: true),
                    Commentaire = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrilleObservationCriteres", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GrilleObservationCriteres_GrilleObservationEvaluations_Eval~",
                        column: x => x.EvaluationId,
                        principalSchema: "jeunes_prestataires",
                        principalTable: "GrilleObservationEvaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GrilleObservationCriteres_EvaluationId_CritereKey",
                schema: "jeunes_prestataires",
                table: "GrilleObservationCriteres",
                columns: new[] { "EvaluationId", "CritereKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GrilleObservationEvaluations_JeuneProfileId",
                schema: "jeunes_prestataires",
                table: "GrilleObservationEvaluations",
                column: "JeuneProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_GrilleObservationEvaluations_JeuneProfileId_EvalueeLe",
                schema: "jeunes_prestataires",
                table: "GrilleObservationEvaluations",
                columns: new[] { "JeuneProfileId", "EvalueeLe" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GrilleObservationCriteres",
                schema: "jeunes_prestataires");

            migrationBuilder.DropTable(
                name: "GrilleObservationEvaluations",
                schema: "jeunes_prestataires");
        }
    }
}
