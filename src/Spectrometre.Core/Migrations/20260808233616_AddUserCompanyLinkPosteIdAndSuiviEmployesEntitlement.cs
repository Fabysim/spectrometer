using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Spectrometre.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCompanyLinkPosteIdAndSuiviEmployesEntitlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PosteId",
                schema: "core",
                table: "UserCompanyLinks",
                type: "integer",
                nullable: true);

            migrationBuilder.InsertData(
                schema: "core",
                table: "PlanModuleEntitlements",
                columns: new[] { "Id", "ModuleCode", "PlanCode" },
                values: new object[,]
                {
                    { 1000, "SuiviEmployes", "Standard" },
                    { 1001, "SuiviEmployes", "StandardPlusTemps" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "core",
                table: "PlanModuleEntitlements",
                keyColumn: "Id",
                keyValue: 1000);

            migrationBuilder.DeleteData(
                schema: "core",
                table: "PlanModuleEntitlements",
                keyColumn: "Id",
                keyValue: 1001);

            migrationBuilder.DropColumn(
                name: "PosteId",
                schema: "core",
                table: "UserCompanyLinks");
        }
    }
}
