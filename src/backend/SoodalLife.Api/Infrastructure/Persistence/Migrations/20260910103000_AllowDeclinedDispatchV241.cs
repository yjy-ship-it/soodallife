using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoodalLifeDbContext))]
[Migration("20260910103000_AllowDeclinedDispatchV241")]
public sealed class AllowDeclinedDispatchV241 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(name: "CK_dispatch_candidates_status", table: "dispatch_candidates");
        migrationBuilder.DropCheckConstraint(name: "CK_request_dispatches_status", table: "request_dispatches");
        migrationBuilder.AddCheckConstraint(name: "CK_dispatch_candidates_status", table: "dispatch_candidates", sql: "[status_code] IN ('ELIGIBLE','INELIGIBLE','DISPATCHED','DECLINED','EXPIRED')");
        migrationBuilder.AddCheckConstraint(name: "CK_request_dispatches_status", table: "request_dispatches", sql: "[status_code] IN ('AVAILABLE','VIEWED','RESPONDED','DECLINED','EXPIRED')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(name: "CK_dispatch_candidates_status", table: "dispatch_candidates");
        migrationBuilder.DropCheckConstraint(name: "CK_request_dispatches_status", table: "request_dispatches");
        migrationBuilder.AddCheckConstraint(name: "CK_dispatch_candidates_status", table: "dispatch_candidates", sql: "[status_code] IN ('ELIGIBLE','INELIGIBLE','DISPATCHED','EXPIRED')");
        migrationBuilder.AddCheckConstraint(name: "CK_request_dispatches_status", table: "request_dispatches", sql: "[status_code] IN ('AVAILABLE','VIEWED','RESPONDED','EXPIRED')");
    }
}
