using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spectrometre.Modules.ProfilEntreprise.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompanyProfileSingletonConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Singleton",
                schema: "public",
                table: "CompanyProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_Singleton",
                schema: "public",
                table: "CompanyProfiles",
                column: "Singleton",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CompanyProfiles_Singleton",
                schema: "public",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "Singleton",
                schema: "public",
                table: "CompanyProfiles");
        }
    }
}
