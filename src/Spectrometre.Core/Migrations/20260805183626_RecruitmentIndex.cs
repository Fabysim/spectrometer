using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Core.Migrations
{
    /// <inheritdoc />
    public partial class RecruitmentIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CandidatureIndexEntries",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<int>(type: "integer", nullable: false),
                    PosteId = table.Column<int>(type: "integer", nullable: false),
                    PosteTitre = table.Column<string>(type: "text", nullable: false),
                    CandidateProfileId = table.Column<int>(type: "integer", nullable: false),
                    Statut = table.Column<string>(type: "text", nullable: false),
                    ScoreCompatibilite = table.Column<int>(type: "integer", nullable: true),
                    TagsCles = table.Column<List<string>>(type: "text[]", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidatureIndexEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PosteIndexEntries",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyId = table.Column<int>(type: "integer", nullable: false),
                    CompanyName = table.Column<string>(type: "text", nullable: false),
                    PosteId = table.Column<int>(type: "integer", nullable: false),
                    Titre = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Departement = table.Column<string>(type: "text", nullable: true),
                    Statut = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosteIndexEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CandidatureIndexEntries_CandidateProfileId",
                schema: "core",
                table: "CandidatureIndexEntries",
                column: "CandidateProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidatureIndexEntries_CompanyId_PosteId_CandidateProfileId",
                schema: "core",
                table: "CandidatureIndexEntries",
                columns: new[] { "CompanyId", "PosteId", "CandidateProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PosteIndexEntries_CompanyId_PosteId",
                schema: "core",
                table: "PosteIndexEntries",
                columns: new[] { "CompanyId", "PosteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PosteIndexEntries_Statut",
                schema: "core",
                table: "PosteIndexEntries",
                column: "Statut");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidatureIndexEntries",
                schema: "core");

            migrationBuilder.DropTable(
                name: "PosteIndexEntries",
                schema: "core");
        }
    }
}
