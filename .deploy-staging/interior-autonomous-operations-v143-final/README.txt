Soodal Life V143 - Interior Autonomous Operations

운영 구조
- 고객과 공급자가 실측 제안, 공급자 선택, 계약자료 등록·확인, 공정 진행, 변경 합의와 완료 확인을 직접 처리합니다.
- 공급자는 제출한 유효 견적을 기준으로 고객에게 실측 일정·실측비·조건을 직접 제안합니다.
- 고객은 후보별 실측비와 조건을 확인한 뒤 하나의 실측 일정을 선택합니다.
- 공급자는 복잡한 공사만 공정을 등록합니다. 간단한 공사는 공정 등록 없이 양 당사자 완료 확인으로 종료할 수 있습니다.
- 별도 검사자가 없는 공사는 공급자 자체 점검과 고객 완료 확인을 최종 확인으로 인정합니다.
- 본사 관리자는 정상 거래를 생성·수정·승인하지 않으며, 최소 개인정보로 현황을 조회하고 별도의 분쟁·제재 절차만 담당합니다.

개인정보와 기존 정책
- 고객이 실측 공급자 또는 최종 공급자를 선택하기 전에는 전화번호와 상세주소를 공개하지 않습니다.
- 관리자 인테리어 화면에서는 고객 전화번호와 상세주소를 표시하지 않습니다.
- 견적 채택 수수료 예약·차감, 계약 확인 7일 기한, 보완·분쟁 중 만료 중지는 기존 정책을 유지합니다.
- 데이터베이스 스키마 변경은 없습니다.

검증
- Backend build: passed (warning 1, error 0)
- Provider interior regression tests: 5 passed
- Frontend TypeScript and production build: passed

배포
1. ZIP과 PowerShell 파일을 D:\SOODALLIFE에 복사합니다.
2. 관리자 권한 PowerShell에서 다음 명령을 실행합니다.
   D:\SOODALLIFE\Deploy-InteriorAutonomousOperationsV143.ps1 -ConfirmProductionDeployment
3. API health/readiness와 고객·공급자·관리자 인테리어 화면을 확인합니다.

복원
- 배포 전에 IIS 구성 백업과 전체 앱 파일 백업을 자동 생성합니다.
- 실패하면 배포 전 파일로 자동 복원합니다.
- API App_Data와 운영 appsettings 파일은 보존합니다.
