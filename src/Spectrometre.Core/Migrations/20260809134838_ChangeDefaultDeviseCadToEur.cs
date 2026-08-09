using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spectrometre.Core.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDefaultDeviseCadToEur : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Libellé de devise uniquement — montants numériques inchangés (pas de conversion monétaire).
            migrationBuilder.Sql(
                """UPDATE core."Plans" SET "PrixDevise" = 'EUR' WHERE "PrixDevise" = 'CAD';""");
            migrationBuilder.Sql(
                """UPDATE core."ModulePrix" SET "Devise" = 'EUR' WHERE "Devise" = 'CAD';""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """UPDATE core."Plans" SET "PrixDevise" = 'CAD' WHERE "PrixDevise" = 'EUR';""");
            migrationBuilder.Sql(
                """UPDATE core."ModulePrix" SET "Devise" = 'CAD' WHERE "Devise" = 'EUR';""");
        }
    }
}
