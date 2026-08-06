using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spectrometre.Modules.ProfilCandidat.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionTextEn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TextEn",
                schema: "profil_candidat",
                table: "CandidateQuestions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextEn",
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 1,
                column: "TextEn",
                value: "I enjoy organizing activities, even when they require a lot of preparation.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 2,
                column: "TextEn",
                value: "I enjoy fixing things, diagnosing the problem and finding a solution.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 3,
                column: "TextEn",
                value: "I enjoy explaining something to someone until they understand it.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 4,
                column: "TextEn",
                value: "I enjoy writing, reading, analyzing or summarizing information.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 5,
                column: "TextEn",
                value: "I enjoy selling, negotiating or convincing someone to embrace an idea.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 6,
                column: "TextEn",
                value: "Working with children, young people or people in difficulty.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 7,
                column: "TextEn",
                value: "Solving technical or practical problems.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 8,
                column: "TextEn",
                value: "Creating posters, content, plans or presentations.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 9,
                column: "TextEn",
                value: "Welcoming clients and meeting their needs.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 10,
                column: "TextEn",
                value: "Coordinating a team or distributing tasks.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 11,
                column: "TextEn",
                value: "When I draw, create, tinker or design something.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 12,
                column: "TextEn",
                value: "When I research a topic online to understand it.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 13,
                column: "TextEn",
                value: "When I cook, garden or do a hands-on activity.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 14,
                column: "TextEn",
                value: "When I code, calculate or work on a logical problem.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 15,
                column: "TextEn",
                value: "When I talk with someone to listen to them or offer advice.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 16,
                column: "TextEn",
                value: "Health, well-being and helping people.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 17,
                column: "TextEn",
                value: "Machines, electricity, computing or technology.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 18,
                column: "TextEn",
                value: "Education, psychology, communication or human relations.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 19,
                column: "TextEn",
                value: "Business, entrepreneurship, management or money.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 20,
                column: "TextEn",
                value: "The environment, agriculture, food or sustainable development.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 21,
                column: "TextEn",
                value: "I learn languages easily.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 22,
                column: "TextEn",
                value: "I quickly understand numbers, calculations or tables.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 23,
                column: "TextEn",
                value: "I easily remember practical instructions.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 24,
                column: "TextEn",
                value: "I quickly understand how devices or tools work.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 25,
                column: "TextEn",
                value: "I learn easily by watching others do it.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 26,
                column: "TextEn",
                value: "To write a text, proofread a document or prepare a presentation.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 27,
                column: "TextEn",
                value: "To fix a phone, a device or solve a computer problem.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 28,
                column: "TextEn",
                value: "To organize a party, a meeting or an activity.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 29,
                column: "TextEn",
                value: "To explain a lesson, an assignment or a procedure.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 30,
                column: "TextEn",
                value: "To listen to a personal problem and give advice.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 31,
                column: "TextEn",
                value: "I'm often told I'm patient.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 32,
                column: "TextEn",
                value: "I'm told I'm serious and reliable.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 33,
                column: "TextEn",
                value: "I'm recognized as someone creative.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 34,
                column: "TextEn",
                value: "People say I communicate easily with others.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 35,
                column: "TextEn",
                value: "I'm told I'm courageous and persistent.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 36,
                column: "TextEn",
                value: "I quickly notice when someone isn't doing well.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 37,
                column: "TextEn",
                value: "I easily find simple solutions to practical problems.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 38,
                column: "TextEn",
                value: "I organize things without being asked to.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 39,
                column: "TextEn",
                value: "I motivate others when they become discouraged.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 40,
                column: "TextEn",
                value: "I easily remember important details.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 41,
                column: "TextEn",
                value: "Reading, understanding and summarizing a text.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 42,
                column: "TextEn",
                value: "Using a computer, word processing software or a spreadsheet.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 43,
                column: "TextEn",
                value: "Doing calculations, managing data or interpreting results.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 44,
                column: "TextEn",
                value: "Giving a presentation in front of a group.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 45,
                column: "TextEn",
                value: "Understanding the basics of a field such as accounting, electricity, mechanics, health or teaching.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 46,
                column: "TextEn",
                value: "Welcoming clients and responding to their requests.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 47,
                column: "TextEn",
                value: "Respecting schedules and work instructions.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 48,
                column: "TextEn",
                value: "Working as part of a team with colleagues.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 49,
                column: "TextEn",
                value: "Using professional tools, machines, software or equipment.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 50,
                column: "TextEn",
                value: "Managing petty cash, inventory, an order or a schedule.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 51,
                column: "TextEn",
                value: "Supervising children or young people.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 52,
                column: "TextEn",
                value: "Preparing meals, organizing an activity or managing a small team.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 53,
                column: "TextEn",
                value: "Taking part in community, religious, sports or social activities.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 54,
                column: "TextEn",
                value: "Caring for an elderly, sick or vulnerable person.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 55,
                column: "TextEn",
                value: "Managing a small family business or helping with farm work.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 56,
                column: "TextEn",
                value: "Improving my spoken communication.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 57,
                column: "TextEn",
                value: "Learning a foreign language.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 58,
                column: "TextEn",
                value: "Becoming more proficient with computers.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 59,
                column: "TextEn",
                value: "Developing my management or accounting skills.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 60,
                column: "TextEn",
                value: "Learning to better manage stress and priorities.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 61,
                column: "TextEn",
                value: "Having a stable, secure job.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 62,
                column: "TextEn",
                value: "Helping others and feeling useful.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 63,
                column: "TextEn",
                value: "Having a good income to support my family.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 64,
                column: "TextEn",
                value: "Constantly learning and progressing.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 65,
                column: "TextEn",
                value: "Having autonomy and being able to take initiative.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 66,
                column: "TextEn",
                value: "Working in a respectful environment.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 67,
                column: "TextEn",
                value: "The education of children and young people.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 68,
                column: "TextEn",
                value: "Access to healthcare.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 69,
                column: "TextEn",
                value: "Poverty, unemployment or social exclusion.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 70,
                column: "TextEn",
                value: "Environmental protection.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 71,
                column: "TextEn",
                value: "Improving customer service.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 72,
                column: "TextEn",
                value: "Safety, order or good organization within a company.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 73,
                column: "TextEn",
                value: "Being verbally encouraged when I do my job well.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 74,
                column: "TextEn",
                value: "Receiving more responsibilities.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 75,
                column: "TextEn",
                value: "Concretely seeing the results of my efforts.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 76,
                column: "TextEn",
                value: "Advancing toward a better position.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 77,
                column: "TextEn",
                value: "Feeling that I contribute to a team's success.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 78,
                column: "TextEn",
                value: "Receiving fair pay for the work accomplished.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 79,
                column: "TextEn",
                value: "I prefer working as part of a team, because I enjoy exchanging with others.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 80,
                column: "TextEn",
                value: "I prefer working alone when I need to concentrate.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 81,
                column: "TextEn",
                value: "I like a mix of both: working alone to make progress, then discussing with the team.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 82,
                column: "TextEn",
                value: "I'm more effective when everyone's roles are clearly defined.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 83,
                column: "TextEn",
                value: "I prefer an environment where people help each other.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 84,
                column: "TextEn",
                value: "I prefer structured work with clear instructions.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 85,
                column: "TextEn",
                value: "I like having some freedom to organize my day.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 86,
                column: "TextEn",
                value: "I'm comfortable when the goals are clear, even if the method stays flexible.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 87,
                column: "TextEn",
                value: "I need a precise framework to avoid confusion.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 88,
                column: "TextEn",
                value: "I prefer environments where new ideas can be proposed.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 89,
                column: "TextEn",
                value: "I prefer a calm, organized environment.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 90,
                column: "TextEn",
                value: "I'm motivated by a dynamic environment with lots of activity.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 91,
                column: "TextEn",
                value: "I enjoy social settings where I meet lots of people.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 92,
                column: "TextEn",
                value: "I prefer a technical environment where concrete problems get solved.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 93,
                column: "TextEn",
                value: "I'm comfortable in an administrative setting with documents, files and procedures.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 94,
                column: "TextEn",
                value: "I enjoy creative environments where you can imagine and build something new.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 95,
                column: "TextEn",
                value: "A steady pace with stable hours.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 96,
                column: "TextEn",
                value: "A fast pace with daily challenges.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 97,
                column: "TextEn",
                value: "A flexible pace that lets me organize myself.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 98,
                column: "TextEn",
                value: "A job involving travel, because I like moving around.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 99,
                column: "TextEn",
                value: "A job without frequent travel, because I prefer staying in one place.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 100,
                column: "TextEn",
                value: "A seasonal or variable pace, as long as busy periods are well organized.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 101,
                column: "TextEn",
                value: "Highly repetitive tasks with no variation.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 102,
                column: "TextEn",
                value: "Tasks that require constantly speaking in public.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 103,
                column: "TextEn",
                value: "Very heavy physical labor.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 104,
                column: "TextEn",
                value: "Long administrative tasks with no human contact.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 105,
                column: "TextEn",
                value: "Activities without clear objectives.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 106,
                column: "TextEn",
                value: "Tasks where I have to act too quickly without understanding what's expected of me.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 107,
                column: "TextEn",
                value: "Last-minute changes without explanation.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 108,
                column: "TextEn",
                value: "Lack of respect or frequent conflicts.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 109,
                column: "TextEn",
                value: "Deadlines that are too short and poorly organized.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 110,
                column: "TextEn",
                value: "Constant noise or excessive commotion.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 111,
                column: "TextEn",
                value: "Lack of equipment to do the job properly.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 112,
                column: "TextEn",
                value: "Lack of support when difficulties arise.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 113,
                column: "TextEn",
                value: "Humiliating or public criticism.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 114,
                column: "TextEn",
                value: "Favoritism.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 115,
                column: "TextEn",
                value: "Lack of communication.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 116,
                column: "TextEn",
                value: "Contradictory orders.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 117,
                column: "TextEn",
                value: "Lack of recognition.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 118,
                column: "TextEn",
                value: "Excessive control without trust.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 119,
                column: "TextEn",
                value: "Sales interests me, but I'm not yet comfortable with the pressure of targets.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 120,
                column: "TextEn",
                value: "Medical work attracts me, but I need to think about my ability to cope with suffering.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 121,
                column: "TextEn",
                value: "Team management interests me, but I need to develop my authority and communication.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 122,
                column: "TextEn",
                value: "Entrepreneurship attracts me, but I need to better understand the financial risks.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 123,
                column: "TextEn",
                value: "IT interests me, but I need to check whether I really enjoy working alone in front of a screen for long stretches.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 124,
                column: "TextEn",
                value: "I feel a sense of accomplishment when I finish a useful or visible task.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 125,
                column: "TextEn",
                value: "I feel a sense of collaboration when roles are clear and everyone respects each other.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 126,
                column: "TextEn",
                value: "I feel frustrated when I have to work without explanation, without sufficient resources, or in a tense atmosphere.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 127,
                column: "TextEn",
                value: "I feel joy when my work helps someone or produces a concrete result.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 128,
                column: "TextEn",
                value: "I feel anger in the face of injustice or lack of respect.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 129,
                column: "TextEn",
                value: "I feel sad when my efforts aren't recognized.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 130,
                column: "TextEn",
                value: "I feel satisfied when my work is meaningful and has a visible result.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestionExamples",
                keyColumn: "Id",
                keyValue: 131,
                column: "TextEn",
                value: "I feel dissatisfied when my efforts aren't recognized or when my tasks don't feel meaningful to me.");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                keyColumn: "Id",
                keyValue: 1,
                column: "TextEn",
                value: "What activities do you enjoy doing, even when they require effort?");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                keyColumn: "Id",
                keyValue: 2,
                column: "TextEn",
                value: "Which tasks give you energy instead of quickly tiring you out?");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                keyColumn: "Id",
                keyValue: 3,
                column: "TextEn",
                value: "In which activities do you sometimes lose track of time?");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                keyColumn: "Id",
                keyValue: 4,
                column: "TextEn",
                value: "Which subjects, fields or problems naturally attract your curiosity?");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                keyColumn: "Id",
                keyValue: 5,
                column: "TextEn",
                value: "What things do you learn more easily than other people?");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                keyColumn: "Id",
                keyValue: 6,
                column: "TextEn",
                value: "What activities do others often ask you for help with?");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                keyColumn: "Id",
                keyValue: 7,
                column: "TextEn",
                value: "Which qualities come up often in the compliments people give you?");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                keyColumn: "Id",
                keyValue: 8,
                column: "TextEn",
                value: "Which talents do you use spontaneously, without always considering them important?");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                keyColumn: "Id",
                keyValue: 9,
                column: "TextEn",
                value: "What skills have you acquired through your studies?");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                keyColumn: "Id",
                keyValue: 10,
                column: "TextEn",
                value: "What skills have you acquired through internships, jobs or hands-on experience?");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                keyColumn: "Id",
                keyValue: 11,
                column: "TextEn",
                value: "What skills have you acquired within your family, community, hobbies or volunteer work?");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                keyColumn: "Id",
                keyValue: 12,
                column: "TextEn",
                value: "What skills would you still like to develop?");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                keyColumn: "Id",
                keyValue: 13,
                column: "TextEn",
                value: "What matters most to you in a job?");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                keyColumn: "Id",
                keyValue: 14,
                column: "TextEn",
                value: "What causes, missions or types of problems would you like to help solve?");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                keyColumn: "Id",
                keyValue: 15,
                column: "TextEn",
                value: "What type of recognition motivates you the most?");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                keyColumn: "Id",
                keyValue: 16,
                column: "TextEn",
                value: "Do you prefer working alone, as part of a team, or a mix of both?");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                keyColumn: "Id",
                keyValue: 17,
                column: "TextEn",
                value: "Do you prefer a highly structured job or one that leaves room for organizing your own work?");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                keyColumn: "Id",
                keyValue: 18,
                column: "TextEn",
                value: "Do you prefer a calm, dynamic, competitive, creative, social, technical or administrative environment?");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                keyColumn: "Id",
                keyValue: 19,
                column: "TextEn",
                value: "What work pace suits you best?");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                keyColumn: "Id",
                keyValue: 20,
                column: "TextEn",
                value: "What types of tasks do you avoid or that quickly exhaust you?");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                keyColumn: "Id",
                keyValue: 21,
                column: "TextEn",
                value: "What working conditions stress you out the most?");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                keyColumn: "Id",
                keyValue: 22,
                column: "TextEn",
                value: "What behaviors or management styles demotivate you?");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                keyColumn: "Id",
                keyValue: 23,
                column: "TextEn",
                value: "What fields or positions interest you, but might not match your personality?");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                keyColumn: "Id",
                keyValue: 24,
                column: "TextEn",
                value: "What activities or situations put you in a mindset of accomplishment, acceptance, accommodation, collaboration, opposition, constraint or frustration?");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                keyColumn: "Id",
                keyValue: 25,
                column: "TextEn",
                value: "What situations or activities trigger feelings of joy, anger or sadness in you?");

            migrationBuilder.UpdateData(
                schema: "profil_candidat",
                table: "CandidateQuestions",
                keyColumn: "Id",
                keyValue: 26,
                column: "TextEn",
                value: "What activities or situations give you feelings of satisfaction or dissatisfaction?");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TextEn",
                schema: "profil_candidat",
                table: "CandidateQuestions");

            migrationBuilder.DropColumn(
                name: "TextEn",
                schema: "profil_candidat",
                table: "CandidateQuestionExamples");
        }
    }
}
