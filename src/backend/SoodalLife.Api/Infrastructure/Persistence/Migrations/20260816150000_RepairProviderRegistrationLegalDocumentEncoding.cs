using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoodalLifeDbContext))]
[Migration("20260816150000_RepairProviderRegistrationLegalDocumentEncoding")]
public partial class RepairProviderRegistrationLegalDocumentEncoding : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE v SET
                v.title = s.title,
                v.content = s.content,
                v.is_active = 1,
                v.is_placeholder = 0
            FROM legal_document_versions v
            INNER JOIN legal_documents d ON d.id = v.legal_document_id
            INNER JOIN (VALUES
                ('PROVIDER_TERMS', N'수달 라이프 공급자 이용약관', N'수달 라이프는 고객과 공급자를 연결하는 통신판매중개 플랫폼입니다. 공급자는 등록정보와 증빙을 사실대로 제출하고, 승인된 서비스와 활동지역에서 관계 법령 및 플랫폼 운영정책을 준수하여 업무를 수행해야 합니다. 서비스 계약과 대금 지급은 고객과 공급자 사이에서 이루어지며, 공급자는 견적·추가비용·취소·환불 조건을 사전에 명확히 안내해야 합니다. 고객이 공급자를 선택하기 전에는 전화번호와 상세주소 등 보호정보에 접근할 수 없습니다. 허위정보, 미방문, 플랫폼 외 거래 유도, 개인정보 오남용 및 부당한 가격변경은 제한·정지·계약해지 사유가 될 수 있습니다. 공급자 수수료는 고객의 최종 선택 또는 정책에서 정한 확정 시점에 원장으로 기록되며, 취소·무효 사유가 인정되면 별도의 복원 원장으로 처리합니다. 인테리어 FEE-I1은 최종 시공업체 선택 시 확정된 구간 수수료 정책을 적용합니다. 본 약관은 2026년 8월 16일부터 적용합니다.'),
                ('PROVIDER_PRIVACY_CONSENT', N'공급자 개인정보 수집·이용 동의', N'회사는 공급자 등록, 본인확인, 사업자 또는 개인 공급자 심사, 서비스 매칭, 분쟁처리 및 법적 의무 이행을 위해 이름, 휴대전화번호, 이메일, 공급자 표시명, 담당자명, 사업자등록정보, 활동지역, 제출 증빙과 서비스 수행기록을 처리합니다. 사업자등록증과 신분증은 권한 있는 심사 담당자만 접근할 수 있는 비공개 증빙으로 관리합니다. 개인 공급자의 신분증에서 주민등록번호 뒷자리 등 불필요한 정보는 수집하지 않으며, 사용자는 해당 부분을 가린 뒤 제출해야 합니다. 보유기간은 관계 법령 및 개인정보처리방침에 따르며 목적 달성 후 안전하게 파기합니다. 필수정보 처리에 동의하지 않으면 공급자 등록과 심사를 진행할 수 없습니다.'),
                ('PROVIDER_MARKETING_CONSENT', N'공급자 마케팅 정보 수신 동의', N'회사는 신규 서비스, 교육, 프로모션 및 공급자 혜택 안내를 위해 동의한 채널로 정보를 발송할 수 있습니다. 동의하지 않아도 공급자 등록과 기본 서비스 이용에는 제한이 없으며 언제든지 알림 설정에서 철회할 수 있습니다. 카카오 알림톡, SMS, 이메일 및 Push가 연동되지 않은 상태에서는 발송 성공으로 처리하지 않습니다.')
            ) s(code, title, content) ON s.code = d.code
            WHERE v.is_active = 1;

            IF @@ROWCOUNT <> 3
                THROW 51000, 'Provider legal document encoding repair verification failed.', 1;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Encoding repair is intentionally non-destructive on rollback.
    }
}
