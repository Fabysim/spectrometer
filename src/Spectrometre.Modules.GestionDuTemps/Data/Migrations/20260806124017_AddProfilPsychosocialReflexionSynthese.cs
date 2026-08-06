using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Modules.GestionDuTemps.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilPsychosocialReflexionSynthese : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProfilsPsychosociaux",
                schema: "gestion_du_temps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CycleId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    SommeilCoucher = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    SommeilLever = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    SommeilReparateur = table.Column<string>(type: "text", nullable: true),
                    ReveilsNocturnes = table.Column<bool>(type: "boolean", nullable: true),
                    EcransAvantSommeil = table.Column<bool>(type: "boolean", nullable: true),
                    TempsPersoQuotidien = table.Column<bool>(type: "boolean", nullable: true),
                    ManqueTempsPerso = table.Column<string>(type: "text", nullable: true),
                    ActivitesRessourcantes = table.Column<string>(type: "text", nullable: true),
                    HoraireTravail = table.Column<string>(type: "text", nullable: true),
                    SurchargePrevisible = table.Column<bool>(type: "boolean", nullable: true),
                    TravailHorsHeures = table.Column<string>(type: "text", nullable: true),
                    DureeTrajets = table.Column<string>(type: "text", nullable: true),
                    DeplacementsImprevus = table.Column<bool>(type: "boolean", nullable: true),
                    EngagementCommunautaire = table.Column<bool>(type: "boolean", nullable: true),
                    EngagementNature = table.Column<string>(type: "text", nullable: true),
                    ProcheMalade = table.Column<bool>(type: "boolean", nullable: true),
                    ModeTravail = table.Column<string>(type: "text", nullable: true),
                    InterruptionsTravail = table.Column<string>(type: "text", nullable: true),
                    AutonomieGestion = table.Column<int>(type: "integer", nullable: true),
                    SentimentPression = table.Column<string>(type: "text", nullable: true),
                    DifficultesConcentration = table.Column<string>(type: "text", nullable: true),
                    DecisionsPrecipitees = table.Column<string>(type: "text", nullable: true),
                    Culpabilite = table.Column<string>(type: "text", nullable: true),
                    ToleranceImprevu = table.Column<string>(type: "text", nullable: true),
                    UtiliseAgenda = table.Column<bool>(type: "boolean", nullable: true),
                    PlanificationAvance = table.Column<string>(type: "text", nullable: true),
                    RituelsQuotidiens = table.Column<bool>(type: "boolean", nullable: true),
                    OuiTropFacile = table.Column<string>(type: "text", nullable: true),
                    CapaciteNon = table.Column<string>(type: "text", nullable: true),
                    GestionAdmin = table.Column<string>(type: "text", nullable: true),
                    StressAdmin = table.Column<string>(type: "text", nullable: true),
                    Tendances = table.Column<List<string>>(type: "text[]", nullable: false),
                    Desequilibres = table.Column<List<string>>(type: "text[]", nullable: false),
                    EmotionsNegatives = table.Column<List<string>>(type: "text[]", nullable: false),
                    EmotionsPositives = table.Column<List<string>>(type: "text[]", nullable: false),
                    ObjectifsProfessionnels = table.Column<List<string>>(type: "text[]", nullable: false),
                    Obstacles = table.Column<List<string>>(type: "text[]", nullable: false),
                    SatisfactionSommeil = table.Column<int>(type: "integer", nullable: true),
                    SatisfactionPerso = table.Column<int>(type: "integer", nullable: true),
                    SatisfactionPro = table.Column<int>(type: "integer", nullable: true),
                    SatisfactionAdmin = table.Column<int>(type: "integer", nullable: true),
                    SatisfactionFamille = table.Column<int>(type: "integer", nullable: true),
                    SatisfactionSocial = table.Column<int>(type: "integer", nullable: true),
                    DirectionSommeil = table.Column<string>(type: "text", nullable: true),
                    DirectionPerso = table.Column<string>(type: "text", nullable: true),
                    DirectionPro = table.Column<string>(type: "text", nullable: true),
                    DirectionAdmin = table.Column<string>(type: "text", nullable: true),
                    DirectionFamille = table.Column<string>(type: "text", nullable: true),
                    DirectionSocial = table.Column<string>(type: "text", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfilsPsychosociaux", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfilsPsychosociaux_Cycles_CycleId",
                        column: x => x.CycleId,
                        principalSchema: "gestion_du_temps",
                        principalTable: "Cycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReflexionsConscientes",
                schema: "gestion_du_temps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CycleId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    SituationActuelle = table.Column<string>(type: "text", nullable: true),
                    SourceIdentifiee = table.Column<bool>(type: "boolean", nullable: true),
                    DateEcheance = table.Column<DateOnly>(type: "date", nullable: true),
                    Ressentis = table.Column<List<string>>(type: "text[]", nullable: false),
                    Sources = table.Column<List<string>>(type: "text[]", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReflexionsConscientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReflexionsConscientes_Cycles_CycleId",
                        column: x => x.CycleId,
                        principalSchema: "gestion_du_temps",
                        principalTable: "Cycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Syntheses",
                schema: "gestion_du_temps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CycleId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ProfilType = table.Column<string>(type: "text", nullable: false),
                    IndiceEquilibre = table.Column<int>(type: "integer", nullable: false),
                    NiveauMaturite = table.Column<int>(type: "integer", nullable: false),
                    ProfilTexte = table.Column<string>(type: "text", nullable: true),
                    IndiceCommentaire = table.Column<string>(type: "text", nullable: true),
                    MaturiteCommentaire = table.Column<string>(type: "text", nullable: true),
                    RecommandationsJson = table.Column<string>(type: "text", nullable: true),
                    AlertesJson = table.Column<string>(type: "text", nullable: true),
                    GenereeParIa = table.Column<bool>(type: "boolean", nullable: false),
                    ProfilSnapshotHash = table.Column<string>(type: "text", nullable: true),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Syntheses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Syntheses_Cycles_CycleId",
                        column: x => x.CycleId,
                        principalSchema: "gestion_du_temps",
                        principalTable: "Cycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProfilsPsychosociaux_CycleId",
                schema: "gestion_du_temps",
                table: "ProfilsPsychosociaux",
                column: "CycleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReflexionsConscientes_CycleId",
                schema: "gestion_du_temps",
                table: "ReflexionsConscientes",
                column: "CycleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Syntheses_CycleId",
                schema: "gestion_du_temps",
                table: "Syntheses",
                column: "CycleId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProfilsPsychosociaux",
                schema: "gestion_du_temps");

            migrationBuilder.DropTable(
                name: "ReflexionsConscientes",
                schema: "gestion_du_temps");

            migrationBuilder.DropTable(
                name: "Syntheses",
                schema: "gestion_du_temps");
        }
    }
}
