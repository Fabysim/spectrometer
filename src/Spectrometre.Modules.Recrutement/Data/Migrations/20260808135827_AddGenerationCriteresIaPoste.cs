using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Modules.Recrutement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGenerationCriteresIaPoste : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GenerationsCriteresIaPoste",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PosteId = table.Column<int>(type: "integer", nullable: false),
                    HashContexte = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    GenereeLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GenereeParIa = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GenerationsCriteresIaPoste", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GenerationsCriteresIaPoste_Postes_PosteId",
                        column: x => x.PosteId,
                        principalSchema: "public",
                        principalTable: "Postes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GenerationsCriteresIaPoste_PosteId",
                schema: "public",
                table: "GenerationsCriteresIaPoste",
                column: "PosteId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GenerationsCriteresIaPoste",
                schema: "public");
        }
    }
}
