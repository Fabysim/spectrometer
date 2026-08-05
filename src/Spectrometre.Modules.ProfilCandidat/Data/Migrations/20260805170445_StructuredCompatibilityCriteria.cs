using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spectrometre.Modules.ProfilCandidat.Data.Migrations
{
    /// <inheritdoc />
    public partial class StructuredCompatibilityCriteria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TechniqueText",
                schema: "profil_candidat",
                table: "CandidateCompatibilityCriteria",
                newName: "TechniqueNotes");

            migrationBuilder.RenameColumn(
                name: "PointsVigilanceText",
                schema: "profil_candidat",
                table: "CandidateCompatibilityCriteria",
                newName: "PointsVigilanceNotes");

            migrationBuilder.RenameColumn(
                name: "OrganisationnelleText",
                schema: "profil_candidat",
                table: "CandidateCompatibilityCriteria",
                newName: "OrganisationnelleNotes");

            migrationBuilder.RenameColumn(
                name: "MotivationnelleText",
                schema: "profil_candidat",
                table: "CandidateCompatibilityCriteria",
                newName: "MotivationnelleNotes");

            migrationBuilder.RenameColumn(
                name: "CulturelleText",
                schema: "profil_candidat",
                table: "CandidateCompatibilityCriteria",
                newName: "CulturelleNotes");

            migrationBuilder.RenameColumn(
                name: "ComportementaleText",
                schema: "profil_candidat",
                table: "CandidateCompatibilityCriteria",
                newName: "ComportementaleNotes");

            migrationBuilder.AddColumn<List<string>>(
                name: "ComportementaleTags",
                schema: "profil_candidat",
                table: "CandidateCompatibilityCriteria",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "CulturelleTags",
                schema: "profil_candidat",
                table: "CandidateCompatibilityCriteria",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "MotivationnelleTags",
                schema: "profil_candidat",
                table: "CandidateCompatibilityCriteria",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "PointsVigilanceTags",
                schema: "profil_candidat",
                table: "CandidateCompatibilityCriteria",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "RythmeTravail",
                schema: "profil_candidat",
                table: "CandidateCompatibilityCriteria",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "TechniqueTags",
                schema: "profil_candidat",
                table: "CandidateCompatibilityCriteria",
                type: "text[]",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComportementaleTags",
                schema: "profil_candidat",
                table: "CandidateCompatibilityCriteria");

            migrationBuilder.DropColumn(
                name: "CulturelleTags",
                schema: "profil_candidat",
                table: "CandidateCompatibilityCriteria");

            migrationBuilder.DropColumn(
                name: "MotivationnelleTags",
                schema: "profil_candidat",
                table: "CandidateCompatibilityCriteria");

            migrationBuilder.DropColumn(
                name: "PointsVigilanceTags",
                schema: "profil_candidat",
                table: "CandidateCompatibilityCriteria");

            migrationBuilder.DropColumn(
                name: "RythmeTravail",
                schema: "profil_candidat",
                table: "CandidateCompatibilityCriteria");

            migrationBuilder.DropColumn(
                name: "TechniqueTags",
                schema: "profil_candidat",
                table: "CandidateCompatibilityCriteria");

            migrationBuilder.RenameColumn(
                name: "TechniqueNotes",
                schema: "profil_candidat",
                table: "CandidateCompatibilityCriteria",
                newName: "TechniqueText");

            migrationBuilder.RenameColumn(
                name: "PointsVigilanceNotes",
                schema: "profil_candidat",
                table: "CandidateCompatibilityCriteria",
                newName: "PointsVigilanceText");

            migrationBuilder.RenameColumn(
                name: "OrganisationnelleNotes",
                schema: "profil_candidat",
                table: "CandidateCompatibilityCriteria",
                newName: "OrganisationnelleText");

            migrationBuilder.RenameColumn(
                name: "MotivationnelleNotes",
                schema: "profil_candidat",
                table: "CandidateCompatibilityCriteria",
                newName: "MotivationnelleText");

            migrationBuilder.RenameColumn(
                name: "CulturelleNotes",
                schema: "profil_candidat",
                table: "CandidateCompatibilityCriteria",
                newName: "CulturelleText");

            migrationBuilder.RenameColumn(
                name: "ComportementaleNotes",
                schema: "profil_candidat",
                table: "CandidateCompatibilityCriteria",
                newName: "ComportementaleText");
        }
    }
}
