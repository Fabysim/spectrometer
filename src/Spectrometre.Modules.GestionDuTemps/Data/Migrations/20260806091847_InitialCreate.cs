using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Modules.GestionDuTemps.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "gestion_du_temps");

            migrationBuilder.CreateTable(
                name: "TypesDeTemps",
                schema: "gestion_du_temps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Cle = table.Column<string>(type: "text", nullable: false),
                    Libelle = table.Column<string>(type: "text", nullable: false),
                    HeureDebut = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    HeureFin = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    RecurrenceJours = table.Column<string>(type: "text", nullable: false),
                    OrdreAffichage = table.Column<int>(type: "integer", nullable: false),
                    CompanyId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TypesDeTemps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Activites",
                schema: "gestion_du_temps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    TypeDeTempsId = table.Column<int>(type: "integer", nullable: false),
                    Nom = table.Column<string>(type: "text", nullable: false),
                    DateActivite = table.Column<DateOnly>(type: "date", nullable: false),
                    HeureDebut = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    DureeMinutes = table.Column<int>(type: "integer", nullable: false),
                    CompanyId = table.Column<int>(type: "integer", nullable: true),
                    Statut = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activites_TypesDeTemps_TypeDeTempsId",
                        column: x => x.TypeDeTempsId,
                        principalSchema: "gestion_du_temps",
                        principalTable: "TypesDeTemps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activites_TypeDeTempsId",
                schema: "gestion_du_temps",
                table: "Activites",
                column: "TypeDeTempsId");

            migrationBuilder.CreateIndex(
                name: "IX_Activites_UserId_DateActivite_HeureDebut",
                schema: "gestion_du_temps",
                table: "Activites",
                columns: new[] { "UserId", "DateActivite", "HeureDebut" });

            migrationBuilder.CreateIndex(
                name: "IX_TypesDeTemps_UserId_Cle",
                schema: "gestion_du_temps",
                table: "TypesDeTemps",
                columns: new[] { "UserId", "Cle" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activites",
                schema: "gestion_du_temps");

            migrationBuilder.DropTable(
                name: "TypesDeTemps",
                schema: "gestion_du_temps");
        }
    }
}
