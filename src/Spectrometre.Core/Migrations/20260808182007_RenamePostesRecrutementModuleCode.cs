using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spectrometre.Core.Migrations
{
    /// <inheritdoc />
    public partial class RenamePostesRecrutementModuleCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "core",
                table: "PlanModuleEntitlements",
                keyColumn: "Id",
                keyValue: 4,
                column: "ModuleCode",
                value: "Recrutement");

            migrationBuilder.UpdateData(
                schema: "core",
                table: "PlanModuleEntitlements",
                keyColumn: "Id",
                keyValue: 12,
                column: "ModuleCode",
                value: "Recrutement");

            // Activations déjà persistées (entreprises ayant acheté/activé l'ancien code).
            migrationBuilder.Sql(
                """UPDATE core."ModuleActivations" SET "ModuleCode" = 'Recrutement' WHERE "ModuleCode" = 'PostesRecrutement';""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """UPDATE core."ModuleActivations" SET "ModuleCode" = 'PostesRecrutement' WHERE "ModuleCode" = 'Recrutement';""");

            migrationBuilder.UpdateData(
                schema: "core",
                table: "PlanModuleEntitlements",
                keyColumn: "Id",
                keyValue: 4,
                column: "ModuleCode",
                value: "PostesRecrutement");

            migrationBuilder.UpdateData(
                schema: "core",
                table: "PlanModuleEntitlements",
                keyColumn: "Id",
                keyValue: 12,
                column: "ModuleCode",
                value: "PostesRecrutement");
        }
    }
}
