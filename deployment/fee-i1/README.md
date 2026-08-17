# FEE-I1 운영 배포 절차

대상: `SOODAL_LIFE_DEV` 운영 연결 DB와 IIS `SoodalLife.Api` 앱 풀. 이 배포는 외부 연동 상태와 운영 Secret을 변경하지 않는다.

## 1. 배포 전 확인과 백업

1. 현재 API `/api/v1/system/health`, `/api/v1/system/ready`가 200인지 확인한다.
2. IIS 설정 백업을 만든다.
3. SQL Server에서 `SOODAL_LIFE_DEV`를 `COPY_ONLY`, `CHECKSUM`, `COMPRESSION` 옵션으로 백업하고 `RESTORE VERIFYONLY ... WITH CHECKSUM`으로 검증한다.
4. 현재 `D:\SOODALLIFE\apps\api` 및 세 프론트엔드 폴더를 날짜가 포함된 별도 백업 폴더에 복사한다.

## 2. DB 마이그레이션

`FEE-I1_ImplementInteriorFinalSelectionFee.sql`은 EF Migration History를 확인하는 멱등성 스크립트다. DB 초기화·삭제를 하지 않는다.

```powershell
sqlcmd -S localhost -E -C -d SOODAL_LIFE_DEV -b `
  -i D:\SOODALLIFE\releases\fee-i1-20260814\deployment\FEE-I1_ImplementInteriorFinalSelectionFee.sql
```

마이그레이션은 기존 `POLICY_PENDING` 프로젝트 중 아직 최종 공급자가 선택되지 않은 건을 `PENDING_SELECTION`으로 바꾼다. 이미 선택된 과거 프로젝트는 `NOT_APPLICABLE`로 전환하며 수수료를 소급 차감하지 않는다.

## 3. IIS 파일 교체

1. `SoodalLife.Api` 앱 풀을 중지한다.
2. 패키지의 `api` 파일을 `D:\SOODALLIFE\apps\api`에 복사한다.
3. 고객·공급자·관리자 정적 파일을 각 IIS 실제 경로에 복사한다.
4. IIS 앱 풀 환경변수, ConnectionString, Data Protection Key 경로, 개인정보 SearchHashKey, Private File 경로는 기존 값을 유지한다.
5. `SoodalLife.Api` 앱 풀을 시작한다.

## 4. 배포 후 검증

- API health/readiness: 각각 HTTP 200 및 `ok`/`ready`
- 고객·공급자·관리자 HTTPS 화면: HTTP 200, 인증서 경고 없음
- CORS: 고객·공급자·관리자 Origin의 사전 요청은 204와 해당 Origin을 반환
- 관리자 로그인과 대시보드: 정상
- 인테리어 최종 공급자 선택: 유효한 최신 상세견적만 선택 가능
- 수수료: 선택 견적금액의 FEE-Q1~FEE-Q7 구간 금액이 한 번만 차감
- 원장: Transaction, FeeCharge, WalletLedgerEntry(USE)가 각 한 건이고 프로젝트 상태는 `ASSESSED`
- 동일 IdempotencyKey 재요청: 추가 차감 없음
- 지갑 잔액 부족: 선택·거래·수수료 원장이 모두 생성되지 않고 기존 상태 유지
- 외부 연동: 기존 `NOT_INTEGRATED` 또는 Fail Closed 상태 유지

## 5. 주의

운영 계정으로 실제 수수료 차감 smoke test를 하지 않는다. 승인된 테스트 공급자 지갑과 테스트 요청을 사용하고, 테스트 결과를 원장과 함께 확인한다. 문제가 생기면 앱 풀을 중지하고 API 파일 백업을 복원한 뒤 DB 복구 필요성을 별도로 판단한다. 새 수수료 원장이 생성된 뒤에는 단순 Down Migration으로 금융 이력을 되돌리지 않는다.
