using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>Adds public provider advertising consultation support.</summary>
    public partial class AddProviderAdvertisingConsultationV120 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(name: "CK_chat_rooms_resource_type", table: "chat_rooms");
            migrationBuilder.AddCheckConstraint(
                name: "CK_chat_rooms_resource_type",
                table: "chat_rooms",
                sql: "[resource_type] IN ('TRANSACTION','SUBSCRIPTION','INTERIOR','AFTER_SERVICE','PROVIDER_CONSULTATION')");

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'dbo.notification_templates', N'U') IS NOT NULL
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM dbo.notification_templates WHERE template_code = 'PROVIDER_CONSULTATION_REQUESTED' AND channel_code = 'WEB')
                    BEGIN
                        INSERT INTO dbo.notification_templates
                        (public_id, template_code, name, description, audience_type_code, event_type_code, channel_code,
                         title_template, body_template, allowed_variables_json, is_required_business_notice, is_marketing,
                         is_active, effective_from, effective_to, created_at, created_by_user_id, updated_at, updated_by_user_id)
                        VALUES
                        (NEWID(), 'PROVIDER_CONSULTATION_REQUESTED', N'공급자 광고 상담 신청',
                         N'고객이 공급자 소개 화면에서 채팅 상담을 신청했을 때 공급자에게 안내합니다.',
                         'PROVIDER', 'CUSTOMER_CONSULTATION_REQUESTED', 'WEB', N'새 채팅 상담 신청',
                         N'{{customer_name}} 고객이 상담 신청하였습니다. 수달 라이프 채팅에서 확인해 주세요.',
                         N'["customer_name"]', 1, 0, 1, SYSUTCDATETIME(), NULL, SYSUTCDATETIME(), NULL, SYSUTCDATETIME(), NULL);
                    END;

                    IF NOT EXISTS (SELECT 1 FROM dbo.notification_templates WHERE template_code = 'PROVIDER_CONSULTATION_REQUESTED' AND channel_code = 'KAKAO')
                    BEGIN
                        INSERT INTO dbo.notification_templates
                        (public_id, template_code, name, description, audience_type_code, event_type_code, channel_code,
                         title_template, body_template, allowed_variables_json, is_required_business_notice, is_marketing,
                         is_active, effective_from, effective_to, created_at, created_by_user_id, updated_at, updated_by_user_id)
                        VALUES
                        (NEWID(), 'PROVIDER_CONSULTATION_REQUESTED', N'공급자 광고 상담 신청',
                         N'알림톡 계약 및 채널 설정이 활성화되면 고객 상담 신청을 공급자에게 전송합니다.',
                         'PROVIDER', 'CUSTOMER_CONSULTATION_REQUESTED', 'KAKAO', N'새 채팅 상담 신청',
                         N'{{customer_name}} 고객이 상담 신청하였습니다. 수달 라이프 채팅에서 확인해 주세요.',
                         N'["customer_name"]', 1, 0, 1, SYSUTCDATETIME(), NULL, SYSUTCDATETIME(), NULL, SYSUTCDATETIME(), NULL);
                    END;
                END;
                """);

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'dbo.notification_templates', N'U') IS NOT NULL
                BEGIN
                    DELETE FROM dbo.notification_templates
                    WHERE template_code = 'PROVIDER_CONSULTATION_REQUESTED' AND channel_code IN ('WEB', 'KAKAO');
                END;
                """);
            migrationBuilder.DropCheckConstraint(name: "CK_chat_rooms_resource_type", table: "chat_rooms");
            migrationBuilder.AddCheckConstraint(
                name: "CK_chat_rooms_resource_type",
                table: "chat_rooms",
                sql: "[resource_type] IN ('TRANSACTION','SUBSCRIPTION','INTERIOR','AFTER_SERVICE')");
        }
    }
}
