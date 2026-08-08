using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Modules.Recrutement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyseIaPoste : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalysesIaPoste",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PosteId = table.Column<int>(type: "integer", nullable: false),
                    CandidatureId = table.Column<int>(type: "integer", nullable: false),
                    AnalyseTexte = table.Column<string>(type: "text", nullable: false),
                    GenereeLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GenereeParIa = table.Column<bool>(type: "boolean", nullable: false),
                    HashSnapshot = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalysesIaPoste", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalysesIaPoste_Candidatures_CandidatureId",
                        column: x => x.CandidatureId,
                        principalSchema: "public",
                        principalTable: "Candidatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalysesIaPoste_CandidatureId",
                schema: "public",
                table: "AnalysesIaPoste",
                column: "CandidatureId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalysesIaPoste_PosteId_CandidatureId",
                schema: "public",
                table: "AnalysesIaPoste",
                columns: new[] { "PosteId", "CandidatureId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalysesIaPoste",
                schema: "public");
        }
    }
}
