using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Spectrometre.Modules.JeunesPrestataires.Data;

#nullable disable

namespace Spectrometre.Modules.JeunesPrestataires.Data.Migrations
{
    [DbContext(typeof(JeunesPrestatairesDbContext))]
    [Migration("20260821140000_AddProfilAccompagnement")]
    public class AddProfilAccompagnement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProfilAccompagnement",
                schema: "jeunes_prestataires",
                table: "JeuneProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProfilAccompagnement",
                schema: "jeunes_prestataires",
                table: "InvitationsJeunesPrestataires",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilAccompagnement",
                schema: "jeunes_prestataires",
                table: "JeuneProfiles");

            migrationBuilder.DropColumn(
                name: "ProfilAccompagnement",
                schema: "jeunes_prestataires",
                table: "InvitationsJeunesPrestataires");
        }
    }
}
