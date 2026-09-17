using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SoodalLife.Api.Infrastructure.Persistence;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoodalLifeDbContext))]
[Migration("20260819041000_ImproveSubscriptionBillingLifecycle")]
public partial class ImproveSubscriptionBillingLifecycle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(name: "CK_subscription_contracts_status", table: "subscription_contracts");
        migrationBuilder.AddCheckConstraint(name: "CK_subscription_contracts_status", table: "subscription_contracts", sql: "[status_code] IN ('PAYMENT_PENDING','ACTIVE','PAUSED','TERMINATION_REQUESTED','TERMINATED')");
        migrationBuilder.Sql("UPDATE subscription_contracts SET billing_status_code = 'LEGACY_REVIEW_REQUIRED' WHERE billing_status_code IS NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("UPDATE subscription_contracts SET status_code = 'PAUSED', billing_status_code = 'LEGACY_REVIEW_REQUIRED' WHERE status_code = 'PAYMENT_PENDING';");
        migrationBuilder.DropCheckConstraint(name: "CK_subscription_contracts_status", table: "subscription_contracts");
        migrationBuilder.AddCheckConstraint(name: "CK_subscription_contracts_status", table: "subscription_contracts", sql: "[status_code] IN ('ACTIVE','PAUSED','TERMINATION_REQUESTED','TERMINATED')");
    }
}
