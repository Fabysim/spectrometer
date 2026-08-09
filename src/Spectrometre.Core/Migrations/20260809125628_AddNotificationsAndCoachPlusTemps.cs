using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Spectrometre.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationsAndCoachPlusTemps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationsUtilisateur",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Titre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Lien = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TypeCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LueLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationsUtilisateur", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "core",
                table: "PlanModuleEntitlements",
                columns: new[] { "Id", "ModuleCode", "PlanCode" },
                values: new object[,]
                {
                    { 2000, "ProfilCoach", "CoachPlusTemps" },
                    { 2001, "GestionDuTemps", "CoachPlusTemps" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationsUtilisateur_CreatedAt",
                schema: "core",
                table: "NotificationsUtilisateur",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationsUtilisateur_UserId_LueLe",
                schema: "core",
                table: "NotificationsUtilisateur",
                columns: new[] { "UserId", "LueLe" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationsUtilisateur",
                schema: "core");

            migrationBuilder.DeleteData(
                schema: "core",
                table: "PlanModuleEntitlements",
                keyColumn: "Id",
                keyValue: 2000);

            migrationBuilder.DeleteData(
                schema: "core",
                table: "PlanModuleEntitlements",
                keyColumn: "Id",
                keyValue: 2001);
        }
    }
}
