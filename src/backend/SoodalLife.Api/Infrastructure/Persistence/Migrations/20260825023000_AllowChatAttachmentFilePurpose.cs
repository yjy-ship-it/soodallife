using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations;

public partial class AllowChatAttachmentFilePurpose : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_files_purpose",
            table: "files");

        migrationBuilder.AddCheckConstraint(
            name: "CK_files_purpose",
            table: "files",
            sql: "[purpose_code] IN ('PROVIDER_DOCUMENT','REQUEST_ANSWER','COMPLETION_EVIDENCE','AFTER_SERVICE','DISPUTE_EVIDENCE','REVIEW','REPORT_EVIDENCE','SANCTION_APPEAL_EVIDENCE','PROVIDER_PUBLIC_LOGO','PROVIDER_PUBLIC_PHOTO','CHAT_ATTACHMENT')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_files_purpose",
            table: "files");

        migrationBuilder.AddCheckConstraint(
            name: "CK_files_purpose",
            table: "files",
            sql: "[purpose_code] IN ('PROVIDER_DOCUMENT','REQUEST_ANSWER','COMPLETION_EVIDENCE','AFTER_SERVICE','DISPUTE_EVIDENCE','REVIEW','REPORT_EVIDENCE','SANCTION_APPEAL_EVIDENCE','PROVIDER_PUBLIC_LOGO','PROVIDER_PUBLIC_PHOTO')");
    }
}
