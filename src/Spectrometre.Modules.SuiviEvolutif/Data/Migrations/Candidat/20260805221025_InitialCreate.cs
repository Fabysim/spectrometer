using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Modules.SuiviEvolutif.Data.Migrations.Candidat
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "suivi_evolutif_candidat");

            migrationBuilder.CreateTable(
                name: "Entries",
                schema: "suivi_evolutif_candidat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CandidateProfileId = table.Column<int>(type: "integer", nullable: false),
                    Champ = table.Column<string>(type: "text", nullable: false),
                    AncienneValeur = table.Column<string>(type: "text", nullable: true),
                    NouvelleValeur = table.Column<string>(type: "text", nullable: true),
                    Horodatage = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Entries_CandidateProfileId_Horodatage",
                schema: "suivi_evolutif_candidat",
                table: "Entries",
                columns: new[] { "CandidateProfileId", "Horodatage" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Entries",
                schema: "suivi_evolutif_candidat");
        }
    }
}
