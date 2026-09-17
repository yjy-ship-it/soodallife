SOODAL LIFE V178 - 긴급출동 카테고리 정책 분리
배포일: 2026-08-31

수정 내용
- 고객 긴급출동 대·중·소 분류를 현재 출동 가능한 전문가 수가 아니라 본사 카테고리 정책으로 표시합니다.
- 전문가가 긴급출동을 잠시 OFF해도 본사 허용 카테고리는 고객 화면에서 사라지지 않습니다.
- 전문가의 서비스 등록, 활동지역, 긴급출동 ON/OFF, 운영시간은 실제 요청 매칭 시점에만 확인합니다.
- 본사에서 긴급출동을 허용하지 않은 서비스는 고객 요청 생성·수정·공개와 전문가 매칭에서 차단합니다.
- 전문가 긴급출동 설정 화면에는 본사에서 허용한 등록 서비스만 표시합니다.
- 현재 가능한 전문가가 없더라도 요청은 정상 등록되고, 가능한 전문가가 확인될 때 전달된다는 안내를 표시합니다.

배포 영향
- API와 고객·전문가·본사 정적 화면을 교체합니다.
- DB 스키마와 데이터는 변경하지 않습니다.
- 운영 appsettings와 API App_Data는 보존합니다.
- 배포 전 IIS 구성과 기존 파일을 자동 백업하며 파일 배포 실패 시 복구합니다.

D:\SOODALLIFE에 아래 두 파일을 함께 둔 뒤 관리자 PowerShell에서 실행합니다.
- Deploy-EmergencyCategoryPolicyV178.ps1
- SoodalLife-EmergencyCategoryPolicy-20260831-v178.zip

실행 명령
Set-Location 'D:\SOODALLIFE'
.\Deploy-EmergencyCategoryPolicyV178.ps1 -ConfirmProductionDeployment
