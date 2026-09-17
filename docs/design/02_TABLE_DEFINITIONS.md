# 테이블 정의서

## 1. 표기와 공통 규칙

- 모든 PK는 `id BIGINT IDENTITY(1,1)` clustered primary key다.
- `public_id`는 애플리케이션에서 UUID v4를 생성하며 DB default는 두지 않는다.
- `UQ`는 unique constraint/index, `IX`는 non-unique index 후보, `FIX`는 filtered index 후보를 뜻한다.
- 모든 FK delete action은 별도 언급이 없어도 `NO ACTION`이다.
- 변경 가능한 테이블의 `row_version ROWVERSION`은 NULL/기본값 개념 없이 SQL Server가 생성한다.
- `created_by_user_id`, `updated_by_user_id`는 시스템 작업을 허용하기 위해 NULL이며 `users.id`를 참조한다.

## 2. 계정·역할·전문가 승인

### 2.1 `users`

- 업무 목적: 인증 주체와 공통 사용자 상태 보관
- PK: `id`

| 컬럼 | SQL Server 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| public_id | UNIQUEIDENTIFIER | N | 앱 UUID v4 | UQ | API 공개 ID |
| login_id | NVARCHAR(256) | N | 없음 |  | 사용자 입력 로그인 식별자 |
| normalized_login_id | NVARCHAR(256) | N | 없음 | UQ | 대소문자/형식 정규화 비교값 |
| password_hash | NVARCHAR(512) | N | 없음 |  | 검증된 hasher의 포맷 문자열, 원문 금지 |
| email | NVARCHAR(320) | Y | NULL | IX 후보 | 이메일, 사용 여부는 인증 결정에 따름 |
| phone | NVARCHAR(32) | Y | NULL | IX 후보 | E.164 등 정규화 정책은 구현 전에 확정 |
| status_code | VARCHAR(20) | N | `ACTIVE` | CHECK, IX | ACTIVE/SUSPENDED/WITHDRAWN |
| last_login_at | DATETIME2(7) | Y | NULL |  | 마지막 성공 로그인 UTC |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() | IX | 생성 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | 자기등록/시스템은 NULL 가능 |
| updated_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 수정 UTC |
| updated_by_user_id | BIGINT | Y | NULL | FK users.id | 수정자 |
| row_version | ROWVERSION | N | SQL Server | concurrency | 낙관적 동시성 |

### 2.2 `roles`

- 업무 목적: 고객/전문가/관리자 역할 기준값
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| code | VARCHAR(30) | N | 없음 | UQ | CUSTOMER/PROVIDER/ADMIN |
| name | NVARCHAR(100) | N | 없음 |  | 표시명 |
| is_active | BIT | N | 1 | IX | 부여 가능 여부 |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 생성 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | 생성자 |
| updated_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 수정 UTC |
| updated_by_user_id | BIGINT | Y | NULL | FK users.id | 수정자 |
| row_version | ROWVERSION | N | SQL Server | concurrency | 동시성 |

## 3. 공급 서비스·파일

### 3.1 `provider_service_categories`

- 업무 목적: 승인 전문가가 제공하는 leaf 서비스 카테고리
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| provider_profile_id | BIGINT | N | 없음 | FK provider_profiles.id, IX | 전문가 |
| category_id | BIGINT | N | 없음 | FK service_categories.id, IX | SERVICE 노드 |
| status_code | VARCHAR(20) | N | `ACTIVE` | CHECK, IX | ACTIVE/INACTIVE |
| activated_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 활성 시작 UTC |
| deactivated_at | DATETIME2(7) | Y | NULL |  | 비활성 UTC |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 생성 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | 생성자 |
| updated_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 수정 UTC |
| updated_by_user_id | BIGINT | Y | NULL | FK users.id | 수정자 |
| row_version | ROWVERSION | N | SQL Server | concurrency | 동시성 |

unique/index 후보: `UQ(provider_profile_id, category_id)`, `(category_id, status_code, provider_profile_id)`.

### 3.2 `provider_service_areas`

- 업무 목적: 전문가의 카테고리별 서비스 가능 SIGUNGU
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| provider_service_category_id | BIGINT | N | 없음 | FK provider_service_categories.id, IX | 공급 카테고리 |
| administrative_area_id | BIGINT | N | 없음 | FK administrative_areas.id, IX | SIGUNGU |
| status_code | VARCHAR(20) | N | `ACTIVE` | CHECK, IX | ACTIVE/INACTIVE |
| activated_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 활성 시작 |
| deactivated_at | DATETIME2(7) | Y | NULL |  | 비활성 시각 |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 생성 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | 생성자 |
| updated_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 수정 UTC |
| updated_by_user_id | BIGINT | Y | NULL | FK users.id | 수정자 |
| row_version | ROWVERSION | N | SQL Server | concurrency | 동시성 |

unique/index 후보: `UQ(provider_service_category_id, administrative_area_id)`, `(administrative_area_id, status_code, provider_service_category_id)`.

### 3.3 `files`

- 업무 목적: private object storage 파일 메타데이터와 검사 상태
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| public_id | UNIQUEIDENTIFIER | N | 앱 UUID v4 | UQ | API 공개 ID |
| purpose_code | VARCHAR(30) | N | 없음 | CHECK, IX | 파일 목적 |
| storage_container | NVARCHAR(200) | N | 없음 |  | 논리 bucket/container |
| storage_key | NVARCHAR(1000) | N | 없음 |  | opaque object key 원문; 직접 index하지 않음 |
| storage_key_hash | BINARY(32) | N | 없음 | UQ | `storage_key` UTF-8 바이트의 SHA-256; 애플리케이션에서 계산 |
| original_file_name | NVARCHAR(255) | N | 없음 |  | 표시용, 경로 사용 금지 |
| content_type | VARCHAR(200) | N | 없음 | IX 후보 | 검사된 MIME |
| size_bytes | BIGINT | N | 없음 | CHECK >= 0 | 크기 |
| sha256_hex | CHAR(64) | N | 없음 | IX 후보 | 무결성 hash |
| status_code | VARCHAR(20) | N | `PENDING` | CHECK, IX | PENDING/ACTIVE/QUARANTINED/DELETED |
| scan_result_text | NVARCHAR(1000) | Y | NULL |  | 악성코드/형식 검사 요약, 비밀 제외 |
| activated_at | DATETIME2(7) | Y | NULL |  | ACTIVE 전환 UTC |
| deleted_at | DATETIME2(7) | Y | NULL |  | 논리 파기 UTC |
| uploaded_by_user_id | BIGINT | Y | NULL | FK users.id, IX | 업로더 |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() | IX | 메타데이터 생성 UTC |
| row_version | ROWVERSION | N | SQL Server | concurrency | 상태 동시성 |

### 3.4 `provider_documents`

- 업무 목적: 전문가 승인용 사업자/자격/보험 파일 연결
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| provider_profile_id | BIGINT | N | 없음 | FK provider_profiles.id, IX | 전문가 |
| file_id | BIGINT | N | 없음 | FK files.id, UQ | 파일 |
| document_type_code | VARCHAR(50) | N | 없음 | IX | 사업자/자격/보험 유형; 값 목록은 승인 흐름에서 확정 |
| document_number | NVARCHAR(100) | Y | NULL |  | 문서 번호, 민감도 검토 |
| issued_at | DATE | Y | NULL |  | 발급일 |
| expires_at | DATE | Y | NULL | IX | 만료일 |
| verification_status_code | VARCHAR(20) | N | `PENDING` | IX | PENDING/VERIFIED/REJECTED 제안 |
| verified_at | DATETIME2(7) | Y | NULL |  | 검증 UTC |
| verified_by_user_id | BIGINT | Y | NULL | FK users.id | 관리자 |
| note | NVARCHAR(1000) | Y | NULL |  | 검증 메모 |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 생성 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | 생성자 |
| updated_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 수정 UTC |
| updated_by_user_id | BIGINT | Y | NULL | FK users.id | 수정자 |
| row_version | ROWVERSION | N | SQL Server | concurrency | 동시성 |

