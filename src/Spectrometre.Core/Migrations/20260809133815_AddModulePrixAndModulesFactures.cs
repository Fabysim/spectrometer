using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Spectrometre.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddModulePrixAndModulesFactures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModulesFactures",
                schema: "core",
                table: "PaiementsEnregistres",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ModulePrix",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ModuleCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrixMensuel = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Devise = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Facturable = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModulePrix", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "core",
                table: "ModulePrix",
                columns: new[] { "Id", "CreatedAt", "Devise", "Facturable", "ModuleCode", "PrixMensuel" },
                values: new object[,]
                {
                    { 1, new DateTimeOffset(new DateTime(2026, 8, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "CAD", false, "ProfilCandidat", 0m },
                    { 2, new DateTimeOffset(new DateTime(2026, 8, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "CAD", false, "ProfilEntreprise", 0m },
                    { 3, new DateTimeOffset(new DateTime(2026, 8, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "CAD", false, "ProfilCoach", 0m },
                    { 4, new DateTimeOffset(new DateTime(2026, 8, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "CAD", false, "Admin", 0m },
                    { 5, new DateTimeOffset(new DateTime(2026, 8, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "CAD", true, "Compatibilite", 15m },
                    { 6, new DateTimeOffset(new DateTime(2026, 8, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "CAD", true, "Recrutement", 25m },
                    { 7, new DateTimeOffset(new DateTime(2026, 8, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "CAD", true, "Vivier", 10m },
                    { 8, new DateTimeOffset(new DateTime(2026, 8, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "CAD", true, "Entretien", 15m },
                    { 9, new DateTimeOffset(new DateTime(2026, 8, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "CAD", true, "Analytics", 15m },
                    { 10, new DateTimeOffset(new DateTime(2026, 8, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "CAD", true, "SuiviEvolutif", 20m },
                    { 11, new DateTimeOffset(new DateTime(2026, 8, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "CAD", true, "SuiviEmployes", 30m },
                    { 12, new DateTimeOffset(new DateTime(2026, 8, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "CAD", true, "GestionDuTemps", 25m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModulePrix_ModuleCode",
                schema: "core",
                table: "ModulePrix",
                column: "ModuleCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModulePrix",
                schema: "core");

            migrationBuilder.DropColumn(
                name: "ModulesFactures",
                schema: "core",
                table: "PaiementsEnregistres");
        }
    }
}
