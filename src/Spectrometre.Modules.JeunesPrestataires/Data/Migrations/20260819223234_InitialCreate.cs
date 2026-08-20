using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Modules.JeunesPrestataires.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rework depuis la première version (CandidateProfileId) — aucune donnée prod à préserver.
            migrationBuilder.Sql("""DROP TABLE IF EXISTS jeunes_prestataires."ConsentementsParentaux" CASCADE;""");

            migrationBuilder.EnsureSchema(
                name: "jeunes_prestataires");

            migrationBuilder.CreateTable(
                name: "ConsentementsParentaux",
                schema: "jeunes_prestataires",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JeuneProfileId = table.Column<int>(type: "integer", nullable: false),
                    Parent1Nom = table.Column<string>(type: "text", nullable: true),
                    Parent1Lien = table.Column<string>(type: "text", nullable: true),
                    Parent1Adresse = table.Column<string>(type: "text", nullable: true),
                    Parent1Telephone = table.Column<string>(type: "text", nullable: true),
                    Parent1Email = table.Column<string>(type: "text", nullable: true),
                    Parent2Nom = table.Column<string>(type: "text", nullable: true),
                    Parent2Lien = table.Column<string>(type: "text", nullable: true),
                    Parent2Adresse = table.Column<string>(type: "text", nullable: true),
                    Parent2Telephone = table.Column<string>(type: "text", nullable: true),
                    Parent2Email = table.Column<string>(type: "text", nullable: true),
                    AutorisationMissions = table.Column<bool>(type: "boolean", nullable: false),
                    AutorisationRevenus = table.Column<bool>(type: "boolean", nullable: false),
                    PartParascolairePourcent = table.Column<decimal>(type: "numeric", nullable: true),
                    PartArgentDePochePourcent = table.Column<decimal>(type: "numeric", nullable: true),
                    AutreAffectation = table.Column<string>(type: "text", nullable: true),
                    ModalitesVersement = table.Column<string>(type: "text", nullable: true),
                    AutorisationDonneesEtImage = table.Column<bool>(type: "boolean", nullable: false),
                    EngagementScolariteSanteEquilibre = table.Column<bool>(type: "boolean", nullable: false),
                    EngagementInformerContraintes = table.Column<bool>(type: "boolean", nullable: false),
                    EngagementEncouragerCharte = table.Column<bool>(type: "boolean", nullable: false),
                    EngagementSignalerMissionInadaptee = table.Column<bool>(type: "boolean", nullable: false),
                    EngagementCollaborerCoach = table.Column<bool>(type: "boolean", nullable: false),
                    NomJeuneConfirmation = table.Column<string>(type: "text", nullable: true),
                    NomParent1Confirmation = table.Column<string>(type: "text", nullable: true),
                    NomParent2Confirmation = table.Column<string>(type: "text", nullable: true),
                    ValideLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentementsParentaux", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvitationsJeunesPrestataires",
                schema: "jeunes_prestataires",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvitationId = table.Column<int>(type: "integer", nullable: false),
                    Nom = table.Column<string>(type: "text", nullable: false),
                    Prenoms = table.Column<string>(type: "text", nullable: false),
                    DateNaissance = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvitationsJeunesPrestataires", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JeuneProfiles",
                schema: "jeunes_prestataires",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Nom = table.Column<string>(type: "text", nullable: false),
                    Prenoms = table.Column<string>(type: "text", nullable: false),
                    DateNaissance = table.Column<DateOnly>(type: "date", nullable: false),
                    InvitationId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JeuneProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsentementsParentaux_JeuneProfileId",
                schema: "jeunes_prestataires",
                table: "ConsentementsParentaux",
                column: "JeuneProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvitationsJeunesPrestataires_InvitationId",
                schema: "jeunes_prestataires",
                table: "InvitationsJeunesPrestataires",
                column: "InvitationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JeuneProfiles_InvitationId",
                schema: "jeunes_prestataires",
                table: "JeuneProfiles",
                column: "InvitationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JeuneProfiles_UserId",
                schema: "jeunes_prestataires",
                table: "JeuneProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsentementsParentaux",
                schema: "jeunes_prestataires");

            migrationBuilder.DropTable(
                name: "InvitationsJeunesPrestataires",
                schema: "jeunes_prestataires");

            migrationBuilder.DropTable(
                name: "JeuneProfiles",
                schema: "jeunes_prestataires");
        }
    }
}
