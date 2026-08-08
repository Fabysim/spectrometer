using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spectrometre.Modules.ProfilEntreprise.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPosteOffreTexte : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OffreGenereeLe",
                schema: "public",
                table: "Postes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OffreGenereeParIa",
                schema: "public",
                table: "Postes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OffreTexte",
                schema: "public",
                table: "Postes",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OffreGenereeLe",
                schema: "public",
                table: "Postes");

            migrationBuilder.DropColumn(
                name: "OffreGenereeParIa",
                schema: "public",
                table: "Postes");

            migrationBuilder.DropColumn(
                name: "OffreTexte",
                schema: "public",
                table: "Postes");
        }
    }
}
