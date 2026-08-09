using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Spectrometre.Core.Migrations
{
    /// <inheritdoc />
    public partial class DropPlansAndPlanModuleEntitlements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlanModuleEntitlements",
                schema: "core");

            migrationBuilder.DropTable(
                name: "Plans",
                schema: "core");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlanModuleEntitlements",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ModuleCode = table.Column<string>(type: "text", nullable: false),
                    PlanCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanModuleEntitlements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plans",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Actif = table.Column<bool>(type: "boolean", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Nom = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Periodicite = table.Column<int>(type: "integer", nullable: false),
                    PrixDevise = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PrixMontant = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "core",
                table: "PlanModuleEntitlements",
                columns: new[] { "Id", "ModuleCode", "PlanCode" },
                values: new object[,]
                {
                    { 1, "ProfilCandidat", "Standard" },
                    { 2, "ProfilEntreprise", "Standard" },
                    { 3, "Compatibilite", "Standard" },
                    { 4, "Recrutement", "Standard" },
                    { 5, "Vivier", "Standard" },
                    { 6, "Entretien", "Standard" },
                    { 7, "SuiviEvolutif", "Standard" },
                    { 8, "Analytics", "Standard" },
                    { 9, "ProfilCandidat", "StandardPlusTemps" },
                    { 10, "ProfilEntreprise", "StandardPlusTemps" },
                    { 11, "Compatibilite", "StandardPlusTemps" },
                    { 12, "Recrutement", "StandardPlusTemps" },
                    { 13, "Vivier", "StandardPlusTemps" },
                    { 14, "Entretien", "StandardPlusTemps" },
                    { 15, "SuiviEvolutif", "StandardPlusTemps" },
                    { 16, "Analytics", "StandardPlusTemps" },
                    { 17, "GestionDuTemps", "StandardPlusTemps" },
                    { 18, "ProfilCoach", "Coach" },
                    { 1000, "SuiviEmployes", "Standard" },
                    { 1001, "SuiviEmployes", "StandardPlusTemps" },
                    { 2000, "ProfilCoach", "CoachPlusTemps" },
                    { 2001, "GestionDuTemps", "CoachPlusTemps" }
                });

            migrationBuilder.InsertData(
                schema: "core",
                table: "Plans",
                columns: new[] { "Id", "Actif", "Code", "CreatedAt", "Nom", "Periodicite", "PrixDevise", "PrixMontant" },
                values: new object[,]
                {
                    { 1, true, "Standard", new DateTimeOffset(new DateTime(2026, 8, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Standard", 0, "EUR", 49m },
                    { 2, true, "StandardPlusTemps", new DateTimeOffset(new DateTime(2026, 8, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Standard + Temps", 0, "EUR", 79m },
                    { 3, true, "Coach", new DateTimeOffset(new DateTime(2026, 8, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Coach (gratuit)", 0, "EUR", 0m },
                    { 4, true, "CoachPlusTemps", new DateTimeOffset(new DateTime(2026, 8, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Coach + Temps", 0, "EUR", 19m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanModuleEntitlements_PlanCode_ModuleCode",
                schema: "core",
                table: "PlanModuleEntitlements",
                columns: new[] { "PlanCode", "ModuleCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Plans_Code",
                schema: "core",
                table: "Plans",
                column: "Code",
                unique: true);
        }
    }
}
