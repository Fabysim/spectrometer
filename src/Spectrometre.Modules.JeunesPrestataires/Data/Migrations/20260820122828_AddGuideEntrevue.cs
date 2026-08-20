using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Modules.JeunesPrestataires.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGuideEntrevue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuidesEntrevue",
                schema: "jeunes_prestataires",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JeuneProfileId = table.Column<int>(type: "integer", nullable: false),
                    Motivations = table.Column<string>(type: "text", nullable: true),
                    Freins = table.Column<string>(type: "text", nullable: true),
                    MissionsAdaptees = table.Column<string>(type: "text", nullable: true),
                    NotesConfidentielles = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuidesEntrevue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuideEntrevuePeurNotes",
                schema: "jeunes_prestataires",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuideEntrevueId = table.Column<int>(type: "integer", nullable: false),
                    PeurKey = table.Column<string>(type: "text", nullable: false),
                    NoteCoach = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuideEntrevuePeurNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuideEntrevuePeurNotes_GuidesEntrevue_GuideEntrevueId",
                        column: x => x.GuideEntrevueId,
                        principalSchema: "jeunes_prestataires",
                        principalTable: "GuidesEntrevue",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuideEntrevuePeurNotes_GuideEntrevueId_PeurKey",
                schema: "jeunes_prestataires",
                table: "GuideEntrevuePeurNotes",
                columns: new[] { "GuideEntrevueId", "PeurKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuidesEntrevue_JeuneProfileId",
                schema: "jeunes_prestataires",
                table: "GuidesEntrevue",
                column: "JeuneProfileId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuideEntrevuePeurNotes",
                schema: "jeunes_prestataires");

            migrationBuilder.DropTable(
                name: "GuidesEntrevue",
                schema: "jeunes_prestataires");
        }
    }
}
