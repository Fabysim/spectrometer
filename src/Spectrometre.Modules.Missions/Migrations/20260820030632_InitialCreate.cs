using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Modules.Missions.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "missions");

            migrationBuilder.CreateTable(
                name: "Missions",
                schema: "missions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParticulierProfileId = table.Column<int>(type: "integer", nullable: false),
                    Titre = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Lieu = table.Column<string>(type: "text", nullable: true),
                    DureeEstimee = table.Column<string>(type: "text", nullable: true),
                    Difficulte = table.Column<int>(type: "integer", nullable: false),
                    RemunerationMontant = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CompetencesTravaillees = table.Column<string>(type: "text", nullable: true),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Missions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParticulierProfiles",
                schema: "missions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Nom = table.Column<string>(type: "text", nullable: false),
                    Prenoms = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticulierProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MissionAcceptations",
                schema: "missions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MissionId = table.Column<int>(type: "integer", nullable: false),
                    JeuneProfileId = table.Column<int>(type: "integer", nullable: false),
                    AccepteeLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    CoachUserId = table.Column<string>(type: "text", nullable: true),
                    DecideeLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionAcceptations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionAcceptations_Missions_MissionId",
                        column: x => x.MissionId,
                        principalSchema: "missions",
                        principalTable: "Missions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MissionAcceptations_JeuneProfileId",
                schema: "missions",
                table: "MissionAcceptations",
                column: "JeuneProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionAcceptations_MissionId",
                schema: "missions",
                table: "MissionAcceptations",
                column: "MissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Missions_ParticulierProfileId",
                schema: "missions",
                table: "Missions",
                column: "ParticulierProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Missions_Statut",
                schema: "missions",
                table: "Missions",
                column: "Statut");

            migrationBuilder.CreateIndex(
                name: "IX_ParticulierProfiles_UserId",
                schema: "missions",
                table: "ParticulierProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MissionAcceptations",
                schema: "missions");

            migrationBuilder.DropTable(
                name: "ParticulierProfiles",
                schema: "missions");

            migrationBuilder.DropTable(
                name: "Missions",
                schema: "missions");
        }
    }
}
