using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Spectrometre.Core.Migrations
{
    /// <inheritdoc />
    public partial class GeneralizeModuleActivationAndPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ModuleActivations_CompanyId_ModuleCode",
                schema: "core",
                table: "ModuleActivations");

            // Généralisation à un couple (SubjectType, SubjectId) — DEUX colonnes neuves plutôt qu'un
            // renommage de CompanyId (EF Core génère par défaut un RenameColumn CompanyId -> SubjectType,
            // ce qui ferait porter les anciennes VALEURS de CompanyId dans la colonne d'énumération
            // SubjectType : faux et destructeur). SubjectType par défaut à 0 (Company) pour toutes les
            // lignes existantes — elles ont TOUTES été créées comme activations d'entreprise avant ce cycle,
            // il n'existait aucune autre notion de sujet.
            migrationBuilder.AddColumn<int>(
                name: "SubjectType",
                schema: "core",
                table: "ModuleActivations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SubjectId",
                schema: "core",
                table: "ModuleActivations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Recopie la valeur réelle de l'ancien CompanyId dans SubjectId — sans cette étape, SubjectId
            // resterait à 0 pour toutes les lignes existantes et leurs activations deviendraient orphelines
            // (plus aucune entreprise réelle ne les retrouverait via IsActiveAsync).
            migrationBuilder.Sql("UPDATE core.\"ModuleActivations\" SET \"SubjectId\" = \"CompanyId\";");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                schema: "core",
                table: "ModuleActivations");

            migrationBuilder.CreateTable(
                name: "CandidateSubscriptions",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CandidateProfileId = table.Column<int>(type: "integer", nullable: false),
                    PlanCode = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RenewalDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanModuleEntitlements",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlanCode = table.Column<string>(type: "text", nullable: false),
                    ModuleCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanModuleEntitlements", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "core",
                table: "PlanModuleEntitlements",
                columns: new[] { "Id", "ModuleCode", "PlanCode" },
                values: new object[,]
                {
                    { 1, "ProfilCandidat", "Standard" },
                    { 2, "ProfilEntreprise", "Standard" },
                    { 3, "Compatibilite", "Standard" },
                    { 4, "PostesRecrutement", "Standard" },
                    { 5, "Vivier", "Standard" },
                    { 6, "Entretien", "Standard" },
                    { 7, "SuiviEvolutif", "Standard" },
                    { 8, "Analytics", "Standard" },
                    { 9, "ProfilCandidat", "StandardPlusTemps" },
                    { 10, "ProfilEntreprise", "StandardPlusTemps" },
                    { 11, "Compatibilite", "StandardPlusTemps" },
                    { 12, "PostesRecrutement", "StandardPlusTemps" },
                    { 13, "Vivier", "StandardPlusTemps" },
                    { 14, "Entretien", "StandardPlusTemps" },
                    { 15, "SuiviEvolutif", "StandardPlusTemps" },
                    { 16, "Analytics", "StandardPlusTemps" },
                    { 17, "GestionDuTemps", "StandardPlusTemps" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantSubscriptions_CompanyId",
                schema: "core",
                table: "TenantSubscriptions",
                column: "CompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleActivations_SubjectType_SubjectId_ModuleCode",
                schema: "core",
                table: "ModuleActivations",
                columns: new[] { "SubjectType", "SubjectId", "ModuleCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateSubscriptions_CandidateProfileId",
                schema: "core",
                table: "CandidateSubscriptions",
                column: "CandidateProfileId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanModuleEntitlements_PlanCode_ModuleCode",
                schema: "core",
                table: "PlanModuleEntitlements",
                columns: new[] { "PlanCode", "ModuleCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidateSubscriptions",
                schema: "core");

            migrationBuilder.DropTable(
                name: "PlanModuleEntitlements",
                schema: "core");

            migrationBuilder.DropIndex(
                name: "IX_TenantSubscriptions_CompanyId",
                schema: "core",
                table: "TenantSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_ModuleActivations_SubjectType_SubjectId_ModuleCode",
                schema: "core",
                table: "ModuleActivations");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                schema: "core",
                table: "ModuleActivations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Symétrique du Up() : ne restaure que les activations d'entreprise (SubjectType = 0/Company) —
            // toute activation candidat créée depuis ce cycle n'a pas d'équivalent dans l'ancien modèle et
            // est nécessairement perdue en cas de rollback, comme pour toute donnée nouvellement introduite.
            migrationBuilder.Sql("UPDATE core.\"ModuleActivations\" SET \"CompanyId\" = \"SubjectId\" WHERE \"SubjectType\" = 0;");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                schema: "core",
                table: "ModuleActivations");

            migrationBuilder.DropColumn(
                name: "SubjectType",
                schema: "core",
                table: "ModuleActivations");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleActivations_CompanyId_ModuleCode",
                schema: "core",
                table: "ModuleActivations",
                columns: new[] { "CompanyId", "ModuleCode" },
                unique: true);
        }
    }
}
