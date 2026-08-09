using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Modules.SuiviEmployes.Data.Migrations
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
                name: "AnalysesIaEmploye",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserCompanyLinkId = table.Column<int>(type: "integer", nullable: false),
                    DataHash = table.Column<string>(type: "text", nullable: false),
                    AnalyseMarkdown = table.Column<string>(type: "text", nullable: false),
                    GenereeLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EnCours = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysesIaEmploye", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationsEmploye",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserCompanyLinkId = table.Column<int>(type: "integer", nullable: false),
                    PosteId = table.Column<int>(type: "integer", nullable: false),
                    CritereId = table.Column<int>(type: "integer", nullable: false),
                    ScoreActuel = table.Column<int>(type: "integer", nullable: false),
                    ScoreSouhaite = table.Column<int>(type: "integer", nullable: false),
                    EvaluationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DaySequence = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationsEmploye", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationsEmployeCloturees",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserCompanyLinkId = table.Column<int>(type: "integer", nullable: false),
                    PosteId = table.Column<int>(type: "integer", nullable: false),
                    EvaluationDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationsEmployeCloturees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationsObjectifs",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserCompanyLinkId = table.Column<int>(type: "integer", nullable: false),
                    DateDebut = table.Column<DateOnly>(type: "date", nullable: false),
                    DateFin = table.Column<DateOnly>(type: "date", nullable: false),
                    Archivee = table.Column<bool>(type: "boolean", nullable: false),
                    EvaluateurUserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationsObjectifs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ValidationsSocioProEmploye",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserCompanyLinkId = table.Column<int>(type: "integer", nullable: false),
                    PosteId = table.Column<int>(type: "integer", nullable: false),
                    ValidatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationsSocioProEmploye", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Objectifs",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EvaluationObjectifsId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Titre = table.Column<string>(type: "text", nullable: false),
                    Moyens = table.Column<string>(type: "text", nullable: true),
                    Atteinte = table.Column<int>(type: "integer", nullable: false),
                    Observation = table.Column<string>(type: "text", nullable: true),
                    Note = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Objectifs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Objectifs_EvaluationsObjectifs_EvaluationObjectifsId",
                        column: x => x.EvaluationObjectifsId,
                        principalSchema: "public",
                        principalTable: "EvaluationsObjectifs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysesIaEmploye_UserCompanyLinkId_DataHash",
                schema: "public",
                table: "AnalysesIaEmploye",
                columns: new[] { "UserCompanyLinkId", "DataHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationsEmploye_UserCompanyLinkId_CritereId_EvaluationDa~",
                schema: "public",
                table: "EvaluationsEmploye",
                columns: new[] { "UserCompanyLinkId", "CritereId", "EvaluationDate", "DaySequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationsEmploye_UserCompanyLinkId_PosteId_EvaluationDate",
                schema: "public",
                table: "EvaluationsEmploye",
                columns: new[] { "UserCompanyLinkId", "PosteId", "EvaluationDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationsEmployeCloturees_UserCompanyLinkId_PosteId_Evalu~",
                schema: "public",
                table: "EvaluationsEmployeCloturees",
                columns: new[] { "UserCompanyLinkId", "PosteId", "EvaluationDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationsObjectifs_UserCompanyLinkId_DateDebut_DateFin",
                schema: "public",
                table: "EvaluationsObjectifs",
                columns: new[] { "UserCompanyLinkId", "DateDebut", "DateFin" });

            migrationBuilder.CreateIndex(
                name: "IX_Objectifs_EvaluationObjectifsId",
                schema: "public",
                table: "Objectifs",
                column: "EvaluationObjectifsId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationsSocioProEmploye_UserCompanyLinkId_PosteId",
                schema: "public",
                table: "ValidationsSocioProEmploye",
                columns: new[] { "UserCompanyLinkId", "PosteId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalysesIaEmploye",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EvaluationsEmploye",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EvaluationsEmployeCloturees",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Objectifs",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ValidationsSocioProEmploye",
                schema: "public");

            migrationBuilder.DropTable(
                name: "EvaluationsObjectifs",
                schema: "public");
        }
    }
}
