using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoodalLifeDbContext))]
[Migration("20260906100000_PromotePrelaunchLegalTermsV194")]
public partial class PromotePrelaunchLegalTermsV194 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DECLARE @Promoted TABLE(PublicId uniqueidentifier, Title nvarchar(300));
            INSERT INTO @Promoted VALUES
              ('19300000-0000-4000-8000-000000000001',N'수달 라이프 고객 서비스 이용약관'),
              ('19300000-0000-4000-8000-000000000002',N'수달 라이프 개인정보처리방침'),
              ('19300000-0000-4000-8000-000000000003',N'[선택] 고객 마케팅 정보 수신 동의'),
              ('19300000-0000-4000-8000-000000000004',N'수달 라이프 전문가 이용약관'),
              ('19300000-0000-4000-8000-000000000005',N'[필수] 전문가 개인정보 수집·이용 동의'),
              ('19300000-0000-4000-8000-000000000006',N'[선택] 전문가 마케팅 정보 수신 동의');

            UPDATE v SET title=p.Title,is_placeholder=0
            FROM legal_document_versions v INNER JOIN @Promoted p ON p.PublicId=v.public_id;

            UPDATE legal_document_versions SET content=REPLACE(content,
              N'제9조 개발단계\n본 문서는 정식 론칭 전 법률검토용 초안입니다. 기존 동의 이력을 덮어쓰지 않으며 운영 승인 후 시행일과 변경사항을 별도로 고지합니다.',
              N'제9조 시행 및 변경\n본 약관의 시행일은 서비스에 표시된 시행일과 같습니다. 중요한 내용이 변경되면 적용일과 변경 사유를 사전에 알리고, 별도 동의가 필요한 변경은 다시 동의를 받습니다.')
            WHERE public_id='19300000-0000-4000-8000-000000000001';
            UPDATE legal_document_versions SET content=REPLACE(content,
              N'8. 개발단계\n본 방침은 정식 론칭 전 법률검토용 초안이며 실제 수탁자·보존기간·보호책임자 정보를 최종 대조한 뒤 신규 버전으로 승인합니다.',
              N'8. 개인정보 보호 업무 및 문의\n개인정보 보호 업무 담당부서는 고객센터이며 1670-5073 또는 soodallife@dh9.kr로 문의할 수 있습니다. 처리방침의 시행일과 변경 이력은 서비스에 함께 표시합니다.')
            WHERE public_id='19300000-0000-4000-8000-000000000002';
            UPDATE legal_document_versions SET content=REPLACE(content,N' 본 문서는 정식 론칭 전 법률검토용 초안입니다.',N'')
            WHERE public_id IN ('19300000-0000-4000-8000-000000000003','19300000-0000-4000-8000-000000000005','19300000-0000-4000-8000-000000000006');
            UPDATE legal_document_versions SET content=REPLACE(content,
              N'제8조 개발단계\n본 문서는 정식 론칭 전 법률검토용 초안이며 수수료·세금·정산·보험 조건은 운영 승인 후 별도 고지합니다.',
              N'제8조 수수료·세금·정산 정책\n수수료 예약·확정·해제 시점, 세금과 정산 조건은 거래 전에 화면과 별도 정책으로 알립니다. 중요한 정책 변경은 적용 전에 고지하고 필요한 경우 별도 동의를 받습니다.')
            WHERE public_id='19300000-0000-4000-8000-000000000004';

            UPDATE d SET is_placeholder=0 FROM legal_documents d
            WHERE d.code IN ('TERMS_OF_SERVICE','PRIVACY_POLICY','MARKETING_CONSENT','PROVIDER_TERMS','PROVIDER_PRIVACY_CONSENT','PROVIDER_MARKETING_CONSENT');
            DELETE v FROM legal_document_versions v INNER JOIN legal_documents d ON d.id=v.legal_document_id
            WHERE d.code IN ('TERMS_OF_SERVICE','PRIVACY_POLICY','MARKETING_CONSENT','PROVIDER_TERMS','PROVIDER_PRIVACY_CONSENT','PROVIDER_MARKETING_CONSENT')
              AND v.is_placeholder=1 AND v.public_id NOT IN (SELECT PublicId FROM @Promoted)
              AND NOT EXISTS (SELECT 1 FROM user_consents c WHERE c.legal_document_version_id=v.id);
            IF (SELECT COUNT(*) FROM legal_document_versions WHERE public_id IN (SELECT PublicId FROM @Promoted) AND is_active=1 AND is_placeholder=0) <> 6
              THROW 51000,'V194 legal terms verification failed.',1;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE legal_document_versions SET is_placeholder=1 WHERE public_id IN (
              '19300000-0000-4000-8000-000000000001','19300000-0000-4000-8000-000000000002','19300000-0000-4000-8000-000000000003',
              '19300000-0000-4000-8000-000000000004','19300000-0000-4000-8000-000000000005','19300000-0000-4000-8000-000000000006');
            UPDATE d SET is_placeholder=1 FROM legal_documents d WHERE d.code IN (
              'TERMS_OF_SERVICE','PRIVACY_POLICY','MARKETING_CONSENT','PROVIDER_TERMS','PROVIDER_PRIVACY_CONSENT','PROVIDER_MARKETING_CONSENT');
            """);
    }
}
