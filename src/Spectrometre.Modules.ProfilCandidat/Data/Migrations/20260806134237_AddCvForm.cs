using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Modules.ProfilCandidat.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCvForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CvCaracteristiquesPersonnelles",
                schema: "profil_candidat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CandidateProfileId = table.Column<int>(type: "integer", nullable: false),
                    QualitesPersonnelles = table.Column<string>(type: "text", nullable: true),
                    AptitudesProfessionnelles = table.Column<string>(type: "text", nullable: true),
                    AttitudesRelationnelles = table.Column<string>(type: "text", nullable: true),
                    CapaciteSousPression = table.Column<string>(type: "text", nullable: true),
                    DisponibiliteMobilite = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CvCaracteristiquesPersonnelles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CvCompetencesEtudes",
                schema: "profil_candidat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CandidateProfileId = table.Column<int>(type: "integer", nullable: false),
                    SpecialitePrincipale = table.Column<string>(type: "text", nullable: true),
                    CompetencesTechniques = table.Column<string>(type: "text", nullable: true),
                    ConnaissancesTheoriques = table.Column<string>(type: "text", nullable: true),
                    LanguesMaitrisees = table.Column<string>(type: "text", nullable: true),
                    OutilsLogicielsMethodes = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CvCompetencesEtudes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CvCoordonnees",
                schema: "profil_candidat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CandidateProfileId = table.Column<int>(type: "integer", nullable: false),
                    Nom = table.Column<string>(type: "text", nullable: true),
                    Prenoms = table.Column<string>(type: "text", nullable: true),
                    DateNaissance = table.Column<DateOnly>(type: "date", nullable: true),
                    LieuNaissance = table.Column<string>(type: "text", nullable: true),
                    Nationalite = table.Column<string>(type: "text", nullable: true),
                    AdresseComplete = table.Column<string>(type: "text", nullable: true),
                    Telephone = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    ProfilOuPosteRecherche = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CvCoordonnees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CvDeclarations",
                schema: "profil_candidat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CandidateProfileId = table.Column<int>(type: "integer", nullable: false),
                    CertificationExactitude = table.Column<bool>(type: "boolean", nullable: false),
                    ConsentementConsultation = table.Column<bool>(type: "boolean", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: true),
                    NomSignataire = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CvDeclarations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CvExperiences",
                schema: "profil_candidat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CandidateProfileId = table.Column<int>(type: "integer", nullable: false),
                    Periode = table.Column<string>(type: "text", nullable: true),
                    EntrepriseOrganisationOuStage = table.Column<string>(type: "text", nullable: true),
                    FonctionOuActiviteExercee = table.Column<string>(type: "text", nullable: true),
                    CompetencesDeveloppees = table.Column<string>(type: "text", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CvExperiences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CvFormations",
                schema: "profil_candidat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CandidateProfileId = table.Column<int>(type: "integer", nullable: false),
                    Periode = table.Column<string>(type: "text", nullable: true),
                    Etablissement = table.Column<string>(type: "text", nullable: true),
                    DiplomeCertificatOuNiveau = table.Column<string>(type: "text", nullable: true),
                    DomaineEtudes = table.Column<string>(type: "text", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CvFormations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CvLoisirs",
                schema: "profil_candidat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CandidateProfileId = table.Column<int>(type: "integer", nullable: false),
                    LoisirsPreferes = table.Column<string>(type: "text", nullable: true),
                    ActivitesSportivesCulturelles = table.Column<string>(type: "text", nullable: true),
                    EngagementsAssociatifs = table.Column<string>(type: "text", nullable: true),
                    AutresCentresInteret = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CvLoisirs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CvReferences",
                schema: "profil_candidat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CandidateProfileId = table.Column<int>(type: "integer", nullable: false),
                    NomPrenom = table.Column<string>(type: "text", nullable: true),
                    Fonction = table.Column<string>(type: "text", nullable: true),
                    EntrepriseOrganisation = table.Column<string>(type: "text", nullable: true),
                    TelephoneOuEmail = table.Column<string>(type: "text", nullable: true),
                    LienAvecPostulant = table.Column<string>(type: "text", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CvReferences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CvCaracteristiquesPersonnelles_CandidateProfileId",
                schema: "profil_candidat",
                table: "CvCaracteristiquesPersonnelles",
                column: "CandidateProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CvCompetencesEtudes_CandidateProfileId",
                schema: "profil_candidat",
                table: "CvCompetencesEtudes",
                column: "CandidateProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CvCoordonnees_CandidateProfileId",
                schema: "profil_candidat",
                table: "CvCoordonnees",
                column: "CandidateProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CvDeclarations_CandidateProfileId",
                schema: "profil_candidat",
                table: "CvDeclarations",
                column: "CandidateProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CvExperiences_CandidateProfileId",
                schema: "profil_candidat",
                table: "CvExperiences",
                column: "CandidateProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_CvFormations_CandidateProfileId",
                schema: "profil_candidat",
                table: "CvFormations",
                column: "CandidateProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_CvLoisirs_CandidateProfileId",
                schema: "profil_candidat",
                table: "CvLoisirs",
                column: "CandidateProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CvReferences_CandidateProfileId",
                schema: "profil_candidat",
                table: "CvReferences",
                column: "CandidateProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CvCaracteristiquesPersonnelles",
                schema: "profil_candidat");

            migrationBuilder.DropTable(
                name: "CvCompetencesEtudes",
                schema: "profil_candidat");

            migrationBuilder.DropTable(
                name: "CvCoordonnees",
                schema: "profil_candidat");

            migrationBuilder.DropTable(
                name: "CvDeclarations",
                schema: "profil_candidat");

            migrationBuilder.DropTable(
                name: "CvExperiences",
                schema: "profil_candidat");

            migrationBuilder.DropTable(
                name: "CvFormations",
                schema: "profil_candidat");

            migrationBuilder.DropTable(
                name: "CvLoisirs",
                schema: "profil_candidat");

            migrationBuilder.DropTable(
                name: "CvReferences",
                schema: "profil_candidat");
        }
    }
}
