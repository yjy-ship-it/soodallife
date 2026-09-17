V241 모바일·맞춤 케어·공동모집 개선 배포

1. 관리자 권한 Windows PowerShell 5.1을 실행합니다.
2. D:\SOODALLIFE\Deploy-MobileCareProposalV241.ps1의 ZIP 해시와 대상 경로를 확인합니다.
3. 다음 명령으로 배포합니다.
   powershell -ExecutionPolicy Bypass -File D:\SOODALLIFE\Deploy-MobileCareProposalV241.ps1 -ConfirmProductionDeployment

배포 전 데이터베이스·IIS·앱 파일을 백업합니다. API의 App_Data와 appsettings 파일은 보존됩니다.
V240 전문가 견적 기본폼 기능도 포함한 누적 배포본입니다.
