# 핵심 도메인 모델

## 1. 표준 용어

- 거래: `Transaction`
- 전문가: `Provider`
- 출장지역: `ProviderServiceArea`
- 완료자료: `WorkCompletion`
- A/S: `AfterServiceCase`
- 외부 식별자: `publicId`

문서의 `Deal`, `Partner`, `ProviderCoverageArea`, `CompletionEvidence`는 구현 표준명으로 사용하지 않는다.

## 2. 도메인 경계

### IAM

- `UserAccount`, `UserRole`, `CustomerProfile`, `ProviderProfile`
- MVP 기본 계정 로그인만 포함

### 카탈로그·정책

- `ServiceCategory`: 관리대장 카테고리 마스터
- `CategoryFieldDefinition`: 837개 동적필드 정의
- `CategoryPolicy`: 견적시간·최대견적·완료증빙·`requiredPhotoCount`·선택적 사진 역할·보증 등 실제 정책
- `AdministrativeArea`: 공식 시·군·구
- `ProviderServiceCategory`
- `ProviderServiceArea`

### 요청·매칭

- `ServiceRequest`, `RequestAnswer`, `RequestMedia`
- `DispatchCandidate`: 계산된 후보
- `RequestDispatch`: 실제 Provider 노출
- `NotificationEvent/Log`: 외부 전송 없는 이벤트·로그

### 견적·채택

- `Quote`: 논리 견적
- `QuoteRevision`: 채택 전 수정이력
- `QuoteItem`: revision별 항목내역
- 단일 채택 사실은 `Quote.status+Transaction.acceptedQuoteRevisionId`로 표현한다. 별도 Acceptance 테이블은 필수 아님.

### 거래·완료

- `Transaction`: 채택 후 업무 집계루트
- `WorkCompletion`: 완료 제출 논리 식별자
- `WorkCompletionRevision`: 보완을 포함한 제출 차수
- `CompletionMedia`: 완료사진 파일과 정책상 역할(`BEFORE/AFTER/DETAIL/SERIAL/DAMAGE` 등)
- `CustomerConfirmation`: 정상/보완/분쟁 확인

### 이력·시설·A/S

- `ServiceHistoryEntry`: 완료·A/S·연결 사건
- `ServiceHistoryItem`: 작업·자재·금액·보증 세부
- `ServiceAsset`: 시설·제품·공간 최소 마스터
- `TransactionAssetLink`: 거래-자산 연결과 당시 스냅샷
- `AfterServiceCase`: 접수→진행→완료
- `AfterServiceAction/Media`: 진행·완료 조치와 증빙

### 공통

- `MediaFile`, `AuditLog`, `OutboxEvent`

## 3. 핵심 관계

| 부모 | 관계 | 자식 | 제약 |
|---|---:|---|---|
| UserAccount | 1:N | UserRole | 활성 역할 유일 |
| ProviderProfile | 1:N | ProviderServiceCategory | Provider+활성 카테고리 유일 |
| ProviderServiceCategory | 1:N | ProviderServiceArea | 카테고리별 시군구 |
| ServiceCategory | 1:N | CategoryFieldDefinition | 적용 서비스/중분류 규칙 |
| CustomerProfile | 1:N | ServiceRequest | 작성자 필수 |
| ServiceRequest | 1:N | RequestAnswer | 요청+필드+버전 유일 |
| ServiceRequest | 1:N | DispatchCandidate | 요청+Provider 후보 유일 |
| DispatchCandidate | 1:0..1 | RequestDispatch | 실제 배포 시 생성 |
| ServiceRequest | 1:N | Quote | Provider당 활성 1개 |
| Quote | 1:N | QuoteRevision | revision 번호 유일 |
| QuoteRevision | 1:N | QuoteItem | 항목합 검증 |
| ServiceRequest | 1:0..1 | Transaction | 활성 거래 최대 1개 |
| QuoteRevision | 1:0..1 | Transaction | 채택 revision |
| Transaction | 1:N | WorkCompletionRevision | 최종확정 최대 1개 |
| Transaction | 1:N | ServiceHistoryEntry | 완료/A·S/연결 사건 |
| CustomerProfile | 1:N | ServiceAsset | 소유 고객 |
| Transaction | N:M | ServiceAsset | TransactionAssetLink |
| Transaction | 1:N | AfterServiceCase | 원 거래 필수 |
| AfterServiceCase | 1:N | ServiceHistoryEntry | 접수/진행/완료 사건 |

## 4. 집계·원자성

### ServiceRequest

- 공개 시 동적필드와 카테고리 정책을 검증한다.
- 채택 시 요청 rowVersion, 최신 QuoteRevision, Provider 상태, 기존 Transaction을 다시 검사한다.
- 채택 견적·요청·나머지 견적·Transaction을 한 DB 트랜잭션으로 변경한다.

### Transaction

- 작업 진행, 완료 revision, 고객 확인을 관리한다.
- 생성 또는 작업 수행 시 적용 카테고리 완료증빙 정책버전/스냅샷을 고정하고, 완료 제출은 이 스냅샷의 사진 최소 개수와 선택적 역할별 조건을 검증한다.
- `requiredPhotoCount`와 역할은 데이터로 해석하며 특정 숫자나 역할 조합을 도메인 로직에 하드코딩하지 않는다.
- 고객 `COMPLETED` 확인과 Transaction 상태확정을 원자 처리한다.
- `ServiceHistoryEntry`는 outbox로 연결하되 소비는 멱등해야 한다.

### ServiceHistory

- 원 거래를 복제한 새 거래가 아니다.
- 사건은 append-only다.
- 조회 요약 스냅샷은 허용하지만 원 요청·견적·Transaction·증빙 링크가 필수다.

## 5. 스냅샷

| 시점 | 보존값 |
|---|---|
| 요청 공개 | 카테고리 코드/명칭/정책버전, 시군구, 동적응답 |
| 견적 revision | Provider 표시정보, 총액/VAT/항목/범위/보증 |
| 채택/작업 수행 | 채택 revision, 당사자, 카테고리 정책버전, 완료증빙 규칙, requiredPhotoCount, 선택적 필수 사진 역할 |
| 완료확정 | 작업내용, 실제금액, 정책에 따라 검증된 증빙과 역할, 고객확정일, warrantyDays/warrantyTo |
| 자산 연결 | 당시 시설명·모델·위치 요약 |

수수료와 신뢰점수 정책값은 관리대장에 보존하지만 MVP 실행 스냅샷의 필수 계산대상은 아니다.

## 6. 접근 경계

- 고객: 본인 요청·Transaction·시설·이력·A/S
- Provider: 본인 배포·견적·수행 Transaction 및 해당 거래 연결 시설/A·S
- 관리자: 업무권한+사유+감사 범위
- 모든 객체는 역할 외에 customerId/providerId/transaction 관계를 검사한다.
