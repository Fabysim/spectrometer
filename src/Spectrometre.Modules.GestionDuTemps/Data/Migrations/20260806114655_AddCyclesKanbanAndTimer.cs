using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Modules.GestionDuTemps.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCyclesKanbanAndTimer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ordre retravaillé à la main par rapport au script généré par défaut (qui aurait perdu des
            // données : CycleId ajouté avec defaultValue 0 — une FK invalide — et Activites.Statut
            // simplement supprimé sans reporter l'information dans KanbanStatuts). Étapes : (1) créer les
            // nouvelles tables, (2) ajouter CycleId en NULLABLE, (3) backfill (un cycle #1 EnCours par
            // utilisateur déjà présent + un KanbanStatut par activité existante, dérivé de l'ancien Statut),
            // (4) rendre CycleId NOT NULL, (5) seulement alors poser les contraintes/index/FK définitifs.

            migrationBuilder.DropIndex(
                name: "IX_TypesDeTemps_UserId_Cle",
                schema: "gestion_du_temps",
                table: "TypesDeTemps");

            migrationBuilder.CreateTable(
                name: "Cycles",
                schema: "gestion_du_temps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    NumeroCycle = table.Column<int>(type: "integer", nullable: false),
                    Statut = table.Column<string>(type: "text", nullable: false),
                    DemarreLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClotureLe = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KanbanStatuts",
                schema: "gestion_du_temps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ActiviteId = table.Column<int>(type: "integer", nullable: false),
                    Statut = table.Column<string>(type: "text", nullable: false),
                    TimerDebutUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TimerDureeReelleMs = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KanbanStatuts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KanbanStatuts_Activites_ActiviteId",
                        column: x => x.ActiviteId,
                        principalSchema: "gestion_du_temps",
                        principalTable: "Activites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // CycleId ajouté NULLABLE pour l'instant — le backfill ci-dessous lui donne une vraie valeur
            // avant qu'on le rende obligatoire (voir AlterColumn plus bas).
            migrationBuilder.AddColumn<int>(
                name: "CycleId",
                schema: "gestion_du_temps",
                table: "TypesDeTemps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CycleId",
                schema: "gestion_du_temps",
                table: "Activites",
                type: "integer",
                nullable: true);

            // Un cycle #1 "EnCours" par utilisateur déjà présent (TypesDeTemps ou Activites) — ces
            // utilisateurs existaient avant l'introduction des cycles dans ce cycle de travail, ils n'ont
            // donc jamais eu qu'un seul "cycle" implicite. DemarreLe = maintenant : leur date de début
            // réelle n'est pas connue plus précisément (CreatedAt de leur plus ancien TypeDeTemps serait une
            // approximation, mais aucune donnée fiable de "début de saison" n'existait avant ce cycle).
            migrationBuilder.Sql("""
                INSERT INTO gestion_du_temps."Cycles" ("UserId", "NumeroCycle", "Statut", "DemarreLe")
                SELECT DISTINCT u."UserId", 1, 'EnCours', now()
                FROM (
                    SELECT "UserId" FROM gestion_du_temps."TypesDeTemps"
                    UNION
                    SELECT "UserId" FROM gestion_du_temps."Activites"
                ) AS u;
                """);

            migrationBuilder.Sql("""
                UPDATE gestion_du_temps."TypesDeTemps" t
                SET "CycleId" = c."Id"
                FROM gestion_du_temps."Cycles" c
                WHERE c."UserId" = t."UserId";
                """);

            migrationBuilder.Sql("""
                UPDATE gestion_du_temps."Activites" a
                SET "CycleId" = c."Id"
                FROM gestion_du_temps."Cycles" c
                WHERE c."UserId" = a."UserId";
                """);

            // Reporte l'ancien statut binaire (À faire/Fait) vers le nouveau KanbanStatut à 3 colonnes avant
            // de le supprimer : Fait -> Termine, AFaire -> AFaire — aucune activité existante ne perd son
            // état d'avancement. Pas de temps réel à reporter (le minuteur n'existait pas avant ce cycle).
            migrationBuilder.Sql("""
                INSERT INTO gestion_du_temps."KanbanStatuts" ("ActiviteId", "Statut", "TimerDureeReelleMs", "UpdatedAt")
                SELECT "Id", CASE WHEN "Statut" = 'Fait' THEN 'Termine' ELSE 'AFaire' END, 0, now()
                FROM gestion_du_temps."Activites";
                """);

            migrationBuilder.DropColumn(
                name: "Statut",
                schema: "gestion_du_temps",
                table: "Activites");

            migrationBuilder.AlterColumn<int>(
                name: "CycleId",
                schema: "gestion_du_temps",
                table: "TypesDeTemps",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CycleId",
                schema: "gestion_du_temps",
                table: "Activites",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TypesDeTemps_CycleId_Cle",
                schema: "gestion_du_temps",
                table: "TypesDeTemps",
                columns: new[] { "CycleId", "Cle" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Activites_CycleId",
                schema: "gestion_du_temps",
                table: "Activites",
                column: "CycleId");

            migrationBuilder.CreateIndex(
                name: "IX_Cycles_UserId",
                schema: "gestion_du_temps",
                table: "Cycles",
                column: "UserId",
                unique: true,
                filter: "\"Statut\" = 'EnCours'");

            migrationBuilder.CreateIndex(
                name: "IX_Cycles_UserId_NumeroCycle",
                schema: "gestion_du_temps",
                table: "Cycles",
                columns: new[] { "UserId", "NumeroCycle" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KanbanStatuts_ActiviteId",
                schema: "gestion_du_temps",
                table: "KanbanStatuts",
                column: "ActiviteId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Activites_Cycles_CycleId",
                schema: "gestion_du_temps",
                table: "Activites",
                column: "CycleId",
                principalSchema: "gestion_du_temps",
                principalTable: "Cycles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TypesDeTemps_Cycles_CycleId",
                schema: "gestion_du_temps",
                table: "TypesDeTemps",
                column: "CycleId",
                principalSchema: "gestion_du_temps",
                principalTable: "Cycles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activites_Cycles_CycleId",
                schema: "gestion_du_temps",
                table: "Activites");

            migrationBuilder.DropForeignKey(
                name: "FK_TypesDeTemps_Cycles_CycleId",
                schema: "gestion_du_temps",
                table: "TypesDeTemps");

            migrationBuilder.DropTable(
                name: "Cycles",
                schema: "gestion_du_temps");

            migrationBuilder.DropTable(
                name: "KanbanStatuts",
                schema: "gestion_du_temps");

            migrationBuilder.DropIndex(
                name: "IX_TypesDeTemps_CycleId_Cle",
                schema: "gestion_du_temps",
                table: "TypesDeTemps");

            migrationBuilder.DropIndex(
                name: "IX_Activites_CycleId",
                schema: "gestion_du_temps",
                table: "Activites");

            migrationBuilder.DropColumn(
                name: "CycleId",
                schema: "gestion_du_temps",
                table: "TypesDeTemps");

            migrationBuilder.DropColumn(
                name: "CycleId",
                schema: "gestion_du_temps",
                table: "Activites");

            migrationBuilder.AddColumn<string>(
                name: "Statut",
                schema: "gestion_du_temps",
                table: "Activites",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TypesDeTemps_UserId_Cle",
                schema: "gestion_du_temps",
                table: "TypesDeTemps",
                columns: new[] { "UserId", "Cle" },
                unique: true);
        }
    }
}
