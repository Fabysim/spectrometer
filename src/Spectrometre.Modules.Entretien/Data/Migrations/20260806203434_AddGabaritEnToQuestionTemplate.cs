using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spectrometre.Modules.Entretien.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGabaritEnToQuestionTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GabaritEn",
                schema: "public",
                table: "QuestionTemplates",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "public",
                table: "QuestionTemplates",
                keyColumn: "Id",
                keyValue: 1,
                column: "GabaritEn",
                value: "In your view, which technical skills are you still missing to be fully operational in this role?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "QuestionTemplates",
                keyColumn: "Id",
                keyValue: 2,
                column: "GabaritEn",
                value: "What support or training does the company plan to offer to close any technical skills gap?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "QuestionTemplates",
                keyColumn: "Id",
                keyValue: 3,
                column: "GabaritEn",
                value: "Can you describe a recent situation where your day-to-day way of working was put to the test?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "QuestionTemplates",
                keyColumn: "Id",
                keyValue: 4,
                column: "GabaritEn",
                value: "Which professional behaviors does the company value most day-to-day, beyond what's officially stated?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "QuestionTemplates",
                keyColumn: "Id",
                keyValue: 5,
                column: "GabaritEn",
                value: "What, in a company's culture, has made you uncomfortable in the past?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "QuestionTemplates",
                keyColumn: "Id",
                keyValue: 6,
                column: "GabaritEn",
                value: "How does the company's stated culture concretely translate into day-to-day decisions?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "QuestionTemplates",
                keyColumn: "Id",
                keyValue: 7,
                column: "GabaritEn",
                value: "You indicated you can tolerate a \"{rythmeCandidat}\" pace, while this role is advertised with a \"{rythmeEntreprise}\" pace — how do you see yourself handling this gap day-to-day?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "QuestionTemplates",
                keyColumn: "Id",
                keyValue: 8,
                column: "GabaritEn",
                value: "This role is advertised with a \"{rythmeEntreprise}\" pace — what does a typical week concretely look like for someone who instead tolerates a \"{rythmeCandidat}\" pace?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "QuestionTemplates",
                keyColumn: "Id",
                keyValue: 9,
                column: "GabaritEn",
                value: "What would make you lose motivation fastest in this role?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "QuestionTemplates",
                keyColumn: "Id",
                keyValue: 10,
                column: "GabaritEn",
                value: "What does the company concretely put in place to nurture its teams' motivation?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "QuestionTemplates",
                keyColumn: "Id",
                keyValue: 11,
                column: "GabaritEn",
                value: "You flagged \"{tag}\" as a potential point of caution — can you clarify what this concretely means to you, and how you've handled it in the past?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "QuestionTemplates",
                keyColumn: "Id",
                keyValue: 12,
                column: "GabaritEn",
                value: "The company also identified \"{tag}\" as a point of caution — how does this concretely show up day-to-day within the team?");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GabaritEn",
                schema: "public",
                table: "QuestionTemplates");
        }
    }
}
