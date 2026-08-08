using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Modules.Entretien.Data.Migrations.Catalog
{
    /// <inheritdoc />
    public partial class AddInterviewQuestionCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "InterviewQuestionCategories",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeedKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewQuestionCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InterviewQuestionSubCategories",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InterviewQuestionCategoryId = table.Column<int>(type: "integer", nullable: false),
                    SeedKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewQuestionSubCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewQuestionSubCategories_InterviewQuestionCategories_~",
                        column: x => x.InterviewQuestionCategoryId,
                        principalSchema: "public",
                        principalTable: "InterviewQuestionCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InterviewQuestions",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InterviewQuestionSubCategoryId = table.Column<int>(type: "integer", nullable: false),
                    SeedKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ExpectedElements = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewQuestions_InterviewQuestionSubCategories_Interview~",
                        column: x => x.InterviewQuestionSubCategoryId,
                        principalSchema: "public",
                        principalTable: "InterviewQuestionSubCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestionCategories_SeedKey",
                schema: "public",
                table: "InterviewQuestionCategories",
                column: "SeedKey",
                unique: true,
                filter: "\"SeedKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestions_InterviewQuestionSubCategoryId",
                schema: "public",
                table: "InterviewQuestions",
                column: "InterviewQuestionSubCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestions_SeedKey",
                schema: "public",
                table: "InterviewQuestions",
                column: "SeedKey",
                unique: true,
                filter: "\"SeedKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestionSubCategories_InterviewQuestionCategoryId",
                schema: "public",
                table: "InterviewQuestionSubCategories",
                column: "InterviewQuestionCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewQuestionSubCategories_SeedKey",
                schema: "public",
                table: "InterviewQuestionSubCategories",
                column: "SeedKey",
                unique: true,
                filter: "\"SeedKey\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterviewQuestions",
                schema: "public");

            migrationBuilder.DropTable(
                name: "InterviewQuestionSubCategories",
                schema: "public");

            migrationBuilder.DropTable(
                name: "InterviewQuestionCategories",
                schema: "public");
        }
    }
}