`document_type_code`와 `verification_status_code`의 최종 값은 전문가 승인 화면 상세가 확정될 때 코드 문서에 추가한다. 스키마는 문자열 코드로 확장 가능하다.

## 4. 고객 요청·동적 답변

### 4.1 `service_requests`

- 업무 목적: 고객 서비스 요청 aggregate
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| public_id | UNIQUEIDENTIFIER | N | 앱 UUID v4 | UQ | API 공개 ID |
| customer_profile_id | BIGINT | N | 없음 | FK customer_profiles.id, IX | 요청 고객 |
| category_id | BIGINT | N | 없음 | FK service_categories.id, IX | leaf 서비스 |
| category_policy_id | BIGINT | N | 없음 | FK category_policies.id, IX | 요청 시 정책 |
| administrative_area_id | BIGINT | N | 없음 | FK administrative_areas.id, IX | 매칭 SIGUNGU |
| detail_address | NVARCHAR(500) | Y | NULL |  | 상세주소, 채택 전 전문가 비공개 |
| title | NVARCHAR(200) | N | 없음 |  | 요청 제목 |
| description | NVARCHAR(MAX) | Y | NULL |  | 공통 설명 |
| status_code | VARCHAR(20) | N | `DRAFT` | CHECK, IX | 요청 상태 |
| is_urgent | BIT | N | 0 | IX | 긴급 요청 여부 |
| policy_snapshot_json | NVARCHAR(MAX) | N | 없음 | ISJSON CHECK | 요청 당시 정책 snapshot |
| opened_at | DATETIME2(7) | Y | NULL | IX | OPEN UTC |
| expires_at | DATETIME2(7) | Y | NULL | IX | 요청 만료 UTC |
| accepted_at | DATETIME2(7) | Y | NULL |  | ACCEPTED UTC |
| cancelled_at | DATETIME2(7) | Y | NULL |  | 취소 UTC |
| cancellation_reason | NVARCHAR(1000) | Y | NULL |  | 취소 사유 |
| idempotency_key | VARCHAR(100) | Y | NULL | UQ filtered | 요청 생성 중복 방지 |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() | IX | 생성 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | 생성자 |
| updated_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 수정 UTC |
| updated_by_user_id | BIGINT | Y | NULL | FK users.id | 수정자 |
| row_version | ROWVERSION | N | SQL Server | concurrency | 동시성 |

주요 index: `(category_id, administrative_area_id, status_code, opened_at)`, `(customer_profile_id, created_at DESC)`.

### 4.2 `request_answers`

- 업무 목적: 동적 필드에 대한 typed 답변
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| service_request_id | BIGINT | N | 없음 | FK service_requests.id, IX | 요청 |
| field_definition_id | BIGINT | N | 없음 | FK category_field_definitions.id, IX | 필드 정의 |
| value_text | NVARCHAR(MAX) | Y | NULL |  | TEXT/LONG_TEXT/ADDRESS/SELECT |
| value_number | DECIMAL(19,4) | Y | NULL |  | NUMBER/MONEY |
| value_boolean | BIT | Y | NULL |  | 향후 boolean 입력 |
| value_date | DATE | Y | NULL |  | 날짜 값 |
| value_datetime | DATETIME2(7) | Y | NULL |  | DATETIME UTC |
| value_json | NVARCHAR(MAX) | Y | NULL | ISJSON CHECK | PERIOD/RECURRENCE/복합 주소 |
| value_currency_code | CHAR(3) | Y | NULL | CHECK 후보 | MONEY일 때 통화 |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 생성 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | 생성자 |
| updated_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 수정 UTC |
| updated_by_user_id | BIGINT | Y | NULL | FK users.id | 수정자 |
| row_version | ROWVERSION | N | SQL Server | concurrency | 동시성 |

unique 후보: `(service_request_id, field_definition_id)`. 값 컬럼의 타입 일치는 애플리케이션이 검증하며 최소 한 값만 존재하는 CHECK는 FILE 전용 답변을 허용하도록 신중히 구성한다.

### 4.3 `request_answer_files`

- 업무 목적: FILE 유형 답변과 파일 연결
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| request_answer_id | BIGINT | N | 없음 | FK request_answers.id, IX | 답변 |
| file_id | BIGINT | N | 없음 | FK files.id, UQ with answer | 파일 |
| display_order | INT | N | 0 |  | 표시 순서 |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 연결 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | 연결자 |

unique 후보: `(request_answer_id, file_id)`.

## 5. 매칭·배포·알림

### 5.1 `dispatch_candidates`

- 업무 목적: 요청 시점 전문가 적격성 계산 결과
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| service_request_id | BIGINT | N | 없음 | FK service_requests.id, IX | 요청 |
| provider_profile_id | BIGINT | N | 없음 | FK provider_profiles.id, IX | 전문가 |
| status_code | VARCHAR(20) | N | 없음 | CHECK, IX | ELIGIBLE/INELIGIBLE/DISPATCHED/EXPIRED |
| category_match | BIT | N | 없음 |  | 카테고리 조건 결과 |
| area_match | BIT | N | 없음 |  | SIGUNGU 조건 결과 |
| approval_match | BIT | N | 없음 |  | 승인/활동 조건 결과 |
| reason_code | VARCHAR(50) | Y | NULL | IX 후보 | 부적격/제외 이유 |
| evaluated_at | DATETIME2(7) | N | SYSUTCDATETIME() | IX | 판정 UTC |
| expires_at | DATETIME2(7) | Y | NULL | IX | 후보 만료 |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 생성 UTC |

unique 후보: `(service_request_id, provider_profile_id)`; 조회 index `(service_request_id, status_code)`.

### 5.2 `request_dispatches`

- 업무 목적: 적격 전문가에게 요청을 실제 공개/배포한 기록
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| public_id | UNIQUEIDENTIFIER | N | 앱 UUID v4 | UQ | 운영/API 식별 |
| service_request_id | BIGINT | N | 없음 | FK service_requests.id, IX | 요청 |
| provider_profile_id | BIGINT | N | 없음 | FK provider_profiles.id, IX | 수신 전문가 |
| candidate_id | BIGINT | N | 없음 | FK dispatch_candidates.id, UQ | 근거 후보 |
| status_code | VARCHAR(20) | N | `AVAILABLE` | CHECK, IX | 배포 상태 |
| available_at | DATETIME2(7) | N | SYSUTCDATETIME() | IX | 공개 UTC |
| viewed_at | DATETIME2(7) | Y | NULL |  | 최초 조회 UTC |
| responded_at | DATETIME2(7) | Y | NULL |  | 응답 UTC |
| expires_at | DATETIME2(7) | N | 없음 | IX | 노출 만료 UTC |
| idempotency_key | VARCHAR(100) | N | 없음 | UQ | 중복 배포 방지 |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 생성 UTC |
| row_version | ROWVERSION | N | SQL Server | concurrency | 상태 동시성 |

unique 후보: `(service_request_id, provider_profile_id)`; 전문가 inbox index `(provider_profile_id, status_code, available_at DESC)`.

### 5.3 `notifications`

- 업무 목적: 인앱/외부 알림의 논리 이벤트와 개인정보 최소 payload
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| public_id | UNIQUEIDENTIFIER | N | 앱 UUID v4 | UQ | 공개 ID |
| recipient_user_id | BIGINT | N | 없음 | FK users.id, IX | 수신자 |
| request_dispatch_id | BIGINT | Y | NULL | FK request_dispatches.id, IX | 배포 원인 |
| type_code | VARCHAR(50) | N | 없음 | IX | 알림 종류 |
| status_code | VARCHAR(30) | N | `PENDING` | CHECK, IX | 논리 상태 |
| title | NVARCHAR(200) | N | 없음 |  | 제목 |
| body | NVARCHAR(2000) | N | 없음 |  | 민감정보 없는 본문 |
| data_json | NVARCHAR(MAX) | Y | NULL | ISJSON CHECK | public ID 중심 payload |
| is_urgent | BIT | N | 0 | IX | 우선순위 |
| recorded_at | DATETIME2(7) | N | SYSUTCDATETIME() | IX | 생성/기록 UTC |
| read_at | DATETIME2(7) | Y | NULL | IX 후보 | 인앱 읽음 UTC |
| idempotency_key | VARCHAR(100) | N | 없음 | UQ | 알림 중복 방지 |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | actor/시스템 |
| row_version | ROWVERSION | N | SQL Server | concurrency | 상태 동시성 |

