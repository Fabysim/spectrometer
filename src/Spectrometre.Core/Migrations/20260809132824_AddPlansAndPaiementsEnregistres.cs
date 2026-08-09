using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Spectrometre.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddPlansAndPaiementsEnregistres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaiementsEnregistres",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubjectType = table.Column<int>(type: "integer", nullable: false),
                    SubjectId = table.Column<int>(type: "integer", nullable: false),
                    PlanCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Montant = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Devise = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DateReception = table.Column<DateOnly>(type: "date", nullable: false),
                    Moyen = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PeriodeCouverteDebut = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodeCouverteFin = table.Column<DateOnly>(type: "date", nullable: false),
                    NotePar = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaiementsEnregistres", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plans",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Nom = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PrixMontant = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PrixDevise = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Periodicite = table.Column<int>(type: "integer", nullable: false),
                    Actif = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "core",
                table: "Plans",
                columns: new[] { "Id", "Actif", "Code", "CreatedAt", "Nom", "Periodicite", "PrixDevise", "PrixMontant" },
                values: new object[,]
                {
                    { 1, true, "Standard", new DateTimeOffset(new DateTime(2026, 8, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Standard", 0, "CAD", 49m },
                    { 2, true, "StandardPlusTemps", new DateTimeOffset(new DateTime(2026, 8, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Standard + Temps", 0, "CAD", 79m },
                    { 3, true, "Coach", new DateTimeOffset(new DateTime(2026, 8, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Coach (gratuit)", 0, "CAD", 0m },
                    { 4, true, "CoachPlusTemps", new DateTimeOffset(new DateTime(2026, 8, 9, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Coach + Temps", 0, "CAD", 19m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaiementsEnregistres_SubjectType_SubjectId_CreatedAt",
                schema: "core",
                table: "PaiementsEnregistres",
                columns: new[] { "SubjectType", "SubjectId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Plans_Code",
                schema: "core",
                table: "Plans",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaiementsEnregistres",
                schema: "core");

            migrationBuilder.DropTable(
                name: "Plans",
                schema: "core");
        }
    }
}
