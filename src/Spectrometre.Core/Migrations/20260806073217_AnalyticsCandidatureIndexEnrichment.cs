using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spectrometre.Core.Migrations
{
    /// <inheritdoc />
    public partial class AnalyticsCandidatureIndexEnrichment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "GrilleCandidatComplete",
                schema: "core",
                table: "CandidatureIndexEntries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "PointsVigilanceTags",
                schema: "core",
                table: "CandidatureIndexEntries",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<int>(
                name: "ScoreComportementale",
                schema: "core",
                table: "CandidatureIndexEntries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScoreCulturelle",
                schema: "core",
                table: "CandidatureIndexEntries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScoreMotivationnelle",
                schema: "core",
                table: "CandidatureIndexEntries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScoreOrganisationnelle",
                schema: "core",
                table: "CandidatureIndexEntries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScoreTechnique",
                schema: "core",
                table: "CandidatureIndexEntries",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GrilleCandidatComplete",
                schema: "core",
                table: "CandidatureIndexEntries");

            migrationBuilder.DropColumn(
                name: "PointsVigilanceTags",
                schema: "core",
                table: "CandidatureIndexEntries");

            migrationBuilder.DropColumn(
                name: "ScoreComportementale",
                schema: "core",
                table: "CandidatureIndexEntries");

            migrationBuilder.DropColumn(
                name: "ScoreCulturelle",
                schema: "core",
                table: "CandidatureIndexEntries");

            migrationBuilder.DropColumn(
                name: "ScoreMotivationnelle",
                schema: "core",
                table: "CandidatureIndexEntries");

            migrationBuilder.DropColumn(
                name: "ScoreOrganisationnelle",
                schema: "core",
                table: "CandidatureIndexEntries");

            migrationBuilder.DropColumn(
                name: "ScoreTechnique",
                schema: "core",
                table: "CandidatureIndexEntries");
        }
    }
}
