using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Modules.PostesRecrutement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGuideDeuxiemeEntrevue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuidesDeuxiemeEntrevue",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PosteId = table.Column<int>(type: "integer", nullable: false),
                    MissionLivrables = table.Column<string>(type: "text", nullable: true),
                    SituationQuantitative = table.Column<string>(type: "text", nullable: true),
                    SituationQualitative = table.Column<string>(type: "text", nullable: true),
                    Objectifs = table.Column<string>(type: "text", nullable: true),
                    Suivi = table.Column<string>(type: "text", nullable: true),
                    Echeances = table.Column<string>(type: "text", nullable: true),
                    AutoriteResponsabilite = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuidesDeuxiemeEntrevue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuidesDeuxiemeEntrevue_Postes_PosteId",
                        column: x => x.PosteId,
                        principalSchema: "public",
                        principalTable: "Postes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GuidesDeuxiemeEntrevue_PosteId",
                schema: "public",
                table: "GuidesDeuxiemeEntrevue",
                column: "PosteId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuidesDeuxiemeEntrevue",
                schema: "public");
        }
    }
}
