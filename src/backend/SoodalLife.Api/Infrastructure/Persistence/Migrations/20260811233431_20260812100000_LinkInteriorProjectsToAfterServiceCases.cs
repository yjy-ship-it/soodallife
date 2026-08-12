using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class _20260812100000_LinkInteriorProjectsToAfterServiceCases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_dispute_cases_source",
                table: "dispute_cases");

            migrationBuilder.DropCheckConstraint(
                name: "CK_after_service_cases_source",
                table: "after_service_cases");

            migrationBuilder.AddColumn<long>(
                name: "interior_project_id",
                table: "after_service_cases",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_after_service_cases_interior_project_id",
                table: "after_service_cases",
                column: "interior_project_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_after_service_cases_source",
                table: "after_service_cases",
                sql: "([transaction_id] IS NOT NULL AND [subscription_visit_schedule_id] IS NULL AND [interior_project_id] IS NULL) OR ([transaction_id] IS NULL AND [subscription_visit_schedule_id] IS NOT NULL AND [interior_project_id] IS NULL) OR ([transaction_id] IS NULL AND [subscription_visit_schedule_id] IS NULL AND [interior_project_id] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_after_service_cases_interior_projects_interior_project_id",
                table: "after_service_cases",
                column: "interior_project_id",
                principalTable: "interior_projects",
                principalColumn: "id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_dispute_cases_source",
                table: "dispute_cases",
                sql: "([transaction_id] IS NOT NULL AND [subscription_visit_schedule_id] IS NULL AND [interior_project_id] IS NULL) OR ([transaction_id] IS NULL AND [subscription_visit_schedule_id] IS NOT NULL AND [interior_project_id] IS NULL) OR ([transaction_id] IS NULL AND [subscription_visit_schedule_id] IS NULL AND [interior_project_id] IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_dispute_cases_source",
                table: "dispute_cases");

            migrationBuilder.DropForeignKey(
                name: "FK_after_service_cases_interior_projects_interior_project_id",
                table: "after_service_cases");

            migrationBuilder.DropIndex(
                name: "IX_after_service_cases_interior_project_id",
                table: "after_service_cases");

            migrationBuilder.DropCheckConstraint(
                name: "CK_after_service_cases_source",
                table: "after_service_cases");

            migrationBuilder.DropColumn(
                name: "interior_project_id",
                table: "after_service_cases");

            migrationBuilder.AddCheckConstraint(
                name: "CK_after_service_cases_source",
                table: "after_service_cases",
                sql: "([transaction_id] IS NOT NULL AND [subscription_visit_schedule_id] IS NULL) OR ([transaction_id] IS NULL AND [subscription_visit_schedule_id] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_dispute_cases_source",
                table: "dispute_cases",
                sql: "([transaction_id] IS NOT NULL AND [subscription_visit_schedule_id] IS NULL) OR ([transaction_id] IS NULL AND [subscription_visit_schedule_id] IS NOT NULL)");
        }
    }
}
