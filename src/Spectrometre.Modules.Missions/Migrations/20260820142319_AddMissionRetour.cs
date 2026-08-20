using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Modules.Missions.Migrations
{
    /// <inheritdoc />
    public partial class AddMissionRetour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MissionRetours",
                schema: "missions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MissionAcceptationId = table.Column<int>(type: "integer", nullable: false),
                    CeQuiSestBienPasse = table.Column<string>(type: "text", nullable: true),
                    CeQuiAEteDifficile = table.Column<string>(type: "text", nullable: true),
                    CeQueJaiAppris = table.Column<string>(type: "text", nullable: true),
                    CeQueJeVeuxAmeliorer = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionRetours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionRetours_MissionAcceptations_MissionAcceptationId",
                        column: x => x.MissionAcceptationId,
                        principalSchema: "missions",
                        principalTable: "MissionAcceptations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MissionRetours_MissionAcceptationId",
                schema: "missions",
                table: "MissionRetours",
                column: "MissionAcceptationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MissionRetours",
                schema: "missions");
        }
    }
}
