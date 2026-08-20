using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Modules.JeunesPrestataires.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoObservationQuestionnaire : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AutoObservationReponses",
                schema: "jeunes_prestataires",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JeuneProfileId = table.Column<int>(type: "integer", nullable: false),
                    QuestionKey = table.Column<string>(type: "text", nullable: false),
                    TextValue = table.Column<string>(type: "text", nullable: true),
                    NumericValue = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoObservationReponses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AutoObservationSectionProgress",
                schema: "jeunes_prestataires",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JeuneProfileId = table.Column<int>(type: "integer", nullable: false),
                    SectionKey = table.Column<string>(type: "text", nullable: false),
                    SavedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoObservationSectionProgress", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AutoObservationSynthesesGenerees",
                schema: "jeunes_prestataires",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JeuneProfileId = table.Column<int>(type: "integer", nullable: false),
                    Contenu = table.Column<string>(type: "text", nullable: false),
                    GenereeLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoObservationSynthesesGenerees", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutoObservationReponses_JeuneProfileId_QuestionKey",
                schema: "jeunes_prestataires",
                table: "AutoObservationReponses",
                columns: new[] { "JeuneProfileId", "QuestionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AutoObservationSectionProgress_JeuneProfileId_SectionKey",
                schema: "jeunes_prestataires",
                table: "AutoObservationSectionProgress",
                columns: new[] { "JeuneProfileId", "SectionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AutoObservationSynthesesGenerees_JeuneProfileId",
                schema: "jeunes_prestataires",
                table: "AutoObservationSynthesesGenerees",
                column: "JeuneProfileId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutoObservationReponses",
                schema: "jeunes_prestataires");

            migrationBuilder.DropTable(
                name: "AutoObservationSectionProgress",
                schema: "jeunes_prestataires");

            migrationBuilder.DropTable(
                name: "AutoObservationSynthesesGenerees",
                schema: "jeunes_prestataires");
        }
    }
}
