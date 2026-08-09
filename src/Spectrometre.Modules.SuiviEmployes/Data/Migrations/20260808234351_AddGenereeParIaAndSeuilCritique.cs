using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spectrometre.Modules.SuiviEmployes.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGenereeParIaAndSeuilCritique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SeuilCritiqueAtteint",
                schema: "public",
                table: "EvaluationsObjectifs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "GenereeParIa",
                schema: "public",
                table: "AnalysesIaEmploye",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SeuilCritiqueAtteint",
                schema: "public",
                table: "EvaluationsObjectifs");

            migrationBuilder.DropColumn(
                name: "GenereeParIa",
                schema: "public",
                table: "AnalysesIaEmploye");
        }
    }
}