inbox index: `(recipient_user_id, read_at, recorded_at DESC)`.

### 5.4 `notification_deliveries`

- 업무 목적: 알림 채널별 발송 시도와 실패 기록
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| notification_id | BIGINT | N | 없음 | FK notifications.id, IX | 논리 알림 |
| channel_code | VARCHAR(20) | N | 없음 | CHECK, IX | IN_APP/ALIMTALK |
| attempt_no | SMALLINT | N | 1 | UQ with notification/channel | 재시도 번호 |
| status_code | VARCHAR(20) | N | `PENDING` | CHECK, IX | PENDING/SENT/FAILED/SKIPPED |
| provider_message_id | NVARCHAR(200) | Y | NULL | IX 후보 | 외부 제공자 ID |
| error_code | NVARCHAR(100) | Y | NULL |  | 실패 코드 |
| error_message | NVARCHAR(1000) | Y | NULL |  | 비밀/개인정보 제거 메시지 |
| attempted_at | DATETIME2(7) | N | SYSUTCDATETIME() | IX | 시도 UTC |
| completed_at | DATETIME2(7) | Y | NULL |  | 완료 UTC |

unique 후보: `(notification_id, channel_code, attempt_no)`.

## 6. 견적

### 6.1 `quotes`

- 업무 목적: 요청별 전문가 1개의 논리 견적 aggregate
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| public_id | UNIQUEIDENTIFIER | N | 앱 UUID v4 | UQ | API 공개 ID |
| service_request_id | BIGINT | N | 없음 | FK service_requests.id, IX | 요청 |
| provider_profile_id | BIGINT | N | 없음 | FK provider_profiles.id, IX | 전문가 |
| request_dispatch_id | BIGINT | N | 없음 | FK request_dispatches.id, IX | 응답 배포 |
| status_code | VARCHAR(20) | N | `DRAFT` | CHECK, IX | 견적 상태 |
| submitted_at | DATETIME2(7) | Y | NULL | IX | 최초/최근 제출 시각은 revision과 함께 갱신 |
| accepted_at | DATETIME2(7) | Y | NULL |  | 채택 UTC |
| withdrawn_at | DATETIME2(7) | Y | NULL |  | 철회 UTC |
| expires_at | DATETIME2(7) | Y | NULL | IX | 현재 제출 revision 만료 |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 생성 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | 생성자 |
| updated_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 수정 UTC |
| updated_by_user_id | BIGINT | Y | NULL | FK users.id | 수정자 |
| row_version | ROWVERSION | N | SQL Server | concurrency | 채택 경쟁 제어 |

unique 후보: `(service_request_id, provider_profile_id)`; 목록 index `(service_request_id, status_code, submitted_at DESC)`.

### 6.2 `quote_revisions`

- 업무 목적: 제출/수정된 견적 내용의 immutable 버전
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| public_id | UNIQUEIDENTIFIER | N | 앱 UUID v4 | UQ | API revision ID |
| quote_id | BIGINT | N | 없음 | FK quotes.id, IX | 논리 견적 |
| revision_no | INT | N | 없음 | UQ with quote | 1부터 증가 |
| summary | NVARCHAR(1000) | N | 없음 |  | 작업 요약 |
| terms | NVARCHAR(MAX) | Y | NULL |  | 조건/설명 |
| subtotal_amount | DECIMAL(19,4) | N | 없음 |  | 공급가/소계 |
| vat_amount | DECIMAL(19,4) | N | 0 |  | 부가세 |
| total_amount | DECIMAL(19,4) | N | 없음 | IX 후보 | 총액 |
| currency_code | CHAR(3) | N | `KRW` | CHECK 후보 | 통화 |
| estimated_duration_text | NVARCHAR(200) | Y | NULL |  | 예상 작업기간 원문 |
| available_start_at | DATETIME2(7) | Y | NULL |  | 가능 시작 UTC |
| valid_until | DATETIME2(7) | N | 없음 | IX | 견적 유효 UTC |
| revision_reason | NVARCHAR(1000) | Y | NULL |  | 수정 사유 |
| submitted_at | DATETIME2(7) | N | SYSUTCDATETIME() | IX | 제출 UTC |
| submitted_by_user_id | BIGINT | N | 없음 | FK users.id | 제출 전문가 계정 |
| idempotency_key | VARCHAR(100) | N | 없음 | UQ | 제출 중복 방지 |

unique/index 후보: `(quote_id, revision_no)`, `(quote_id, submitted_at DESC)`.

### 6.3 `quote_items`

- 업무 목적: 견적 revision의 구조화된 항목
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| quote_revision_id | BIGINT | N | 없음 | FK quote_revisions.id, IX | 견적 revision |
| line_no | INT | N | 없음 | UQ with revision | 표시 순번 |
| item_name | NVARCHAR(200) | N | 없음 |  | 항목명 |
| description | NVARCHAR(1000) | Y | NULL |  | 설명 |
| quantity | DECIMAL(19,4) | N | 1 | CHECK > 0 | 수량 |
| unit_text | NVARCHAR(50) | Y | NULL |  | 단위 |
| unit_price_amount | DECIMAL(19,4) | N | 없음 |  | 단가 |
| line_total_amount | DECIMAL(19,4) | N | 없음 |  | 항목 합계 |
| currency_code | CHAR(3) | N | `KRW` | CHECK 후보 | 통화 |

unique 후보: `(quote_revision_id, line_no)`.

## 7. 계정 역할·프로필·승인 상세

### 7.1 `user_roles`

- 업무 목적: 한 사용자의 다중 역할과 부여/회수 이력
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| user_id | BIGINT | N | 없음 | FK users.id, IX | 사용자 |
| role_id | BIGINT | N | 없음 | FK roles.id, IX | 역할 |
| granted_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 부여 UTC |
| granted_by_user_id | BIGINT | Y | NULL | FK users.id | 부여자/시스템 |
| revoked_at | DATETIME2(7) | Y | NULL | FIX | 회수 UTC |
| revoked_by_user_id | BIGINT | Y | NULL | FK users.id | 회수자 |

인덱스 후보: `UQ/FIX (user_id, role_id) WHERE revoked_at IS NULL`.

### 7.2 `customer_profiles`

- 업무 목적: 고객 역할의 업무 프로필
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| public_id | UNIQUEIDENTIFIER | N | 앱 UUID v4 | UQ | API 공개 ID |
| user_id | BIGINT | N | 없음 | FK users.id, UQ | 계정당 하나 |
| display_name | NVARCHAR(100) | N | 없음 |  | 고객 표시명/이름 |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 생성 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | 생성자 |
| updated_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 수정 UTC |
| updated_by_user_id | BIGINT | Y | NULL | FK users.id | 수정자 |
| row_version | ROWVERSION | N | SQL Server | concurrency | 동시성 |

### 7.3 `provider_profiles`

- 업무 목적: 전문가 업무정보, 승인 현재값, 활동 상태
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| public_id | UNIQUEIDENTIFIER | N | 앱 UUID v4 | UQ | API 공개 ID |
| user_id | BIGINT | N | 없음 | FK users.id, UQ | 계정당 하나 |
| business_name | NVARCHAR(200) | N | 없음 | IX 후보 | 상호/표시명 |
| business_registration_no | NVARCHAR(32) | Y | NULL | UQ 후보 | 저장/암호화 범위 DBI-003 |
| approval_status_code | VARCHAR(20) | N | `PENDING` | CHECK, IX | PENDING/APPROVED/REJECTED/SUSPENDED |
| activity_status_code | VARCHAR(20) | N | `INACTIVE` | CHECK, IX | ACTIVE/INACTIVE |
| trust_score | DECIMAL(9,4) | Y | NULL | IX 후보 | 산식 미확정, 신규는 NULL |
| approval_decided_at | DATETIME2(7) | Y | NULL |  | 최근 승인 결정 UTC |
| approval_decided_by_user_id | BIGINT | Y | NULL | FK users.id | 최근 결정 관리자 |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 생성 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | 생성자 |
| updated_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 수정 UTC |
| updated_by_user_id | BIGINT | Y | NULL | FK users.id | 수정자 |
| row_version | ROWVERSION | N | SQL Server | concurrency | 동시성 |

