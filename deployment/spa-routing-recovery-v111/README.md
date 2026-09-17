# V111 IIS SPA 경로 복구

V109/V110 정적 패키지에서 누락된 `web.config`, 서비스워커, 매니페스트, 아이콘을 고객·공급자·관리자 사이트에 복구합니다.

- `/provider`, `/company`, `/policies/*`, `/support`, `/notices`, `/faq` 직접 요청을 `index.html`로 연결합니다.
- V110의 화면 이동 시 상단 이동과 수달 케어 모바일 여백 수정도 포함합니다.
- 패키지에 `web.config`가 없으면 배포 시작 전에 중단합니다.
- API와 데이터베이스는 변경하지 않습니다.

관리자 PowerShell에서 `D:\SOODALLIFE\Deploy-SpaRoutingRecoveryV111.ps1`을 실행하세요.
