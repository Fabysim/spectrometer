using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Spectrometre.Core.Migrations
{
    /// <inheritdoc />
    public partial class PurgeNonOfficialPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ne conserve que les 4 plans seedés (PlanCodes.*) — purge des plans de test (plan-histo-*, plan-test-*, …).
            migrationBuilder.Sql(
                """
                DELETE FROM core."PlanModuleEntitlements"
                WHERE "PlanCode" NOT IN ('Standard', 'StandardPlusTemps', 'Coach', 'CoachPlusTemps');
                """);
            migrationBuilder.Sql(
                """
                DELETE FROM core."Plans"
                WHERE "Code" NOT IN ('Standard', 'StandardPlusTemps', 'Coach', 'CoachPlusTemps');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irréversible : les plans parasites ne sont pas restaurés.
        }
    }
}