복합 index 후보: `(approval_status_code, activity_status_code, trust_score DESC)`.

### 7.4 `provider_approval_events`

- 업무 목적: 전문가 승인/반려/정지 결정의 append-only 업무 이력
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| provider_profile_id | BIGINT | N | 없음 | FK provider_profiles.id, IX | 대상 전문가 |
| from_status_code | VARCHAR(20) | Y | NULL | CHECK | 최초 결정은 NULL 가능 |
| to_status_code | VARCHAR(20) | N | 없음 | CHECK | 변경 후 승인 상태 |
| action_code | VARCHAR(20) | N | 없음 | CHECK | APPROVE/REJECT/SUSPEND/RESUME |
| reason | NVARCHAR(1000) | Y | NULL |  | 결정 사유 |
| decided_at | DATETIME2(7) | N | SYSUTCDATETIME() | IX | 결정 UTC |
| decided_by_user_id | BIGINT | N | 없음 | FK users.id, IX | 관리자 |
| correlation_id | UNIQUEIDENTIFIER | Y | NULL | IX | 요청 추적 ID |

복합 index 후보: `(provider_profile_id, decided_at DESC)`.

## 8. 카테고리·정책·지역

### 8.1 `service_categories`

- 업무 목적: 대분류/중분류/하위 서비스를 단일 계층으로 관리
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| public_id | UNIQUEIDENTIFIER | N | 앱 UUID v4 | UQ | API 공개 ID |
| parent_id | BIGINT | Y | NULL | FK self, IX | MAJOR는 NULL |
| level_code | VARCHAR(20) | N | 없음 | CHECK, IX | MAJOR/MIDDLE/SERVICE |
| external_code | VARCHAR(50) | Y | NULL | UQ filtered | Excel leaf 코드 |
| source_record_id | VARCHAR(50) | Y | NULL | UQ filtered | Excel ID |
| name | NVARCHAR(200) | N | 없음 | UQ with parent | 표시명 |
| status_code | VARCHAR(20) | N | `ACTIVE` | CHECK, IX | ACTIVE/PAUSED/REVIEW |
| sort_order | INT | N | 0 | IX 후보 | 동일 부모 내 정렬 |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 생성 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | 생성자 |
| updated_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 수정 UTC |
| updated_by_user_id | BIGINT | Y | NULL | FK users.id | 수정자 |
| row_version | ROWVERSION | N | SQL Server | concurrency | 동시성 |

unique/index 후보: `UQ(parent_id, name)`(필터 없음; 최상위 `parent_id IS NULL` 이름 중복도 금지), `UQ external_code WHERE external_code IS NOT NULL`, `(parent_id, status_code, sort_order)`.

### 8.2 `category_policies`

- 업무 목적: leaf 서비스의 가격/매칭/완료/보증 정책 버전
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| public_id | UNIQUEIDENTIFIER | N | 앱 UUID v4 | UQ | 정책 공개/운영 ID |
| category_id | BIGINT | N | 없음 | FK service_categories.id, IX | SERVICE 노드 |
| policy_version | VARCHAR(30) | N | 없음 | UQ with category | Excel v1.1 등 |
| transaction_type_code | VARCHAR(20) | N | 없음 | CHECK, IX | ONE_TIME/SUBSCRIPTION/PROJECT |
| request_method_text | NVARCHAR(300) | N | 없음 |  | 요청 방식 원문 |
| onsite_requirement_text | NVARCHAR(30) | N | 없음 |  | 필수/선택 원문 |
| is_emergency_allowed | BIT | N | 0 | IX | 긴급 허용 |
| subscription_option_text | NVARCHAR(30) | N | 없음 |  | 정기구독 허용 원문 |
| standard_work_unit_text | NVARCHAR(100) | N | 없음 |  | 작업 단위 |
| base_price_amount | DECIMAL(19,4) | N | 없음 |  | 기본요금 |
| currency_code | CHAR(3) | N | `KRW` | CHECK 후보 | 통화 |
| price_method_text | NVARCHAR(30) | N | 없음 |  | 예약가/견적형 |
| vat_display_rule_text | NVARCHAR(100) | N | 없음 |  | 부가세 표시 규칙 |
| minimum_budget_amount | DECIMAL(19,4) | N | 없음 |  | 최소 희망예산 |
| max_quote_count | SMALLINT | N | 없음 | CHECK > 0 | 최대 견적 수 |
| quote_validity_minutes | INT | N | 없음 | CHECK > 0 | 견적 유효기간 |
| fee_policy_id | BIGINT | N | 없음 | FK fee_policies.id, IX | 수수료 정책 |
| estimated_quote_fee_amount | DECIMAL(19,4) | N | 없음 |  | 예상 수수료 |
| fee_charge_timing_text | NVARCHAR(200) | N | 없음 |  | 차감 시점 원문 |
| fee_restore_condition_text | NVARCHAR(500) | N | 없음 |  | 복원 조건 원문 |
| matching_area_rule_text | NVARCHAR(200) | N | 없음 |  | 매칭 지역 원문 |
| notification_target_rule_text | NVARCHAR(300) | N | 없음 |  | 알림 대상 원문 |
| provider_response_deadline_minutes | INT | N | 없음 | CHECK > 0 | 응답 기한 |
| request_field_summary_text | NVARCHAR(1000) | N | 없음 |  | 표시용 요약 |
| required_completion_photo_count | SMALLINT | N | 0 | CHECK >= 0 | Excel 0/2/5 |
| required_qualification_summary_text | NVARCHAR(1000) | N | 없음 |  | 자격 요약 원문 |
| insurance_requirement_text | NVARCHAR(100) | N | 없음 |  | 보험 원문 |
| safety_grade_code | VARCHAR(20) | N | 없음 | CHECK, IX | NORMAL/MEDIUM/HIGH |
| completion_evidence_rule_text | NVARCHAR(1000) | N | 없음 |  | 완료 증빙 원문 |
| default_warranty_days | SMALLINT | N | 0 | CHECK >= 0 | 기본 A/S일 |
| trust_score_display_text | NVARCHAR(100) | N | 없음 |  | 표시 원문 |
| default_sort_code | VARCHAR(30) | N | 없음 |  | CREDIT_DESC |
| service_area_level_code | VARCHAR(20) | N | `SIGUNGU` | CHECK | 매칭 단위 |
| reference_url | NVARCHAR(2048) | N | 없음 |  | 근거 URL |
| admin_note | NVARCHAR(2000) | N | 없음 |  | 관리 메모 |
| effective_from | DATE | N | 없음 | IX | 시작일 |
| effective_to | DATE | Y | NULL | IX | 종료일 exclusive 제안 |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 생성 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | 생성자 |
| updated_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 수정 UTC |
| updated_by_user_id | BIGINT | Y | NULL | FK users.id | 수정자 |
| row_version | ROWVERSION | N | SQL Server | concurrency | 동시성 |

unique/index 후보: `UQ(category_id, policy_version)`, `(category_id, effective_from DESC)`. 유효기간 중첩은 애플리케이션에서 검증한다.

### 8.3 `completion_photo_roles`

- 업무 목적: 완료 증빙 사진 역할을 데이터로 관리하고 향후 역할 확장을 지원
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| code | VARCHAR(50) | N | 없음 | UQ | BEFORE/AFTER/OTHER, 향후 DETAIL/SERIAL/PROCESS 등 |
| name | NVARCHAR(100) | N | 없음 |  | 표시명 |
| description | NVARCHAR(1000) | N | 없음 |  | 역할 정의와 사용 예 |
| is_active | BIT | N | 1 | IX | 신규 정책에서 선택 가능 여부 |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 생성 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | 생성자 |
| updated_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 수정 UTC |
| updated_by_user_id | BIGINT | Y | NULL | FK users.id | 수정자 |
| row_version | ROWVERSION | N | SQL Server | concurrency | 동시성 |

