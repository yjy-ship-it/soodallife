using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoodalLife.Api.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SoodalLifeDbContext))]
[Migration("20260815153000_UpdateCustomerServiceContact")]
public partial class UpdateCustomerServiceContact : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE managed_content_versions
            SET answer_text = N'개인정보 보호책임자 또는 개인정보 문의 이메일 info@dh9.kr로 문의할 수 있습니다. 일반 서비스 이용 문의는 전국 대표번호 1670-5073 또는 soodallife@dh9.kr로 접수해 주세요.'
            WHERE public_id = 'fa9b0026-0000-4000-8000-000000000026';

            UPDATE managed_content_versions
            SET answer_text = N'전국 대표번호 1670-5073 또는 이메일 soodallife@dh9.kr로 문의해 주세요. 문의할 때 요청번호나 거래번호와 함께 문제 상황을 알려주면 확인에 도움이 됩니다. 비밀번호, 주민등록번호와 금융 비밀번호는 보내지 마세요.'
            WHERE public_id = 'fa9b0028-0000-4000-8000-000000000028';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE managed_content_versions
            SET answer_text = N'개인정보 보호책임자 또는 개인정보 문의 이메일 info@dh9.kr로 문의할 수 있습니다. 일반 서비스 이용 문의는 전국 대표번호 1670-5073 또는 soollife@dh9.kr로 접수해 주세요.'
            WHERE public_id = 'fa9b0026-0000-4000-8000-000000000026';

            UPDATE managed_content_versions
            SET answer_text = N'전국 대표번호 1670-5073 또는 이메일 soollife@dh9.kr로 문의해 주세요. 문의할 때 요청번호나 거래번호와 함께 문제 상황을 알려주면 확인에 도움이 됩니다. 비밀번호, 주민등록번호와 금융 비밀번호는 보내지 마세요.'
            WHERE public_id = 'fa9b0028-0000-4000-8000-000000000028';
            """);
    }
}
