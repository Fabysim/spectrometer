using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Modules.Coaching.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddObjectifsCoaching : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PeriodesObjectifsCoaching",
                schema: "coaching",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LienCoachingId = table.Column<int>(type: "integer", nullable: false),
                    DateDebut = table.Column<DateOnly>(type: "date", nullable: false),
                    DateFin = table.Column<DateOnly>(type: "date", nullable: false),
                    Archivee = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodesObjectifsCoaching", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ObjectifsCoaching",
                schema: "coaching",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PeriodeObjectifsCoachingId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Titre = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Moyens = table.Column<string>(type: "text", nullable: true),
                    Atteinte = table.Column<int>(type: "integer", nullable: false),
                    Observation = table.Column<string>(type: "text", nullable: true),
                    Note = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectifsCoaching", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObjectifsCoaching_PeriodesObjectifsCoaching_PeriodeObjectif~",
                        column: x => x.PeriodeObjectifsCoachingId,
                        principalSchema: "coaching",
                        principalTable: "PeriodesObjectifsCoaching",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ObjectifsCoaching_PeriodeObjectifsCoachingId",
                schema: "coaching",
                table: "ObjectifsCoaching",
                column: "PeriodeObjectifsCoachingId");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodesObjectifsCoaching_LienCoachingId_Archivee",
                schema: "coaching",
                table: "PeriodesObjectifsCoaching",
                columns: new[] { "LienCoachingId", "Archivee" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ObjectifsCoaching",
                schema: "coaching");

            migrationBuilder.DropTable(
                name: "PeriodesObjectifsCoaching",
                schema: "coaching");
        }
    }
}
