using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoodalLifeDbContext))]
[Migration("20260811090000_ProtectAuditLogsAppendOnly")]
public partial class ProtectAuditLogsAppendOnly : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            EXEC(N'CREATE TRIGGER [TR_audit_logs_append_only]
            ON [audit_logs]
            INSTEAD OF UPDATE, DELETE
            AS
            BEGIN
                SET NOCOUNT ON;
                THROW 51000, N''감사로그는 수정하거나 삭제할 수 없습니다.'', 1;
            END;')
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS [TR_audit_logs_append_only];");
    }
}
