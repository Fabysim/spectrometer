using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spectrometre.Modules.ProfilEntreprise.Data.Migrations
{
    /// <inheritdoc />
    public partial class StructuredCompatibilityCriteria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TechniqueText",
                schema: "public",
                table: "CompanyCompatibilityCriteria",
                newName: "TechniqueNotes");

            migrationBuilder.RenameColumn(
                name: "PointsVigilanceText",
                schema: "public",
                table: "CompanyCompatibilityCriteria",
                newName: "PointsVigilanceNotes");

            migrationBuilder.RenameColumn(
                name: "OrganisationnelleText",
                schema: "public",
                table: "CompanyCompatibilityCriteria",
                newName: "OrganisationnelleNotes");

            migrationBuilder.RenameColumn(
                name: "MotivationnelleText",
                schema: "public",
                table: "CompanyCompatibilityCriteria",
                newName: "MotivationnelleNotes");

            migrationBuilder.RenameColumn(
                name: "CulturelleText",
                schema: "public",
                table: "CompanyCompatibilityCriteria",
                newName: "CulturelleNotes");

            migrationBuilder.RenameColumn(
                name: "ComportementaleText",
                schema: "public",
                table: "CompanyCompatibilityCriteria",
                newName: "ComportementaleNotes");

            migrationBuilder.AddColumn<List<string>>(
                name: "ComportementaleTags",
                schema: "public",
                table: "CompanyCompatibilityCriteria",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "CulturelleTags",
                schema: "public",
                table: "CompanyCompatibilityCriteria",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "MotivationnelleTags",
                schema: "public",
                table: "CompanyCompatibilityCriteria",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "PointsVigilanceTags",
                schema: "public",
                table: "CompanyCompatibilityCriteria",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "RythmeTravail",
                schema: "public",
                table: "CompanyCompatibilityCriteria",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "TechniqueTags",
                schema: "public",
                table: "CompanyCompatibilityCriteria",
                type: "text[]",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComportementaleTags",
                schema: "public",
                table: "CompanyCompatibilityCriteria");

            migrationBuilder.DropColumn(
                name: "CulturelleTags",
                schema: "public",
                table: "CompanyCompatibilityCriteria");

            migrationBuilder.DropColumn(
                name: "MotivationnelleTags",
                schema: "public",
                table: "CompanyCompatibilityCriteria");

            migrationBuilder.DropColumn(
                name: "PointsVigilanceTags",
                schema: "public",
                table: "CompanyCompatibilityCriteria");

            migrationBuilder.DropColumn(
                name: "RythmeTravail",
                schema: "public",
                table: "CompanyCompatibilityCriteria");

            migrationBuilder.DropColumn(
                name: "TechniqueTags",
                schema: "public",
                table: "CompanyCompatibilityCriteria");

            migrationBuilder.RenameColumn(
                name: "TechniqueNotes",
                schema: "public",
                table: "CompanyCompatibilityCriteria",
                newName: "TechniqueText");

            migrationBuilder.RenameColumn(
                name: "PointsVigilanceNotes",
                schema: "public",
                table: "CompanyCompatibilityCriteria",
                newName: "PointsVigilanceText");

            migrationBuilder.RenameColumn(
                name: "OrganisationnelleNotes",
                schema: "public",
                table: "CompanyCompatibilityCriteria",
                newName: "OrganisationnelleText");

            migrationBuilder.RenameColumn(
                name: "MotivationnelleNotes",
                schema: "public",
                table: "CompanyCompatibilityCriteria",
                newName: "MotivationnelleText");

            migrationBuilder.RenameColumn(
                name: "CulturelleNotes",
                schema: "public",
                table: "CompanyCompatibilityCriteria",
                newName: "CulturelleText");

            migrationBuilder.RenameColumn(
                name: "ComportementaleNotes",
                schema: "public",
                table: "CompanyCompatibilityCriteria",
                newName: "ComportementaleText");
        }
    }
}
