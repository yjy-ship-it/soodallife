수달 라이프 V243 전문가 업무·카테고리 보완 누적 배포본

1. D:\SOODALLIFE\Deploy-ProviderWorkflowV243.ps1을 Windows PowerShell 5.1 관리자 권한으로 실행합니다.
2. 실행 예:
   powershell.exe -ExecutionPolicy Bypass -File D:\SOODALLIFE\Deploy-ProviderWorkflowV243.ps1 -ConfirmProductionDeployment
3. 스크립트가 ZIP SHA-256, 필수 파일, UTF-8 BOM, PowerShell 구문을 먼저 검사합니다.
4. SQL Server와 IIS 백업이 성공한 뒤에만 누적 SQL, V243 데이터 보정, API·세 사이트 파일을 적용합니다.
5. appsettings*.json과 API App_Data는 배포 묶음에 포함하지 않으며 운영 값을 보존합니다.

주요 반영: 일정 로그인 만료 복구, 견적 목록 분리·한글 상태·기본폼 UX·숫자 입력, 반복 일정 단순화, 공동모집 정원 검증·50% 공개 기준, 이미지 전용 요청 첨부, 신고 모바일 정렬, 프로필 이미지 대체, 광고 분야 설명, 긴급출동 냉난방/수리 정책, 경기도 수원시 구 단위 행정구역.
V243 district expansion: 35 attached districts across Gyeonggi, Chungbuk, Chungnam, Jeonbuk, Gyeongbuk and Gyeongnam. Together with the 4 Suwon districts from V242, 39 flattened district choices are available.

V243-R2: corrected the final API marker verification phrase; database and district contents are unchanged.

V244: 13 parent city choices are deactivated while all 39 city-district choices remain active. Historical foreign-key references are preserved.

V244-R2: physically deletes the 13 parent city rows only when no foreign-key reference exists. All 39 district choices are verified after deletion.
