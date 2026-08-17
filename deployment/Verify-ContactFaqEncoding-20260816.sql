SET NOCOUNT ON;

SELECT public_id, title, answer_text
FROM dbo.managed_content_versions
WHERE public_id IN (
    'fa9b0026-0000-4000-8000-000000000026',
    'fa9b0028-0000-4000-8000-000000000028'
)
ORDER BY public_id;

SELECT COUNT(*) AS CorrectKoreanFaqCount
FROM dbo.managed_content_versions
WHERE (public_id = 'fa9b0026-0000-4000-8000-000000000026'
       AND answer_text = N'개인정보 보호책임자 또는 개인정보 문의 이메일 info@dh9.kr로 문의할 수 있습니다. 일반 서비스 이용 문의는 전국 대표번호 1670-5073 또는 soodallife@dh9.kr로 접수해 주세요.')
   OR (public_id = 'fa9b0028-0000-4000-8000-000000000028'
       AND answer_text = N'전국 대표번호 1670-5073 또는 이메일 soodallife@dh9.kr로 문의해 주세요. 문의할 때 요청번호나 거래번호와 함께 문제 상황을 알려주면 확인에 도움이 됩니다. 비밀번호, 주민등록번호와 금융 비밀번호는 보내지 마세요.');