초기 데이터는 BEFORE, AFTER, OTHER다. 역할 추가는 코드 변경이 아니라 승인된 정책 데이터 변경으로 수행한다.

### 8.4 `category_completion_photo_requirements`

- 업무 목적: 카테고리 정책 버전별 완료사진 역할 최소수량 관리
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| category_policy_id | BIGINT | N | 없음 | FK category_policies.id, IX | 적용 정책 버전 |
| photo_role_id | BIGINT | N | 없음 | FK completion_photo_roles.id, IX | 사진 역할 |
| minimum_count | SMALLINT | N | 없음 | CHECK >= 0 | 역할별 최소 제출 수량 |
| display_order | INT | N | 0 | IX 후보 | 안내/검증 표시 순서 |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 생성 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | 생성자 |
| updated_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 수정 UTC |
| updated_by_user_id | BIGINT | Y | NULL | FK users.id | 수정자 |
| row_version | ROWVERSION | N | SQL Server | concurrency | 동시성 |

unique 후보: `(category_policy_id, photo_role_id)`. 정책의 역할별 `minimum_count` 합은 `category_policies.required_completion_photo_count`와 같아야 하며 import/domain validation에서 검증한다. 0장 정책은 요구사항 행을 만들지 않는다.

### 8.5 `category_field_definitions`

- 업무 목적: 동적 요청 입력필드 정의
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| public_id | UNIQUEIDENTIFIER | N | 앱 UUID v4 | UQ | 관리/API 식별 |
| source_field_id | VARCHAR(50) | N | 없음 | UQ | Excel 명시적 필드ID(`FLD-xxxxx`); 행 순서와 무관한 원본 불변 식별자 |
| owner_middle_category_id | BIGINT | N | 없음 | FK service_categories.id, IX | 정의 소유 중분류 |
| field_key | VARCHAR(100) | N | 없음 | IX with owner | Excel 업무 필드 키 원문; 동일 중분류 내 중복 허용, 질문 식별자로 사용하지 않음 |
| label | NVARCHAR(200) | N | 없음 |  | 화면 라벨 |
| field_type_code | VARCHAR(20) | N | 없음 | CHECK, IX | 입력 유형 |
| is_required | BIT | N | 0 | IX | 필수 여부 |
| options_or_unit_text | NVARCHAR(2000) | Y | NULL |  | 선택값/단위 원문 |
| provider_visibility_code | VARCHAR(20) | N | `FULL` | CHECK | FULL/AREA_ONLY |
| pre_accept_masking_code | VARCHAR(30) | N | `NONE` | CHECK | NONE/DETAIL_ADDRESS |
| validation_rule_text | NVARCHAR(1000) | N | 없음 |  | 검증 규칙 원문 |
| display_order | INT | N | 0 | IX | 표시 순서 |
| status_code | VARCHAR(20) | N | `ACTIVE` | CHECK | ACTIVE/INACTIVE |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 생성 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | 생성자 |
| updated_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 수정 UTC |
| updated_by_user_id | BIGINT | Y | NULL | FK users.id | 수정자 |
| row_version | ROWVERSION | N | SQL Server | concurrency | 동시성 |

unique/index: `UQ(source_field_id)`, 비고유 `(owner_middle_category_id, field_key)`, `(owner_middle_category_id, status_code, display_order)`. Excel 837개 원본 행은 `source_field_id`로 개별 식별하며, 동일 중분류의 동일 `field_key` 17쌍도 별개의 정의로 보존한다.

### 8.6 `category_field_assignments`

- 업무 목적: 필드 정의를 중분류 전체 또는 특정 leaf에 적용
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| field_definition_id | BIGINT | N | 없음 | FK category_field_definitions.id, IX | 필드 |
| target_category_id | BIGINT | N | 없음 | FK service_categories.id, IX | MIDDLE 또는 SERVICE |
| scope_code | VARCHAR(20) | N | 없음 | CHECK | MIDDLE/SERVICE |
| is_active | BIT | N | 1 | IX | 현재 적용 여부 |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 생성 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | 생성자 |
| updated_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 수정 UTC |
| updated_by_user_id | BIGINT | Y | NULL | FK users.id | 수정자 |
| row_version | ROWVERSION | N | SQL Server | concurrency | 동시성 |

unique 후보: `(field_definition_id, target_category_id)`.

### 8.7 `fee_policies`

- 업무 목적: Excel의 견적/지원/구독 수수료 정책 원본 카탈로그
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| public_id | UNIQUEIDENTIFIER | N | 앱 UUID v4 | UQ | 운영 식별 |
| code | VARCHAR(50) | N | 없음 | UQ | FEE-Q1, SUB-RATE 등 |
| policy_kind_code | VARCHAR(20) | N | 없음 | CHECK, IX | QUOTE/SUPPORT/PROJECT/SUBSCRIPTION |
| transaction_type_code | VARCHAR(20) | N | 없음 | CHECK, IX | ONE_TIME/PROJECT/SUBSCRIPTION |
| applies_to_text | NVARCHAR(200) | N | 없음 |  | 적용 거래 원문 |
| calculation_method_text | NVARCHAR(100) | Y | NULL |  | 정률/월정액/회차정액/혼합형 |
| min_base_amount | DECIMAL(19,4) | Y | NULL | IX 후보 | 요금 하한 |
| max_base_amount | DECIMAL(19,4) | Y | NULL | IX 후보 | 요금 상한 |
| display_fee_amount | DECIMAL(19,4) | Y | NULL |  | 견적 화면 표시액 |
| rate | DECIMAL(9,6) | Y | NULL | CHECK 0~1 | 구독 비율 |
| monthly_amount | DECIMAL(19,4) | Y | NULL |  | 구독 월 정액 |
| per_visit_amount | DECIMAL(19,4) | Y | NULL |  | 구독 회차 정액 |
| currency_code | CHAR(3) | N | `KRW` | CHECK 후보 | 통화 |
| charge_timing_text | NVARCHAR(300) | N | 없음 |  | 차감/적용 시점 원문 |
| restore_rule_text | NVARCHAR(1000) | Y | NULL |  | 복원 원칙 |
| note | NVARCHAR(1000) | Y | NULL |  | 비고 |
| effective_from | DATE | Y | NULL | IX | 원본에 없으므로 import batch 기준 검토 |
| effective_to | DATE | Y | NULL | IX | 종료일 |
| is_active | BIT | N | 1 | IX | 활성 여부 |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 생성 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | 생성자 |
| updated_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 수정 UTC |
| updated_by_user_id | BIGINT | Y | NULL | FK users.id | 수정자 |
| row_version | ROWVERSION | N | SQL Server | concurrency | 동시성 |

### 8.8 `qualification_policies`

- 업무 목적: 중분류별 전문가 자격·보험·안전 승인 기준
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| source_policy_id | VARCHAR(50) | N | 없음 | UQ | Excel 정책ID |
| middle_category_id | BIGINT | N | 없음 | FK service_categories.id, IX | MIDDLE 노드 |
| identity_verification_rule_text | NVARCHAR(500) | N | 없음 |  | 본인인증 원문 |
| business_registration_rule_text | NVARCHAR(500) | N | 없음 |  | 사업자등록 원문 |
| required_license_text | NVARCHAR(2000) | N | 없음 |  | 자격·면허 원문 |
| insurance_rule_text | NVARCHAR(1000) | N | 없음 |  | 보험 원문 |
| equipment_facility_rule_text | NVARCHAR(1000) | N | 없음 |  | 장비·시설 원문 |
| background_check_rule_text | NVARCHAR(1000) | N | 없음 |  | 신원검증 원문 |
| safety_grade_code | VARCHAR(20) | N | 없음 | CHECK, IX | NORMAL/MEDIUM/HIGH |
| emergency_rule_text | NVARCHAR(1000) | N | 없음 |  | 긴급출동 원문 |
| review_cycle_text | NVARCHAR(500) | N | 없음 |  | 승인주기 원문 |
| admin_checklist_text | NVARCHAR(2000) | N | 없음 |  | 관리자 확인사항 |
| effective_from | DATE | Y | NULL | IX | 적용 시작 |
| effective_to | DATE | Y | NULL | IX | 적용 종료 |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 생성 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | 생성자 |
| updated_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 수정 UTC |
| updated_by_user_id | BIGINT | Y | NULL | FK users.id | 수정자 |
| row_version | ROWVERSION | N | SQL Server | concurrency | 동시성 |

