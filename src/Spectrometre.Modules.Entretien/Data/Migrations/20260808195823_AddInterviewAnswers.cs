using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Modules.Entretien.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewAnswers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InterviewAnswers",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InterviewQuestionId = table.Column<int>(type: "integer", nullable: false),
                    CandidateProfileId = table.Column<int>(type: "integer", nullable: false),
                    Response = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewAnswers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewAnswers_InterviewQuestionId_CandidateProfileId",
                schema: "public",
                table: "InterviewAnswers",
                columns: new[] { "InterviewQuestionId", "CandidateProfileId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterviewAnswers",
                schema: "public");
        }
    }
}
