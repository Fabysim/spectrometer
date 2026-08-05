using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Spectrometre.Modules.Compatibilite.Data.Migrations
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
                name: "CompatibilityResults",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CandidateProfileId = table.Column<int>(type: "integer", nullable: false),
                    CompanyProfileId = table.Column<int>(type: "integer", nullable: false),
                    ScoreTechnique = table.Column<int>(type: "integer", nullable: false),
                    ScoreComportementale = table.Column<int>(type: "integer", nullable: false),
                    ScoreCulturelle = table.Column<int>(type: "integer", nullable: false),
                    ScoreOrganisationnelle = table.Column<int>(type: "integer", nullable: false),
                    ScoreMotivationnelle = table.Column<int>(type: "integer", nullable: false),
                    ScoreGlobal = table.Column<int>(type: "integer", nullable: false),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompatibilityResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompatibilityWeightSettings",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Axis = table.Column<int>(type: "integer", nullable: false),
                    WeightPercent = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompatibilityWeightSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompatibilityVigilancePoint",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompatibilityResultId = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompatibilityVigilancePoint", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompatibilityVigilancePoint_CompatibilityResults_Compatibil~",
                        column: x => x.CompatibilityResultId,
                        principalSchema: "public",
                        principalTable: "CompatibilityResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "public",
                table: "CompatibilityWeightSettings",
                columns: new[] { "Id", "Axis", "WeightPercent" },
                values: new object[,]
                {
                    { 1, 0, 20m },
                    { 2, 1, 20m },
                    { 3, 2, 20m },
                    { 4, 3, 20m },
                    { 5, 4, 20m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompatibilityVigilancePoint_CompatibilityResultId",
                schema: "public",
                table: "CompatibilityVigilancePoint",
                column: "CompatibilityResultId");

            migrationBuilder.CreateIndex(
                name: "IX_CompatibilityWeightSettings_Axis",
                schema: "public",
                table: "CompatibilityWeightSettings",
                column: "Axis",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompatibilityVigilancePoint",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CompatibilityWeightSettings",
                schema: "public");

            migrationBuilder.DropTable(
                name: "CompatibilityResults",
                schema: "public");
        }
    }
}
