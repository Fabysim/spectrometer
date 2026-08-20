using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddParticulierSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParticulierSubscriptions",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParticulierProfileId = table.Column<int>(type: "integer", nullable: false),
                    PlanCode = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RenewalDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParticulierSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParticulierSubscriptions_ParticulierProfileId",
                schema: "core",
                table: "ParticulierSubscriptions",
                column: "ParticulierProfileId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParticulierSubscriptions",
                schema: "core");
        }
    }
}
