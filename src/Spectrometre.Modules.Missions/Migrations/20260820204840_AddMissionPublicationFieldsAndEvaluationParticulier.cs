using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Modules.Missions.Migrations
{
    /// <inheritdoc />
    public partial class AddMissionPublicationFieldsAndEvaluationParticulier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AccesDifficile",
                schema: "missions",
                table: "Missions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Categorie",
                schema: "missions",
                table: "Missions",
                type: "integer",
                nullable: false,
                defaultValue: 10); // MissionCategorie.Autre — titres libres déjà publiés

            migrationBuilder.AddColumn<int>(
                name: "NiveauEncadrement",
                schema: "missions",
                table: "Missions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "PortDeCharge",
                schema: "missions",
                table: "Missions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PresenceAnimaux",
                schema: "missions",
                table: "Missions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PresenceEscaliers",
                schema: "missions",
                table: "Missions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RisqueParticulier",
                schema: "missions",
                table: "Missions",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MissionEvaluationsParticulier",
                schema: "missions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MissionAcceptationId = table.Column<int>(type: "integer", nullable: false),
                    Ponctualite = table.Column<bool>(type: "boolean", nullable: true),
                    ConsignesComprises = table.Column<bool>(type: "boolean", nullable: true),
                    TacheRealiseeCorrectement = table.Column<bool>(type: "boolean", nullable: true),
                    AttitudeRespectueuse = table.Column<bool>(type: "boolean", nullable: true),
                    PointsPositifs = table.Column<string>(type: "text", nullable: true),
                    PointsAAmeliorer = table.Column<string>(type: "text", nullable: true),
                    AccepteraitNouvelleMission = table.Column<bool>(type: "boolean", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionEvaluationsParticulier", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionEvaluationsParticulier_MissionAcceptations_MissionAc~",
                        column: x => x.MissionAcceptationId,
                        principalSchema: "missions",
                        principalTable: "MissionAcceptations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MissionEvaluationsParticulier_MissionAcceptationId",
                schema: "missions",
                table: "MissionEvaluationsParticulier",
                column: "MissionAcceptationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MissionEvaluationsParticulier",
                schema: "missions");

            migrationBuilder.DropColumn(
                name: "AccesDifficile",
                schema: "missions",
                table: "Missions");

            migrationBuilder.DropColumn(
                name: "Categorie",
                schema: "missions",
                table: "Missions");

            migrationBuilder.DropColumn(
                name: "NiveauEncadrement",
                schema: "missions",
                table: "Missions");

            migrationBuilder.DropColumn(
                name: "PortDeCharge",
                schema: "missions",
                table: "Missions");

            migrationBuilder.DropColumn(
                name: "PresenceAnimaux",
                schema: "missions",
                table: "Missions");

            migrationBuilder.DropColumn(
                name: "PresenceEscaliers",
                schema: "missions",
                table: "Missions");

            migrationBuilder.DropColumn(
                name: "RisqueParticulier",
                schema: "missions",
                table: "Missions");
        }
    }
}
