using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Modules.JeunesPrestataires.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanActionAndSyntheseValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ValideeLe",
                schema: "jeunes_prestataires",
                table: "AutoObservationSynthesesGenerees",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValideeParCoachUserId",
                schema: "jeunes_prestataires",
                table: "AutoObservationSynthesesGenerees",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlansActionAutoObservation",
                schema: "jeunes_prestataires",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JeuneProfileId = table.Column<int>(type: "integer", nullable: false),
                    ObjectifPrincipal = table.Column<string>(type: "text", nullable: true),
                    PremiereAction = table.Column<string>(type: "text", nullable: true),
                    ResponsableSuivi = table.Column<string>(type: "text", nullable: true),
                    Echeance = table.Column<DateOnly>(type: "date", nullable: true),
                    IndicateurReussite = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlansActionAutoObservation", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlansActionAutoObservation_JeuneProfileId",
                schema: "jeunes_prestataires",
                table: "PlansActionAutoObservation",
                column: "JeuneProfileId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlansActionAutoObservation",
                schema: "jeunes_prestataires");

            migrationBuilder.DropColumn(
                name: "ValideeLe",
                schema: "jeunes_prestataires",
                table: "AutoObservationSynthesesGenerees");

            migrationBuilder.DropColumn(
                name: "ValideeParCoachUserId",
                schema: "jeunes_prestataires",
                table: "AutoObservationSynthesesGenerees");
        }
    }
}