unique 후보: `(middle_category_id, source_policy_id)`.

### 8.9 `administrative_areas`

- 업무 목적: 정확한 SIGUNGU 매칭과 행정구역 변경 이력
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| public_id | UNIQUEIDENTIFIER | N | 앱 UUID v4 | UQ | API 식별 |
| source_system_code | VARCHAR(30) | N | `MOIS_STANDARD_CODE` | IX | 행정안전부 행정표준코드관리시스템 원천 |
| area_code | VARCHAR(20) | N | 없음 | IX, UQ with effective_from | 공식 지역코드 |
| area_name | NVARCHAR(100) | N | 없음 | IX | 공식 지역명 |
| area_level_code | VARCHAR(20) | N | 없음 | CHECK, IX | SIDO/SIGUNGU; 매칭은 SIGUNGU만 사용 |
| parent_area_id | BIGINT | Y | NULL | FK self, IX | SIDO는 NULL, SIGUNGU는 상위 SIDO |
| source_parent_area_code | VARCHAR(20) | Y | NULL | IX | 원천 파일의 상위지역코드 보존 |
| source_created_date | DATE | Y | NULL |  | 원천 생성일 |
| source_abolished_date | DATE | Y | NULL |  | 원천 폐지일 |
| abolition_type_code | VARCHAR(30) | Y | NULL | IX 후보 | 원천 폐지구분 |
| effective_from | DATE | N | 없음 | IX | 유효 시작 |
| effective_to | DATE | Y | NULL | IX | 유효 종료 |
| is_active | BIT | N | 1 | IX | 현재 사용 여부 |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 생성 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | 생성자 |
| updated_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 수정 UTC |
| updated_by_user_id | BIGINT | Y | NULL | FK users.id | 수정자 |
| row_version | ROWVERSION | N | SQL Server | concurrency | 동시성 |

unique/index 후보: `UQ(area_code, effective_from)`, `FIX UNIQUE(area_code) WHERE is_active = 1`, `(parent_area_id, area_level_code, is_active, area_name)`. 폐지 코드는 물리삭제하지 않는다. 구코드→신코드 다대다 매핑은 필요 시 별도 연결 테이블로 확장한다.

## 9. 거래·작업완료

### 9.1 `transactions`

- 업무 목적: 채택된 견적과 고객-전문가 작업의 계약적 연결; 회계 원장은 아님
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| public_id | UNIQUEIDENTIFIER | N | 앱 UUID v4 | UQ | API 공개 ID |
| service_request_id | BIGINT | N | 없음 | FK service_requests.id, UQ | MVP 요청당 거래 최대 1 |
| accepted_quote_revision_id | BIGINT | N | 없음 | FK quote_revisions.id, UQ | 채택 견적 버전 |
| customer_profile_id | BIGINT | N | 없음 | FK customer_profiles.id, IX | 고객 snapshot 대상 |
| provider_profile_id | BIGINT | N | 없음 | FK provider_profiles.id, IX | 전문가 snapshot 대상 |
| category_id | BIGINT | N | 없음 | FK service_categories.id, IX | leaf 카테고리 |
| status_code | VARCHAR(30) | N | `CREATED` | CHECK, IX | 거래 상태 |
| agreed_amount | DECIMAL(19,4) | N | 없음 |  | 채택 총액 |
| currency_code | CHAR(3) | N | `KRW` | CHECK 후보 | 통화 |
| quote_snapshot_json | NVARCHAR(MAX) | N | 없음 | ISJSON CHECK | 채택 revision/items 불변 snapshot |
| category_policy_snapshot_json | NVARCHAR(MAX) | N | 없음 | ISJSON CHECK | 적용 카테고리 정책 |
| completion_policy_snapshot_json | NVARCHAR(MAX) | N | 없음 | ISJSON CHECK | 총수량/역할별 최소수량/증빙 원문/정책버전 |
| warranty_days_snapshot | SMALLINT | N | 0 | CHECK >= 0 | 채택 시 기본 A/S일 |
| provider_trust_score_snapshot | DECIMAL(9,4) | Y | NULL |  | 채택 시 신뢰점수 |
| started_at | DATETIME2(7) | Y | NULL |  | 작업 시작 UTC |
| completed_at | DATETIME2(7) | Y | NULL | IX | 고객 확정 완료 UTC |
| cancelled_at | DATETIME2(7) | Y | NULL |  | 취소 UTC |
| cancellation_reason | NVARCHAR(1000) | Y | NULL |  | 취소 사유 |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() | IX | 채택/생성 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | 생성 actor |
| updated_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 수정 UTC |
| updated_by_user_id | BIGINT | Y | NULL | FK users.id | 수정 actor |
| row_version | ROWVERSION | N | SQL Server | concurrency | 채택/상태 경쟁 제어 |

주요 index: `(customer_profile_id, created_at DESC)`, `(provider_profile_id, status_code, created_at DESC)`.

### 9.2 `work_completions`

- 업무 목적: 거래별 작업완료 aggregate와 현재 상태
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| public_id | UNIQUEIDENTIFIER | N | 앱 UUID v4 | UQ | API 공개 ID |
| transaction_id | BIGINT | N | 없음 | FK transactions.id, UQ | 거래당 하나 |
| status_code | VARCHAR(30) | N | `DRAFT` | CHECK, IX | completion 상태 |
| latest_revision_no | INT | N | 0 |  | 빠른 표시용, revision 생성과 원자 갱신 |
| first_submitted_at | DATETIME2(7) | Y | NULL |  | 최초 제출 UTC |
| confirmed_at | DATETIME2(7) | Y | NULL | IX | 고객 확정 UTC |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 생성 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | 생성자 |
| updated_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 수정 UTC |
| updated_by_user_id | BIGINT | Y | NULL | FK users.id | 수정자 |
| row_version | ROWVERSION | N | SQL Server | concurrency | 상태 동시성 |

### 9.3 `work_completion_revisions`

- 업무 목적: 전문가가 제출한 작업완료 내용의 immutable 버전
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| public_id | UNIQUEIDENTIFIER | N | 앱 UUID v4 | UQ | API revision ID |
| work_completion_id | BIGINT | N | 없음 | FK work_completions.id, IX | 완료 aggregate |
| revision_no | INT | N | 없음 | UQ with completion | 1부터 증가 |
| status_code | VARCHAR(30) | N | `SUBMITTED` | CHECK, IX | revision 상태 |
| work_summary | NVARCHAR(MAX) | N | 없음 |  | 수행 내용 |
| checklist_json | NVARCHAR(MAX) | Y | NULL | ISJSON CHECK | 체크리스트 snapshot |
| provider_attestation_at | DATETIME2(7) | N | 없음 |  | 전문가 전자확인 UTC |
| submitted_at | DATETIME2(7) | N | SYSUTCDATETIME() | IX | 제출 UTC |
| submitted_by_user_id | BIGINT | N | 없음 | FK users.id | 전문가 계정 |
| revision_reason | NVARCHAR(1000) | Y | NULL |  | 수정 제출 사유 |
| idempotency_key | VARCHAR(100) | N | 없음 | UQ | 제출 중복 방지 |

unique/index 후보: `(work_completion_id, revision_no)`, `(work_completion_id, submitted_at DESC)`.

### 9.4 `completion_evidence_files`

