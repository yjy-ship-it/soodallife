using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoodalLifeDbContext))]
[Migration("20260818030000_AllowProviderPromotionImageFilePurposes")]
public partial class AllowProviderPromotionImageFilePurposes : Migration
{
    private const string PreviousPurposes = "[purpose_code] IN ('PROVIDER_DOCUMENT','REQUEST_ANSWER','COMPLETION_EVIDENCE','AFTER_SERVICE','DISPUTE_EVIDENCE','REVIEW','REPORT_EVIDENCE','SANCTION_APPEAL_EVIDENCE')";
    private const string ExpandedPurposes = "[purpose_code] IN ('PROVIDER_DOCUMENT','REQUEST_ANSWER','COMPLETION_EVIDENCE','AFTER_SERVICE','DISPUTE_EVIDENCE','REVIEW','REPORT_EVIDENCE','SANCTION_APPEAL_EVIDENCE','PROVIDER_PUBLIC_LOGO','PROVIDER_PUBLIC_PHOTO')";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(name: "CK_files_purpose", table: "files");
        migrationBuilder.AddCheckConstraint(name: "CK_files_purpose", table: "files", sql: ExpandedPurposes);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(name: "CK_files_purpose", table: "files");
        migrationBuilder.AddCheckConstraint(name: "CK_files_purpose", table: "files", sql: PreviousPurposes);
    }
}
