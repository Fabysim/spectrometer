using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spectrometre.Modules.Recrutement.Data.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// No-op volontaire : les 6 tables Poste/Candidature/… ne sont plus dans le modèle Recrutement
    /// (snapshot mis à jour), mais on ne génère PAS de <c>DropTable</c> — les schémas tenant déjà
    /// provisionnés gardent ces tables (désormais portées par <c>ProfilEntrepriseDbContext</c>).
    /// Le schéma <c>public</c> (gabarit) est nettoyé manuellement avant <c>database update</c>
    /// ProfilEntreprise (voir rapport de scission).
    /// </remarks>
    public partial class SlimToGuideAndAnalyseOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionnellement vide — voir remarques de la classe.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionnellement vide — le modèle ne peut pas « re-posséder » les tables sans
            // risque de conflit avec ProfilEntreprise.
        }
    }
}
