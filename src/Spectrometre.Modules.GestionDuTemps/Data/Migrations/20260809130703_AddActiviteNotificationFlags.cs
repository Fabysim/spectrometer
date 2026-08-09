using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spectrometre.Modules.GestionDuTemps.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActiviteNotificationFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotificationDebutEnvoyee",
                schema: "gestion_du_temps",
                table: "Activites",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotificationFinEnvoyee",
                schema: "gestion_du_temps",
                table: "Activites",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationDebutEnvoyee",
                schema: "gestion_du_temps",
                table: "Activites");

            migrationBuilder.DropColumn(
                name: "NotificationFinEnvoyee",
                schema: "gestion_du_temps",
                table: "Activites");
        }
    }
}