- 업무 목적: 완료 revision별 사진/문서 증빙 역할과 순서
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| completion_revision_id | BIGINT | N | 없음 | FK work_completion_revisions.id, IX | 완료 revision |
| file_id | BIGINT | N | 없음 | FK files.id, IX | 파일 |
| photo_role_id | BIGINT | N | 없음 | FK completion_photo_roles.id, IX | BEFORE/AFTER/OTHER 또는 향후 역할 |
| display_order | INT | N | 0 |  | 표시 순서 |
| description | NVARCHAR(500) | Y | NULL |  | 증빙 설명 |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 연결 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | 연결자 |

unique 후보: `(completion_revision_id, file_id)`; 수량 검증 index `(completion_revision_id, photo_role_id)`.

### 9.5 `customer_confirmations`

- 업무 목적: 완료 revision에 대한 고객 확정/수정요청/분쟁 응답의 append-only 기록
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| public_id | UNIQUEIDENTIFIER | N | 앱 UUID v4 | UQ | API 공개 ID |
| transaction_id | BIGINT | N | 없음 | FK transactions.id, IX | 거래 |
| completion_revision_id | BIGINT | N | 없음 | FK work_completion_revisions.id, IX | 응답 대상 revision |
| result_code | VARCHAR(30) | N | 없음 | CHECK, IX | COMPLETED/REVISION_REQUESTED/DISPUTED |
| comment | NVARCHAR(2000) | Y | NULL |  | 수정/분쟁 사유 |
| confirmed_at | DATETIME2(7) | N | SYSUTCDATETIME() | IX | 응답 UTC |
| confirmed_by_user_id | BIGINT | N | 없음 | FK users.id | 고객 계정 |
| idempotency_key | VARCHAR(100) | N | 없음 | UQ | 중복 응답 방지 |

index 후보: `(transaction_id, confirmed_at DESC)`. 거래 완료 응답 최대 1회는 `FIX UNIQUE(transaction_id) WHERE result_code = 'COMPLETED'`.

## 10. 수리/서비스 이력·자산

### 10.1 `service_history_entries`

- 업무 목적: 고객의 완료/A/S/자산 연결 이벤트를 append-only로 보존
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| public_id | UNIQUEIDENTIFIER | N | 앱 UUID v4 | UQ | API 공개 ID |
| customer_profile_id | BIGINT | N | 없음 | FK customer_profiles.id, IX | 이력 소유 고객 |
| transaction_id | BIGINT | Y | NULL | FK transactions.id, IX | 원천 거래 |
| source_completion_revision_id | BIGINT | Y | NULL | FK work_completion_revisions.id, IX | 완료 원천 revision |
| after_service_case_id | BIGINT | Y | NULL | FK after_service_cases.id, IX | A/S 원천; 순환 생성 순서 주의 |
| event_type_code | VARCHAR(40) | N | 없음 | CHECK, IX | 이력 event |
| title | NVARCHAR(200) | N | 없음 |  | 표시 제목 |
| summary | NVARCHAR(2000) | N | 없음 |  | 표시 요약 |
| provider_name_snapshot | NVARCHAR(200) | Y | NULL |  | 당시 전문가 표시명 |
| category_name_snapshot | NVARCHAR(500) | Y | NULL |  | 당시 분류 경로 |
| total_amount_snapshot | DECIMAL(19,4) | Y | NULL |  | 당시 금액 |
| currency_code | CHAR(3) | Y | NULL | CHECK 후보 | 금액 존재 시 통화 |
| completed_at_snapshot | DATETIME2(7) | Y | NULL |  | 완료 시각 |
| warranty_start_date | DATE | Y | NULL | IX 후보 | 보증 시작일 |
| warranty_end_date | DATE | Y | NULL | IX | 보증 종료일 |
| snapshot_json | NVARCHAR(MAX) | N | 없음 | ISJSON CHECK | 과거 재현용 최소 snapshot |
| occurred_at | DATETIME2(7) | N | 없음 | IX | 업무 발생 UTC |
| idempotency_key | VARCHAR(120) | N | 없음 | UQ | 중복 이력 방지 |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 기록 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | actor/시스템 |

주요 index: `(customer_profile_id, occurred_at DESC)`, `(transaction_id, event_type_code)`. `after_service_case_id`는 case 생성 후 event를 기록하는 방향으로 사용하며 상호 cascade는 없다.

### 10.2 `service_history_items`

- 업무 목적: history entry의 항목/비용 요약
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| service_history_entry_id | BIGINT | N | 없음 | FK service_history_entries.id, IX | 이력 |
| line_no | INT | N | 없음 | UQ with entry | 순번 |
| item_name | NVARCHAR(200) | N | 없음 |  | 항목명 |
| description | NVARCHAR(1000) | Y | NULL |  | 설명 |
| quantity | DECIMAL(19,4) | Y | NULL |  | 수량 snapshot |
| unit_text | NVARCHAR(50) | Y | NULL |  | 단위 |
| amount | DECIMAL(19,4) | Y | NULL |  | 금액 snapshot |
| currency_code | CHAR(3) | Y | NULL | CHECK 후보 | 통화 |

unique 후보: `(service_history_entry_id, line_no)`.

### 10.3 `service_assets`

- 업무 목적: 고객 소유 장비/설비/공간 등 서비스 대상
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| public_id | UNIQUEIDENTIFIER | N | 앱 UUID v4 | UQ | API 공개 ID |
| customer_profile_id | BIGINT | N | 없음 | FK customer_profiles.id, IX | 소유 고객 |
| asset_type_code | VARCHAR(50) | N | 없음 | IX | 장비/설비 유형; 기준 확장 가능 |
| name | NVARCHAR(200) | N | 없음 |  | 고객 표시명 |
| manufacturer | NVARCHAR(200) | Y | NULL |  | 제조사 |
| model_name | NVARCHAR(200) | Y | NULL |  | 모델명 |
| serial_number | NVARCHAR(200) | Y | NULL |  | 일련번호, 민감도/암호화 검토 |
| installed_at | DATE | Y | NULL |  | 설치일 |
| attributes_json | NVARCHAR(MAX) | Y | NULL | ISJSON CHECK | 확장 속성 |
| status_code | VARCHAR(20) | N | `ACTIVE` | CHECK, IX | ACTIVE/INACTIVE |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 생성 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | 생성자 |
| updated_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 수정 UTC |
| updated_by_user_id | BIGINT | Y | NULL | FK users.id | 수정자 |
| row_version | ROWVERSION | N | SQL Server | concurrency | 동시성 |

index 후보: `(customer_profile_id, status_code, name)`.

### 10.4 `transaction_asset_links`

- 업무 목적: 거래와 고객 자산 연결 및 정정 이력
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| transaction_id | BIGINT | N | 없음 | FK transactions.id, IX | 거래 |
| service_asset_id | BIGINT | N | 없음 | FK service_assets.id, IX | 자산 |
| source_history_entry_id | BIGINT | Y | NULL | FK service_history_entries.id | 연결 생성 근거 |
| status_code | VARCHAR(20) | N | `ACTIVE` | CHECK, IX | ACTIVE/CORRECTED |
| correction_reason | NVARCHAR(1000) | Y | NULL |  | 정정 사유 |
| linked_at | DATETIME2(7) | N | SYSUTCDATETIME() | IX | 연결 UTC |
| linked_by_user_id | BIGINT | Y | NULL | FK users.id | actor/시스템 |
| corrected_at | DATETIME2(7) | Y | NULL |  | 정정 UTC |
| corrected_by_user_id | BIGINT | Y | NULL | FK users.id | 정정 actor |

index 후보: `(transaction_id, status_code)`, `(service_asset_id, linked_at DESC)`; 활성 중복은 `FIX UNIQUE(transaction_id, service_asset_id) WHERE status_code='ACTIVE'`.

## 11. 최소 A/S

### 11.1 `after_service_cases`

