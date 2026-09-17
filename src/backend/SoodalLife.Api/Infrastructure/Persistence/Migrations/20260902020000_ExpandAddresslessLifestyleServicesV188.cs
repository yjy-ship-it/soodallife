using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SoodalLife.Api.Infrastructure.Persistence;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoodalLifeDbContext))]
[Migration("20260902020000_ExpandAddresslessLifestyleServicesV188")]
public sealed class ExpandAddresslessLifestyleServicesV188 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DECLARE @AddresslessServices TABLE (service_name nvarchar(200) NOT NULL PRIMARY KEY);
            INSERT INTO @AddresslessServices (service_name) VALUES
                (N'홈페이지 제작'),(N'쇼핑몰 제작'),(N'모바일 웹'),(N'모바일 앱'),(N'웹앱'),
                (N'업무시스템'),(N'MES·ERP·CRM 개발'),(N'상세페이지'),(N'UI·UX'),(N'로고'),
                (N'포장디자인'),(N'디자인'),(N'홍보영상'),(N'숏폼'),(N'카드뉴스'),
                (N'AI 콘텐츠 제작'),(N'문서 번역'),(N'영상 자막'),(N'기술번역'),(N'검색광고'),
                (N'SNS 마케팅'),(N'블로그 마케팅'),(N'블로그 원고'),(N'PPT 제작'),(N'문서작성'),
                (N'엑셀 작업'),(N'데이터 입력'),(N'호스팅 관리'),(N'브랜드 컨설팅'),
                (N'사업자등록'),(N'기업인증'),(N'계약서 검토'),(N'취업규칙'),(N'급여관리'),
                (N'기장'),(N'노무상담'),(N'법률상담'),(N'법인설립'),(N'분쟁지원'),
                (N'세무상담'),(N'세무신고'),(N'인허가'),(N'정부지원사업'),(N'채용대행'),
                (N'전문인력 매칭'),(N'특허·상표');

            UPDATE policy
            SET coverage_type_code = 'NATIONWIDE_REMOTE', updated_at = SYSUTCDATETIME()
            FROM category_operation_policies policy
            INNER JOIN service_categories service ON service.id = policy.category_id AND service.level_code = 'SERVICE'
            INNER JOIN service_categories middle ON middle.id = service.parent_id AND middle.level_code = 'MIDDLE'
            INNER JOIN service_categories major ON major.id = middle.parent_id AND major.level_code = 'MAJOR'
            INNER JOIN @AddresslessServices target ON target.service_name = service.name
            WHERE major.name = N'생활서비스' AND policy.is_active = 1;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // The previous per-service value cannot be reconstructed safely after later policy edits.
    }
}
