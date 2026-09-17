using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoodalLifeDbContext))]
[Migration("20260906090000_StagePrelaunchLegalDraftsV193")]
public partial class StagePrelaunchLegalDraftsV193 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DECLARE @EffectiveAt datetime2 = '2026-09-06T00:00:00Z';
            DECLARE @Drafts TABLE(Code varchar(50), PublicId uniqueidentifier, Title nvarchar(300), Content nvarchar(max));

            INSERT INTO @Drafts VALUES
            ('TERMS_OF_SERVICE','19300000-0000-4000-8000-000000000001',N'[개발·법률검토용] 수달 라이프 고객 서비스 이용약관',N'제1조 목적\n본 약관은 주식회사 디에이치가 운영하는 수달 라이프에서 고객이 생활서비스를 탐색하고 요청·견적 비교·전문가 선택·거래관리를 이용하는 조건을 정합니다.\n\n제2조 플랫폼의 지위\n회사는 고객과 전문가를 연결하는 통신판매중개자이며 개별 서비스 계약의 당사자는 고객과 선택된 전문가입니다. 다만 관계 법령상 회사의 책임을 배제하지 않습니다.\n\n제3조 회원과 계정\n회원은 만 14세 이상이어야 하고 정확한 정보를 제공해야 합니다. 계정 양도, 타인 사칭, 허위 요청, 시스템 침해와 부정 이용을 금지합니다.\n\n제4조 요청·견적·계약\n고객은 서비스 범위·수량·일정·지역 등 필요한 정보를 정확히 입력합니다. 화면의 참고가격은 확정가격이 아니며 실제 견적에는 작업범위, 자재, 출장비, VAT 포함 여부와 최종금액을 표시해야 합니다. 고객이 견적을 채택하면 고객과 전문가 사이에 거래가 성립합니다.\n\n제5조 개인정보 공개\n선택 전에는 시·군·구 등 최소 지역정보만 사용하며 연락처와 상세주소는 업무에 선택·배정된 전문가에게 필요한 기간에만 공개합니다. 온라인·비대면 서비스에는 현장주소를 요구하지 않습니다.\n\n제6조 대금·취소·환불\n정식 PG·에스크로 도입 전 서비스 대금은 고객이 전문가에게 직접 지급합니다. 취소·환불은 관계 법령, 공개 정책, 진행단계, 귀책사유와 입증된 실제비용을 기준으로 처리합니다.\n\n제7조 안전·게시물·파일\n회원은 주민등록번호, 금융정보, 현관 비밀번호 등 민감정보를 게시하지 않아야 합니다. 회사는 개인정보 또는 악성파일 위험이 있는 첨부를 비공개·삭제할 수 있습니다.\n\n제8조 이용제한·분쟁\n허위정보, 반복 노쇼, 외부거래 강요, 개인정보 오남용, 차별·괴롭힘과 불법행위가 확인되면 신규 매칭 제한, 이용정지 또는 신고 조치를 할 수 있습니다. 분쟁은 당사자 협의를 우선하며 관계기관 절차와 대한민국 법령을 따릅니다.\n\n제9조 개발단계\n본 문서는 정식 론칭 전 법률검토용 초안입니다. 기존 동의 이력을 덮어쓰지 않으며 운영 승인 후 시행일과 변경사항을 별도로 고지합니다.'),
            ('PRIVACY_POLICY','19300000-0000-4000-8000-000000000002',N'[개발·법률검토용] 개인정보처리방침',N'1. 처리 목적\n회사는 회원가입·로그인, 본인확인, 주소관리, 요청·견적·거래·구독·인테리어·긴급출동·A/S·분쟁, 채팅·파일, 수수료와 보안관리를 위해 필요한 개인정보를 처리합니다.\n\n2. 처리 항목\n계정정보, 이름, 휴대전화, 이메일, 서비스 지역과 선택 후 상세주소, 요청·견적·거래기록, 채팅·첨부파일, 접속기록 및 동의이력을 처리할 수 있습니다. 주민등록번호와 민감정보는 법적 근거 없이 요구하지 않습니다.\n\n3. 제3자 제공과 공개시점\n연락처와 상세주소는 선택·배정된 전문가에게 서비스 수행에 필요한 범위와 기간에만 제공합니다. 선택 전, 업무 종료 또는 전문가 교체 후에는 신규 조회를 제한합니다.\n\n4. 처리위탁\n서버·저장·백업 등 실제 수탁자, 위탁업무, 보유기간은 계약 내용과 대조하여 운영 전 공개합니다. 계약·승인되지 않은 외부 연동은 성공으로 처리하지 않습니다.\n\n5. 보유와 파기\n목적 달성 시 지체 없이 파기하되 전자상거래 등 관계 법령상 보존 의무가 있는 기록은 해당 기간 동안 분리 보관합니다.\n\n6. 안전조치\n접근권한 최소화, 암호화, 접속기록, 파일 안전검사, 위치정보와 메타데이터 제거 등 보호조치를 적용합니다.\n\n7. 정보주체 권리\n회원은 열람·정정·삭제·처리정지와 선택 동의 철회를 요청할 수 있습니다. 고객센터 1670-5073 또는 soodallife@dh9.kr로 문의할 수 있습니다.\n\n8. 개발단계\n본 방침은 정식 론칭 전 법률검토용 초안이며 실제 수탁자·보존기간·보호책임자 정보를 최종 대조한 뒤 신규 버전으로 승인합니다.'),
            ('MARKETING_CONSENT','19300000-0000-4000-8000-000000000003',N'[선택·개발·법률검토용] 고객 마케팅 정보 수신 동의',N'회사는 신규 서비스, 혜택, 행사와 프로모션을 안내하기 위해 회원이 선택한 WEB·문자·이메일·PUSH 채널로 마케팅 정보를 발송할 수 있습니다. 동의 항목은 연락처, 이메일, 서비스 관심정보이며 이용 목적은 맞춤형 혜택 안내입니다. 동의하지 않아도 기본 서비스 이용에는 제한이 없고 언제든 마이수달에서 철회할 수 있습니다. 야간 광고성 정보는 별도 동의가 있는 경우에만 발송합니다. 실제 외부 발송은 채널 계약과 운영 승인이 완료된 뒤 시작합니다. 본 문서는 정식 론칭 전 법률검토용 초안입니다.'),
            ('PROVIDER_TERMS','19300000-0000-4000-8000-000000000004',N'[개발·법률검토용] 수달 라이프 전문가 이용약관',N'제1조 목적\n본 약관은 전문가가 수달 라이프를 통해 고객에게 서비스를 제안·견적·수행할 때 회사와 전문가의 권리·의무를 정합니다.\n\n제2조 등록과 승인\n전문가는 본인·사업자·자격·보험·활동지역 정보를 사실대로 제출하고 승인된 서비스 범위에서만 활동합니다. 정보가 변경되면 즉시 갱신해야 합니다.\n\n제3조 견적과 VAT\n견적에는 작업범위, 제외사항, 자재·출장비, 공급가액, VAT 포함·별도·면세 여부와 고객 최종금액을 명확히 표시합니다. 합의 없는 추가비용 청구를 금지합니다.\n\n제4조 개인정보\n고객 연락처와 상세주소는 선택·배정 후 필요한 기간에만 이용하며 광고, 명부작성, 제3자 제공 또는 거래 외 연락에 사용하지 않습니다. 업무 종료 후 법적 의무가 없는 별도 저장정보는 삭제합니다.\n\n제5조 수수료·충전금\n플랫폼 수수료와 예약금은 관리자 정책값과 화면의 사전 안내를 따릅니다. 예약·확정·복원·환불은 원장으로 기록하며 잔액 부족 시 유료 기능을 등록할 수 없습니다.\n\n제6조 서비스 수행\n확정 일정, 안전수칙, 자격·보험 의무, A/S·취소·환불 조건을 준수하고 완료 증빙과 고객 확인을 사실대로 기록합니다.\n\n제7조 금지행위와 제한\n허위 증빙, 무단 재위탁, 플랫폼 외 거래 강요, 개인정보 오남용, 부당한 가격변경, 차별·괴롭힘과 시스템 침해를 금지합니다. 위반 시 매칭 제한·정지·계약해지·관계기관 신고가 가능합니다.\n\n제8조 개발단계\n본 문서는 정식 론칭 전 법률검토용 초안이며 수수료·세금·정산·보험 조건은 운영 승인 후 별도 고지합니다.'),
            ('PROVIDER_PRIVACY_CONSENT','19300000-0000-4000-8000-000000000005',N'[필수·개발·법률검토용] 전문가 개인정보 수집·이용 동의',N'회사는 전문가 등록·본인확인·사업자 또는 개인 심사·서비스 승인·매칭·견적·거래·수수료·분쟁과 법적 의무 이행을 위해 이름, 연락처, 이메일, 표시명, 담당자명, 사업자등록정보, 활동지역, 자격·보험 및 제출 증빙, 서비스 수행기록을 처리합니다. 증빙은 권한 있는 심사 담당자만 접근하며 주민등록번호 뒷자리 등 불필요한 정보는 가린 뒤 제출해야 합니다. 관계 법령상 보존의무가 없으면 목적 달성 후 파기합니다. 필수 처리에 동의하지 않으면 전문가 등록과 활동이 불가능합니다. 본 문서는 정식 론칭 전 법률검토용 초안입니다.'),
            ('PROVIDER_MARKETING_CONSENT','19300000-0000-4000-8000-000000000006',N'[선택·개발·법률검토용] 전문가 마케팅 정보 수신 동의',N'회사는 신규 서비스, 교육, 전문가 혜택과 프로모션을 안내하기 위해 동의한 WEB·문자·이메일·PUSH 채널을 사용할 수 있습니다. 동의하지 않아도 전문가 등록과 기본 업무에는 제한이 없으며 언제든 설정에서 철회할 수 있습니다. 야간 광고성 정보는 별도 동의가 있는 경우에만 발송하고 계약·승인되지 않은 외부 채널은 발송 성공으로 처리하지 않습니다. 본 문서는 정식 론칭 전 법률검토용 초안입니다.');

            UPDATE v SET is_active=0,effective_to=@EffectiveAt
            FROM legal_document_versions v INNER JOIN legal_documents d ON d.id=v.legal_document_id
            WHERE d.code IN (SELECT Code FROM @Drafts) AND v.is_active=1 AND v.public_id NOT IN (SELECT PublicId FROM @Drafts);

            UPDATE d SET is_placeholder=1 FROM legal_documents d WHERE d.code IN (SELECT Code FROM @Drafts);

            INSERT INTO legal_document_versions(public_id,legal_document_id,version_no,title,content,effective_from,effective_to,is_active,is_placeholder)
            SELECT x.PublicId,d.id,ISNULL((SELECT MAX(v.version_no) FROM legal_document_versions v WHERE v.legal_document_id=d.id),0)+1,x.Title,x.Content,@EffectiveAt,NULL,1,1
            FROM @Drafts x INNER JOIN legal_documents d ON d.code=x.Code
            WHERE NOT EXISTS(SELECT 1 FROM legal_document_versions v WHERE v.public_id=x.PublicId);

            IF (SELECT COUNT(*) FROM legal_document_versions WHERE public_id IN (SELECT PublicId FROM @Drafts) AND is_active=1 AND is_placeholder=1) <> 6
                THROW 51000,'V193 legal draft verification failed.',1;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM legal_document_versions WHERE public_id IN (
              '19300000-0000-4000-8000-000000000001','19300000-0000-4000-8000-000000000002','19300000-0000-4000-8000-000000000003',
              '19300000-0000-4000-8000-000000000004','19300000-0000-4000-8000-000000000005','19300000-0000-4000-8000-000000000006');
            UPDATE d SET is_placeholder=CASE WHEN d.audience_code='CUSTOMER' THEN 1 ELSE 0 END
            FROM legal_documents d WHERE d.code IN ('TERMS_OF_SERVICE','PRIVACY_POLICY','MARKETING_CONSENT','PROVIDER_TERMS','PROVIDER_PRIVACY_CONSENT','PROVIDER_MARKETING_CONSENT');
            WITH latest AS (SELECT legal_document_id,MAX(version_no) version_no FROM legal_document_versions GROUP BY legal_document_id)
            UPDATE v SET is_active=1,effective_to=NULL FROM legal_document_versions v INNER JOIN latest x ON x.legal_document_id=v.legal_document_id AND x.version_no=v.version_no;
            """);
    }
}
