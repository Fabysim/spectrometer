using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spectrometre.Modules.ProfilEntreprise.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionTextEn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TextEn",
                schema: "public",
                table: "CompanyQuestions",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 1,
                column: "TextEn",
                value: "Company or organization name");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 2,
                column: "TextEn",
                value: "Main sector of activity");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 3,
                column: "TextEn",
                value: "Main location and areas of operation");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 4,
                column: "TextEn",
                value: "Company size: small, medium, large, group, association, institution");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 5,
                column: "TextEn",
                value: "Years in operation or founding year");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 6,
                column: "TextEn",
                value: "Types of positions generally offered");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 7,
                column: "TextEn",
                value: "What is the company's main mission?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 8,
                column: "TextEn",
                value: "What need of society, the market or the community is the company trying to meet?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 9,
                column: "TextEn",
                value: "What vision is the company pursuing in the medium or long term?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 10,
                column: "TextEn",
                value: "What makes the company useful, different or important in its sector?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 11,
                column: "TextEn",
                value: "What problems does the company want to help solve?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 12,
                column: "TextEn",
                value: "What are the three to five main values the company wants to embody?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 13,
                column: "TextEn",
                value: "How do these values translate into everyday decisions?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 14,
                column: "TextEn",
                value: "What values are expected of employees?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 15,
                column: "TextEn",
                value: "What behaviors are encouraged because they match the company's culture?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 16,
                column: "TextEn",
                value: "What behaviors are incompatible with the company's spirit?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 17,
                column: "TextEn",
                value: "How would you describe the overall work climate: calm, dynamic, demanding, family-like, competitive, creative, structured, flexible?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 18,
                column: "TextEn",
                value: "Is the work mostly individual, collective or a mix of both?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 19,
                column: "TextEn",
                value: "What role do rules, procedures and instructions play in how work is organized?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 20,
                column: "TextEn",
                value: "How much room is given to initiative, autonomy and creativity?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 21,
                column: "TextEn",
                value: "How does the company handle periods of pressure, urgency or high activity?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 22,
                column: "TextEn",
                value: "What leadership style dominates in the company: directive, participative, collaborative, transformational, paternalistic, delegative, results-oriented?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 23,
                column: "TextEn",
                value: "How do managers give instructions and follow up on work?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 24,
                column: "TextEn",
                value: "What role do employees have in decision-making?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 25,
                column: "TextEn",
                value: "How do managers support new employees?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 26,
                column: "TextEn",
                value: "How are mistakes handled: sanction, learning, correction, support, discussion?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 27,
                column: "TextEn",
                value: "What type of employee thrives best under this leadership style?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 28,
                column: "TextEn",
                value: "How do employees communicate with each other: verbally, in writing, in meetings, by messaging, through reports?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 29,
                column: "TextEn",
                value: "Is the relational climate mostly formal, informal, hierarchical, family-like, direct, diplomatic or reserved?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 30,
                column: "TextEn",
                value: "How are disagreements or conflicts usually handled?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 31,
                column: "TextEn",
                value: "How much importance does the company place on listening, respect, politeness and cooperation?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 32,
                column: "TextEn",
                value: "What relational behaviors are particularly valued in the company?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 33,
                column: "TextEn",
                value: "What relational behaviors create difficulties in the company?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 34,
                column: "TextEn",
                value: "How does the company recognize effort and good results?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 35,
                column: "TextEn",
                value: "What types of motivation are most present: salary, responsibility, recognition, progression, stability, autonomy, team spirit?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 36,
                column: "TextEn",
                value: "What learning, training or advancement opportunities are offered?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 37,
                column: "TextEn",
                value: "How does the company support people who want to advance?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 38,
                column: "TextEn",
                value: "What signs show that an employee is well integrated and valued in the company?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 39,
                column: "TextEn",
                value: "What are the usual hours and work pace?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 40,
                column: "TextEn",
                value: "Does the job require travel, mobility, special availability or variable hours?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 41,
                column: "TextEn",
                value: "What are the main physical, psychological, relational or organizational constraints?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 42,
                column: "TextEn",
                value: "What level of autonomy is expected of the employee?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 43,
                column: "TextEn",
                value: "What level of pressure, speed or precision does the job require?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 44,
                column: "TextEn",
                value: "What resources does the company provide to enable good work?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 45,
                column: "TextEn",
                value: "What personal qualities help someone succeed in the company?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 46,
                column: "TextEn",
                value: "What technical or professional skills are most sought after?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 47,
                column: "TextEn",
                value: "What type of personality fits easily into the team?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 48,
                column: "TextEn",
                value: "What type of candidate might struggle in this environment?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 49,
                column: "TextEn",
                value: "What personal values of the candidate must be compatible with the company's?");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 50,
                column: "TextEn",
                value: "Our company is mainly characterized by...");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 51,
                column: "TextEn",
                value: "Our three most important values are...");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 52,
                column: "TextEn",
                value: "Our leadership style is more...");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 53,
                column: "TextEn",
                value: "Our relational style is more...");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 54,
                column: "TextEn",
                value: "The employees who succeed best here are those who...");

            migrationBuilder.UpdateData(
                schema: "public",
                table: "CompanyQuestions",
                keyColumn: "Id",
                keyValue: 55,
                column: "TextEn",
                value: "Candidates should pay particular attention to...");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TextEn",
                schema: "public",
                table: "CompanyQuestions");
        }
    }
}
