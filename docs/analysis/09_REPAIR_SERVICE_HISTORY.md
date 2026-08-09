# 수리·서비스 이력 연결 설계

## 1. 원칙

이력은 새 거래가 아니다. 기존 요청·채택 QuoteRevision·Transaction·WorkCompletionRevision·CustomerConfirmation을 `transactionId` 중심으로 연결한 append-only 사건 계층이다.

정상완료 한 건에 고객용/Provider용 사실을 따로 복제하지 않는다. 하나의 `ServiceHistoryEntry`를 권한별로 투영한다.

## 2. 완료 이력 생성

트리거: Provider가 완료를 제출하고 고객이 최신 revision을 `COMPLETED`로 확정.

원 처리:

1. Transaction과 completion revision 상태 확정
2. `COMPLETION` outbox event 기록
3. 소비자가 `ServiceHistoryEntry` 영속 생성
4. Transaction+completion revision 멱등키로 중복 차단

연결값:

- 요청과 카테고리/동적응답 스냅샷
- 채택 견적 총액·VAT·항목·범위·보증
- 실제 작업내용·금액·증빙
- 완료 제출에 적용된 카테고리 정책버전/스냅샷, `requiredPhotoCount`, 선택적 사진 역할 요구사항
- 고객 완료확정일
- warrantyDays와 계산된 warrantyTo

## 3. 고객 이력

- 본인 Transaction만 조회
- 기간, 카테고리, Provider, 시설·제품, A/S 상태 필터
- 견적, 정책에 따라 제출된 완료증빙과 사진 역할, 실제금액, 보증, A/S를 한 상세에서 연결
- 최근 서비스일 내림차순

## 4. Provider 이력

- 본인이 수행한 Transaction만 조회
- 고객 식별정보 최소화
- 작업내용·실제금액·완료증빙·보증·A/S 제공
- 시설·제품은 해당 Transaction에 연결된 범위만 제공

## 5. 시설·제품 카드

MVP 포함 기능:

- 고객 최소 등록: 유형, 이름, 제조사/모델 등 선택정보
- 고객 본인 목록·상세 조회
- 본인 Transaction 연결
- 자산별 완료/A·S 시간순 이력
- 해당 거래 Provider의 제한 조회
- 연결 정정과 감사

관계: 고객 1:N ServiceAsset, Transaction N:M ServiceAsset, 링크는 `TransactionAssetLink`.

## 6. 최소 A/S

### 상태

`RECEIVED → IN_PROGRESS → COMPLETED`

### 보증

- 시작일: 고객 완료확정일
- 기간: 채택/완료에 확정된 `warrantyDays`
- 관리대장 `기본 A/S일`은 카테고리 기본값(현재 0/3/30일)
- 거래에서 확정된 스냅샷을 후속 정책변경보다 우선

### 사건

- 접수: `AFTER_SERVICE_RECEIVED`
- 진행: `AFTER_SERVICE_STARTED`
- 완료: `AFTER_SERVICE_COMPLETED`

원 완료 이력은 수정하지 않고 사건을 추가한다.

### 재방문 구분

- 보증 범위 내 재방문: 같은 AfterServiceCase
- 별도 유상 추가작업: 새 ServiceRequest와 Transaction

## 7. 접근권한

- 소유 고객: 본인 시설·거래·A/S 전체
- 해당 Provider: 본인 수행 Transaction과 연결된 시설/A·S 범위
- 관리자: 업무권한+사유+감사
- 타 고객/타 Provider: 차단

## 8. 정정·삭제

- 원 요청·견적·Transaction·완료를 이력화면에서 편집하지 않는다.
- 관리대장 정책변경 후에도 이력은 해당 Transaction에 고정된 완료증빙 정책버전/스냅샷과 검증 당시 사진 역할을 표시한다.
- 자산 링크 정정은 기존 링크 상태와 전후값을 보존한다.
- A/S 조치는 새 action/event로 추가한다.
- 물리삭제·보존기간은 OI-034에서 확정한다.

## 9. 테스트

| ID | 시나리오 | 합격 기준 |
|---|---|---|
| `HIS-001` | 완료 자동생성 | 재시도에도 동일 Transaction 이력 1건 |
| `HIS-002` | 양측 조회 | 고객/Provider가 같은 원본을 권한별 조회 |
| `HIS-003` | 권한 | 타 사용자 접근 차단 |
| `HIS-004` | 시설 누적 | 여러 거래와 A/S가 시간순 연결 |
| `HIS-005` | 보증 | 고객 확정일+warrantyDays 계산 일치 |
| `HIS-006` | A/S | 접수→진행→완료 사건과 원 거래 연결 |
| `HIS-007` | 링크 정정 | 전후값·사유·행위자 보존 |
| `HIS-008` | 증빙정책 보존 | 관리대장 변경 후에도 기존 거래의 사진 개수·역할 조건과 제출 증빙 불변 |
