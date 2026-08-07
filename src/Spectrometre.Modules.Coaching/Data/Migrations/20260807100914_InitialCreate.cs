using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Modules.Coaching.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "coaching");

            migrationBuilder.CreateTable(
                name: "AnamnesesCoaching",
                schema: "coaching",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LienCoachingId = table.Column<int>(type: "integer", nullable: false),
                    Contenu = table.Column<string>(type: "text", nullable: false),
                    GenereeParIa = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnamnesesCoaching", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LiensCoaching",
                schema: "coaching",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SuiviUserId = table.Column<string>(type: "text", nullable: false),
                    CoachUserId = table.Column<string>(type: "text", nullable: false),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    InvitationId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AccepteLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClotureLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiensCoaching", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnamnesesCoaching_LienCoachingId",
                schema: "coaching",
                table: "AnamnesesCoaching",
                column: "LienCoachingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LiensCoaching_CoachUserId",
                schema: "coaching",
                table: "LiensCoaching",
                column: "CoachUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LiensCoaching_SuiviUserId",
                schema: "coaching",
                table: "LiensCoaching",
                column: "SuiviUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnamnesesCoaching",
                schema: "coaching");

            migrationBuilder.DropTable(
                name: "LiensCoaching",
                schema: "coaching");
        }
    }
}
