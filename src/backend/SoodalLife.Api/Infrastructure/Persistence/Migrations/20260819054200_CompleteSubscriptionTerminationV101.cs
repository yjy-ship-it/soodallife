using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SoodalLife.Api.Infrastructure.Persistence;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoodalLifeDbContext))]
[Migration("20260819054200_CompleteSubscriptionTerminationV101")]
public partial class CompleteSubscriptionTerminationV101 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint("CK_subscription_refund_adjustments_status", "subscription_refund_adjustments");
        migrationBuilder.AddColumn<string>("calculation_json", "subscription_refund_adjustments", "nvarchar(max)", nullable: true);
        migrationBuilder.AddColumn<string>("external_refund_reference", "subscription_refund_adjustments", "varchar(200)", unicode: false, maxLength: 200, nullable: true);
        migrationBuilder.AddColumn<string>("failure_code", "subscription_refund_adjustments", "varchar(100)", unicode: false, maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<string>("failure_reason", "subscription_refund_adjustments", "nvarchar(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<decimal>("provider_adjustment_amount", "subscription_refund_adjustments", "decimal(19,4)", nullable: false, defaultValue: 0m);
        migrationBuilder.AddColumn<DateTime>("gateway_terminated_at", "subscription_contracts", "datetime2(7)", nullable: true);
        migrationBuilder.AddColumn<string>("gateway_termination_failure_reason", "subscription_contracts", "nvarchar(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<string>("gateway_termination_status_code", "subscription_contracts", "varchar(30)", unicode: false, maxLength: 30, nullable: true);
        migrationBuilder.AddCheckConstraint("CK_subscription_refund_adjustments_status", "subscription_refund_adjustments", "[status_code] IN ('REQUESTED','WAITING_CASES','MANUAL_REQUIRED','APPROVED','PROCESSING','COMPLETED','REJECTED','CANCELLED','FAILED')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint("CK_subscription_refund_adjustments_status", "subscription_refund_adjustments");
        migrationBuilder.DropColumn("calculation_json", "subscription_refund_adjustments");
        migrationBuilder.DropColumn("external_refund_reference", "subscription_refund_adjustments");
        migrationBuilder.DropColumn("failure_code", "subscription_refund_adjustments");
        migrationBuilder.DropColumn("failure_reason", "subscription_refund_adjustments");
        migrationBuilder.DropColumn("provider_adjustment_amount", "subscription_refund_adjustments");
        migrationBuilder.DropColumn("gateway_terminated_at", "subscription_contracts");
        migrationBuilder.DropColumn("gateway_termination_failure_reason", "subscription_contracts");
        migrationBuilder.DropColumn("gateway_termination_status_code", "subscription_contracts");
        migrationBuilder.AddCheckConstraint("CK_subscription_refund_adjustments_status", "subscription_refund_adjustments", "[status_code] IN ('REQUESTED','APPROVED','COMPLETED','REJECTED','CANCELLED')");
    }
}
