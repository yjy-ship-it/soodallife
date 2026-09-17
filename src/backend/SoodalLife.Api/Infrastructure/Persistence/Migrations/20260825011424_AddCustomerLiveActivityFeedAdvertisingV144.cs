using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    public partial class AddCustomerLiveActivityFeedAdvertisingV144 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "advertising_placements",
                columns: new[] { "id", "code", "created_at", "created_by_user_id", "description", "is_active", "name", "public_id", "route_hint", "updated_at", "updated_by_user_id" },
                values: new object[] { 3L, "CUSTOMER_LIVE_ACTIVITY_FEED", new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc), null, "전국에서 지금 진행 중인 서비스 목록 사이에 광고임을 명확히 표시하여 노출", true, "고객 실시간 서비스 목록", new Guid("11a10000-0000-0000-0000-000000000003"), "/customer#live-activity", new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.InsertData(
                table: "provider_advertising_rate_policies",
                columns: new[] { "id", "created_at", "created_by_user_id", "currency_code", "district_unit_amount", "duration_days", "effective_from", "effective_to", "fixed_amount", "is_active", "placement_id", "province_unit_amount", "public_id", "regional_fee_cap_amount", "updated_at", "updated_by_user_id" },
                values: new object[,]
                {
                    { 7L, new DateTime(2026, 8, 19, 0, 0, 0, DateTimeKind.Utc), null, "KRW", 2000m, 7, new DateOnly(2026, 8, 19), null, 28000m, true, 3L, 10000m, new Guid("11a20000-0000-0000-0000-000000000007"), 30000m, new DateTime(2026, 8, 19, 0, 0, 0, DateTimeKind.Utc), null },
                    { 8L, new DateTime(2026, 8, 19, 0, 0, 0, DateTimeKind.Utc), null, "KRW", 2000m, 14, new DateOnly(2026, 8, 19), null, 48000m, true, 3L, 10000m, new Guid("11a20000-0000-0000-0000-000000000008"), 30000m, new DateTime(2026, 8, 19, 0, 0, 0, DateTimeKind.Utc), null },
                    { 9L, new DateTime(2026, 8, 19, 0, 0, 0, DateTimeKind.Utc), null, "KRW", 2000m, 30, new DateOnly(2026, 8, 19), null, 88000m, true, 3L, 10000m, new Guid("11a20000-0000-0000-0000-000000000009"), 30000m, new DateTime(2026, 8, 19, 0, 0, 0, DateTimeKind.Utc), null }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "provider_advertising_rate_policies", keyColumn: "id", keyValue: 7L);
            migrationBuilder.DeleteData(table: "provider_advertising_rate_policies", keyColumn: "id", keyValue: 8L);
            migrationBuilder.DeleteData(table: "provider_advertising_rate_policies", keyColumn: "id", keyValue: 9L);
            migrationBuilder.DeleteData(table: "advertising_placements", keyColumn: "id", keyValue: 3L);
        }
    }
}
