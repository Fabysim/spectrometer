using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Spectrometre.Modules.PostesRecrutement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCritereEvaluation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CriteresEvaluation",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PosteId = table.Column<int>(type: "integer", nullable: false),
                    Categorie = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Libelle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NiveauRequis = table.Column<int>(type: "integer", nullable: false),
                    OrdreAffichage = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriteresEvaluation", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CriteresEvaluation_PosteId_OrdreAffichage",
                schema: "public",
                table: "CriteresEvaluation",
                columns: new[] { "PosteId", "OrdreAffichage" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CriteresEvaluation",
                schema: "public");
        }
    }
}
