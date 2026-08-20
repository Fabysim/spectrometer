using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Modules.Missions.Migrations
{
    /// <inheritdoc />
    public partial class AddMissionPreparationCoche : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MissionPreparationCoches",
                schema: "missions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MissionAcceptationId = table.Column<int>(type: "integer", nullable: false),
                    ItemKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Coche = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionPreparationCoches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MissionPreparationCoches_MissionAcceptations_MissionAccepta~",
                        column: x => x.MissionAcceptationId,
                        principalSchema: "missions",
                        principalTable: "MissionAcceptations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MissionPreparationCoches_MissionAcceptationId_ItemKey",
                schema: "missions",
                table: "MissionPreparationCoches",
                columns: new[] { "MissionAcceptationId", "ItemKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MissionPreparationCoches",
                schema: "missions");
        }
    }
}
