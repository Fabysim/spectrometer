using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spectrometre.Modules.Recrutement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEstPreselectionne : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EstPreselectionne",
                schema: "public",
                table: "Candidatures",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstPreselectionne",
                schema: "public",
                table: "Candidatures");
        }
    }
}
