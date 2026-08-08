using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spectrometre.Modules.ProfilEntreprise.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluationNiveauDeclare : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "NiveauFinal",
                schema: "public",
                table: "EvaluationsCriteresCandidature",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "NiveauDeclare",
                schema: "public",
                table: "EvaluationsCriteresCandidature",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NiveauDeclare",
                schema: "public",
                table: "EvaluationsCriteresCandidature");

            migrationBuilder.AlterColumn<int>(
                name: "NiveauFinal",
                schema: "public",
                table: "EvaluationsCriteresCandidature",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
