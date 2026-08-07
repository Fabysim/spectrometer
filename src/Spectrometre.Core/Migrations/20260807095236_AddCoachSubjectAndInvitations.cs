using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddCoachSubjectAndInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CoachSubscriptions",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CoachProfileId = table.Column<int>(type: "integer", nullable: false),
                    PlanCode = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RenewalDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Invitations",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmetteurUserId = table.Column<string>(type: "text", nullable: false),
                    EmailInvite = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ContextId = table.Column<int>(type: "integer", nullable: true),
                    Token = table.Column<string>(type: "text", nullable: false),
                    Statut = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpireLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AccepteeLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitations", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "core",
                table: "PlanModuleEntitlements",
                columns: new[] { "Id", "ModuleCode", "PlanCode" },
                values: new object[] { 18, "ProfilCoach", "Coach" });

            migrationBuilder.CreateIndex(
                name: "IX_CoachSubscriptions_CoachProfileId",
                schema: "core",
                table: "CoachSubscriptions",
                column: "CoachProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_EmailInvite_Type",
                schema: "core",
                table: "Invitations",
                columns: new[] { "EmailInvite", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_EmetteurUserId",
                schema: "core",
                table: "Invitations",
                column: "EmetteurUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_Token",
                schema: "core",
                table: "Invitations",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoachSubscriptions",
                schema: "core");

            migrationBuilder.DropTable(
                name: "Invitations",
                schema: "core");

            migrationBuilder.DeleteData(
                schema: "core",
                table: "PlanModuleEntitlements",
                keyColumn: "Id",
                keyValue: 18);
        }
    }
}
