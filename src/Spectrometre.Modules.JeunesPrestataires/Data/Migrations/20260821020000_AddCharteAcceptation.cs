using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Modules.JeunesPrestataires.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCharteAcceptation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharteAcceptations",
                schema: "jeunes_prestataires",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JeuneProfileId = table.Column<int>(type: "integer", nullable: false),
                    NomConfirmation = table.Column<string>(type: "text", nullable: false),
                    AccepteeLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharteAcceptations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharteAcceptations_JeuneProfileId",
                schema: "jeunes_prestataires",
                table: "CharteAcceptations",
                column: "JeuneProfileId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharteAcceptations",
                schema: "jeunes_prestataires");
        }
    }
}