- 업무 목적: 완료 거래에 대한 최소 A/S 접수와 현재 상태
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| public_id | UNIQUEIDENTIFIER | N | 앱 UUID v4 | UQ | API 공개 ID |
| transaction_id | BIGINT | N | 없음 | FK transactions.id, IX | 원 거래 |
| customer_profile_id | BIGINT | N | 없음 | FK customer_profiles.id, IX | 접수 고객 |
| provider_profile_id | BIGINT | N | 없음 | FK provider_profiles.id, IX | 담당 전문가 |
| status_code | VARCHAR(20) | N | `RECEIVED` | CHECK, IX | RECEIVED/IN_PROGRESS/COMPLETED |
| subject | NVARCHAR(200) | N | 없음 |  | 접수 제목 |
| description | NVARCHAR(MAX) | N | 없음 |  | 증상/요청 내용 |
| received_at | DATETIME2(7) | N | SYSUTCDATETIME() | IX | 접수 UTC |
| started_at | DATETIME2(7) | Y | NULL |  | 처리 시작 UTC |
| completed_at | DATETIME2(7) | Y | NULL | IX | 처리 완료 UTC |
| idempotency_key | VARCHAR(100) | N | 없음 | UQ | 접수 중복 방지 |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 생성 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | 고객/관리자 |
| updated_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 수정 UTC |
| updated_by_user_id | BIGINT | Y | NULL | FK users.id | 수정 actor |
| row_version | ROWVERSION | N | SQL Server | concurrency | 상태 경쟁 제어 |

주요 index: `(transaction_id, received_at DESC)`, `(provider_profile_id, status_code, received_at)`.

### 11.2 `after_service_actions`

- 업무 목적: A/S 상태 변경과 처리 메모 append-only 기록
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| after_service_case_id | BIGINT | N | 없음 | FK after_service_cases.id, IX | A/S 건 |
| from_status_code | VARCHAR(20) | Y | NULL | CHECK | 최초 접수는 NULL 가능 |
| to_status_code | VARCHAR(20) | N | 없음 | CHECK, IX | 변경 후 상태 |
| action_note | NVARCHAR(2000) | Y | NULL |  | 처리 메모 |
| occurred_at | DATETIME2(7) | N | SYSUTCDATETIME() | IX | 발생 UTC |
| actor_user_id | BIGINT | Y | NULL | FK users.id, IX | 고객/전문가/관리자/시스템 |
| idempotency_key | VARCHAR(100) | N | 없음 | UQ | 중복 상태변경 방지 |

index 후보: `(after_service_case_id, occurred_at)`.

### 11.3 `after_service_files`

- 업무 목적: A/S 접수/처리 증빙 파일 연결
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| after_service_case_id | BIGINT | N | 없음 | FK after_service_cases.id, IX | A/S 건 |
| after_service_action_id | BIGINT | Y | NULL | FK after_service_actions.id, IX | 특정 처리에 연결 시 |
| file_id | BIGINT | N | 없음 | FK files.id, IX | 파일 |
| role_code | VARCHAR(50) | Y | NULL | IX 후보 | 접수사진/완료증빙 등 확장값 |
| description | NVARCHAR(500) | Y | NULL |  | 설명 |
| created_at | DATETIME2(7) | N | SYSUTCDATETIME() |  | 연결 UTC |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | 연결자 |

unique 후보: `(after_service_case_id, file_id)`.

## 12. 감사·비동기 일관성

### 12.1 `audit_logs`

- 업무 목적: 보안/관리/핵심 상태 변경 감사의 append-only 기록
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| occurred_at | DATETIME2(7) | N | SYSUTCDATETIME() | IX | 발생 UTC |
| actor_user_id | BIGINT | Y | NULL | FK users.id, IX | 시스템은 NULL |
| actor_role_code | VARCHAR(30) | Y | NULL | IX 후보 | 당시 역할 snapshot |
| action_code | VARCHAR(50) | N | 없음 | IX | 감사 action |
| entity_type | VARCHAR(100) | N | 없음 | IX | 대상 종류 |
| entity_public_id | UNIQUEIDENTIFIER | Y | NULL | IX | 대상 공개 ID |
| result_code | VARCHAR(20) | N | `SUCCESS` | IX | SUCCESS/FAILURE |
| correlation_id | UNIQUEIDENTIFIER | Y | NULL | IX | 요청 추적 |
| ip_address | VARCHAR(45) | Y | NULL |  | IPv4/IPv6; 보존정책 대상 |
| user_agent | NVARCHAR(1000) | Y | NULL |  | 길이 제한/민감정보 제거 |
| reason | NVARCHAR(1000) | Y | NULL |  | 관리자 사유 |
| before_json | NVARCHAR(MAX) | Y | NULL | ISJSON CHECK | 허용 필드만, 비밀/직접 PII 제외 |
| after_json | NVARCHAR(MAX) | Y | NULL | ISJSON CHECK | 허용 필드만, 비밀/직접 PII 제외 |
| metadata_json | NVARCHAR(MAX) | Y | NULL | ISJSON CHECK | 최소 메타데이터 |

주요 index: `(entity_type, entity_public_id, occurred_at DESC)`, `(actor_user_id, occurred_at DESC)`, `(correlation_id)`.

### 12.2 `outbox_events`

- 업무 목적: DB 상태 변경과 알림/history 등 후속 처리의 transactional outbox
- PK: `id`

| 컬럼 | 형식 | NULL | 기본값 | FK / Unique / Index | 설명 |
|---|---|---:|---|---|---|
| id | BIGINT IDENTITY(1,1) | N | IDENTITY | PK | 내부 키 |
| public_id | UNIQUEIDENTIFIER | N | 앱 UUID v4 | UQ | 이벤트 ID |
| aggregate_type | VARCHAR(100) | N | 없음 | IX | aggregate 종류 |
| aggregate_public_id | UNIQUEIDENTIFIER | N | 없음 | IX | aggregate 공개 ID |
| event_type | VARCHAR(100) | N | 없음 | IX | 도메인 이벤트 종류 |
| payload_json | NVARCHAR(MAX) | N | 없음 | ISJSON CHECK | 개인정보 최소 payload |
| status_code | VARCHAR(20) | N | `PENDING` | CHECK, IX | PENDING/PROCESSING/PUBLISHED/FAILED |
| occurred_at | DATETIME2(7) | N | SYSUTCDATETIME() | IX | 업무 발생 UTC |
| available_at | DATETIME2(7) | N | SYSUTCDATETIME() | IX | 처리 가능 UTC |
| attempt_count | INT | N | 0 |  | 시도 횟수 |
| last_attempt_at | DATETIME2(7) | Y | NULL |  | 최근 시도 UTC |
| processed_at | DATETIME2(7) | Y | NULL | IX 후보 | 성공 UTC |
| error_message | NVARCHAR(2000) | Y | NULL |  | 비밀/개인정보 제거 오류 |
| idempotency_key | VARCHAR(120) | N | 없음 | UQ | 이벤트 생성 중복 방지 |
| created_by_user_id | BIGINT | Y | NULL | FK users.id | actor/시스템 |
| row_version | ROWVERSION | N | SQL Server | concurrency | worker claim 동시성 |

worker index: `(status_code, available_at, id)`.

## 13. 테이블 목록 요약

총 43개 테이블이다.

| 영역 | 테이블 |
|---|---|
| 계정/역할/승인 | users, roles, user_roles, customer_profiles, provider_profiles, provider_approval_events |
| 카테고리/정책/지역 | service_categories, category_policies, completion_photo_roles, category_completion_photo_requirements, category_field_definitions, category_field_assignments, fee_policies, qualification_policies, administrative_areas |
| 공급 서비스/파일 | provider_service_categories, provider_service_areas, files, provider_documents |
| 요청 | service_requests, request_answers, request_answer_files |
| 매칭/알림 | dispatch_candidates, request_dispatches, notifications, notification_deliveries |
| 견적 | quotes, quote_revisions, quote_items |
| 거래/완료 | transactions, work_completions, work_completion_revisions, completion_evidence_files, customer_confirmations |
| 이력/자산 | service_history_entries, service_history_items, service_assets, transaction_asset_links |
| A/S | after_service_cases, after_service_actions, after_service_files |
| 공통 | audit_logs, outbox_events |
