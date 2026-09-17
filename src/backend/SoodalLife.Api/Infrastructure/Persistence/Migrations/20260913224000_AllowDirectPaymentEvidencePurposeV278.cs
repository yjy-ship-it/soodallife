using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SoodalLife.Api.Infrastructure.Persistence;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoodalLifeDbContext))]
[Migration("20260913224000_AllowDirectPaymentEvidencePurposeV278")]
public partial class AllowDirectPaymentEvidencePurposeV278 : Migration
{
    private const string PreviousPurposes = "[purpose_code] IN ('PROVIDER_DOCUMENT','REQUEST_ANSWER','COMPLETION_EVIDENCE','AFTER_SERVICE','DISPUTE_EVIDENCE','REVIEW','REPORT_EVIDENCE','SANCTION_APPEAL_EVIDENCE','PROVIDER_PUBLIC_LOGO','PROVIDER_PUBLIC_PHOTO','CHAT_ATTACHMENT','HELP_ROOM_PHOTO')";
    private const string CurrentPurposes = "[purpose_code] IN ('PROVIDER_DOCUMENT','REQUEST_ANSWER','COMPLETION_EVIDENCE','AFTER_SERVICE','DISPUTE_EVIDENCE','REVIEW','REPORT_EVIDENCE','SANCTION_APPEAL_EVIDENCE','PROVIDER_PUBLIC_LOGO','PROVIDER_PUBLIC_PHOTO','CHAT_ATTACHMENT','HELP_ROOM_PHOTO','DIRECT_PAYMENT_EVIDENCE')";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(name: "CK_files_purpose", table: "files");
        migrationBuilder.AddCheckConstraint(name: "CK_files_purpose", table: "files", sql: CurrentPurposes);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(name: "CK_files_purpose", table: "files");
        migrationBuilder.AddCheckConstraint(name: "CK_files_purpose", table: "files", sql: PreviousPurposes);
    }
}
