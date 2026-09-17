using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoodalLifeDbContext))]
[Migration("20260818090000_AllowProviderApprovalResubmitAction")]
public partial class AllowProviderApprovalResubmitAction : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(name: "CK_provider_approval_events_action", table: "provider_approval_events");
        migrationBuilder.AddCheckConstraint(
            name: "CK_provider_approval_events_action",
            table: "provider_approval_events",
            sql: "[action_code] IN ('APPROVE','REJECT','SUSPEND','RESUME','RESUBMIT')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(name: "CK_provider_approval_events_action", table: "provider_approval_events");
        migrationBuilder.AddCheckConstraint(
            name: "CK_provider_approval_events_action",
            table: "provider_approval_events",
            sql: "[action_code] IN ('APPROVE','REJECT','SUSPEND','RESUME')");
    }
}
