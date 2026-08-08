using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spectrometre.Modules.PostesRecrutement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPosteChampsClotureSalaire : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Avantages",
                schema: "public",
                table: "Postes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DateCloture",
                schema: "public",
                table: "Postes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Salaire",
                schema: "public",
                table: "Postes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TachesDescription",
                schema: "public",
                table: "Postes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Avantages",
                schema: "public",
                table: "Postes");

            migrationBuilder.DropColumn(
                name: "DateCloture",
                schema: "public",
                table: "Postes");

            migrationBuilder.DropColumn(
                name: "Salaire",
                schema: "public",
                table: "Postes");

            migrationBuilder.DropColumn(
                name: "TachesDescription",
                schema: "public",
                table: "Postes");
        }
    }
}
