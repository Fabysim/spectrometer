using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spectrometre.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyAdministrativeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                schema: "core",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                schema: "core",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                schema: "core",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                schema: "core",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VTA",
                schema: "core",
                table: "Companies",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                schema: "core",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "City",
                schema: "core",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Country",
                schema: "core",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                schema: "core",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "VTA",
                schema: "core",
                table: "Companies");
        }
    }
}
