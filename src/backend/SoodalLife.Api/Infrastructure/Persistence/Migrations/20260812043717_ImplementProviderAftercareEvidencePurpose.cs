using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ImplementProviderAftercareEvidencePurpose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_files_purpose",
                table: "files");

            migrationBuilder.AddCheckConstraint(
                name: "CK_files_purpose",
                table: "files",
                sql: "[purpose_code] IN ('PROVIDER_DOCUMENT','REQUEST_ANSWER','COMPLETION_EVIDENCE','AFTER_SERVICE','DISPUTE_EVIDENCE','REVIEW','REPORT_EVIDENCE','SANCTION_APPEAL_EVIDENCE')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_files_purpose",
                table: "files");

            migrationBuilder.AddCheckConstraint(
                name: "CK_files_purpose",
                table: "files",
                sql: "[purpose_code] IN ('PROVIDER_DOCUMENT','REQUEST_ANSWER','COMPLETION_EVIDENCE','AFTER_SERVICE','REVIEW','REPORT_EVIDENCE','SANCTION_APPEAL_EVIDENCE')");
        }
    }
}
