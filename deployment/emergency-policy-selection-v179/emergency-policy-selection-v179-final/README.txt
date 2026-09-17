SOODAL LIFE V179 - 긴급출동 정책 선택 오류 수정
배포일: 2026-08-31

수정 내용
- 긴급출동 목록에는 보이지만 저장 시 허용되지 않았다는 오류가 발생하는 정책 선택 불일치를 수정합니다.
- 긴급 요청 생성·수정·공개 단계에서 현재 유효한 긴급출동 허용 정책만 선택합니다.
- 같은 서비스에 유효한 정책 이력이 여러 개 있어도 긴급출동 허용 정책을 일관되게 선택합니다.
- 공개 서비스 상세의 긴급출동 허용 여부를 목록 및 저장 API와 같은 조건으로 계산합니다.

배포 영향
- API만 교체합니다.
- 고객·전문가·본사 화면 파일은 변경하지 않습니다.
- DB 스키마와 데이터는 변경하지 않습니다.
- 운영 appsettings와 API App_Data는 보존합니다.
- 배포 전 IIS 구성과 기존 API 파일을 자동 백업합니다.

D:\SOODALLIFE에서 관리자 PowerShell로 실행합니다.

Set-Location 'D:\SOODALLIFE'
.\Deploy-EmergencyPolicySelectionV179.ps1 -ConfirmProductionDeployment
