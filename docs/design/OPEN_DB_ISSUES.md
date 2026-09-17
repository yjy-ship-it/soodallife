# OPEN DB Issues

## 관리 원칙

- 이 문서는 실제 DB 구현 전에 결정권자의 판단이 필요한 사항만 담는다.
- 기존 `docs/analysis/OPEN_ISSUES.md`의 OI-001~OI-033과 OI-039는 계속 `RESOLVED`이며 변경하지 않는다.
- 기존 OPEN OI-034~OI-038은 그대로 유효하다. 아래 번호는 이를 대체하지 않는 DB 구현 관점의 추가 결정 항목이다.

## RESOLVED — DB 구현 P0

### DBI-001 — RESOLVED — 완료 증빙 사진 정책

- 상태: RESOLVED
- 카테고리별 필수사진 총수량은 Excel 관리대장의 값을 Source of Truth로 사용한다.
- `requiredPhotoCount = 0`: 사진 제출 불필요, 역할 요구사항 없음.
- `requiredPhotoCount = 2`: BEFORE 최소 1장, AFTER 최소 1장.
- `requiredPhotoCount = 5`: BEFORE 최소 1장, AFTER 최소 1장, OTHER 최소 3장.
- OTHER는 작업과정, 상세부위, 제품명판, 손상부위, 자재 등의 추가증빙 용도다.
- 사진 역할은 `completion_photo_roles`, 정책별 최소수량은 `category_completion_photo_requirements`에서 데이터로 관리한다.
- 향후 DETAIL/SERIAL/PROCESS 등의 역할을 데이터로 추가할 수 있으며 기존 거래에는 소급 적용하지 않는다.
- 거래 또는 작업 시작 시 총수량, 역할별 최소수량, 적용 정책 버전을 `transactions.completion_policy_snapshot_json`에 보존한다.

### DBI-002 — RESOLVED — 행정구역 코드

- 상태: RESOLVED
- 공식 원천은 행정안전부 행정표준코드관리시스템(`code.go.kr`)의 법정동/지역코드 자료다.
- 전문가 출장지역과 서비스 매칭 단위는 SIGUNGU다.
- `administrative_areas`는 `area_code`, `area_name`, `area_level_code`, `parent_area_id`, `effective_from`, `effective_to`, `is_active`를 지원한다.
- 원천자료의 시도/시군구 계층, 생성일, 폐지일, 폐지구분, 상위지역코드를 함께 보존한다.
- 폐지 코드는 물리삭제하지 않는다. 기존 요청/거래는 과거 지역 version을 계속 참조하고 신규 요청부터 신규 활성 코드를 사용한다.
- 구코드→신코드 매핑이 필요하면 별도 연결 테이블로 확장할 수 있다.
- 초기 적재와 갱신은 별도 import 작업으로 수행하며 schema Migration과 분리한다.

## P0 — 남은 OPEN 항목

없음.

## P1 — 인증/보안·운영 구현 전에 결정 필요

### DBI-003 개인정보 컬럼 암호화 범위

- 상태: OPEN
- 연결 이슈: OI-034, OI-035, OI-038
- 결정할 것: 전화/이메일/상세주소의 애플리케이션 암호화 또는 Always Encrypted 범위, 검색 필요성, key rotation 주체.

### DBI-004 인증 세션/refresh token 저장 방식

- 상태: OPEN
- 결정할 것: ASP.NET Core Identity 도입 여부, cookie/JWT 방식, refresh token 사용 여부와 폐기/회전 모델.
- 미결정 시 처리: `users.password_hash`까지만 설계하고 token/session 테이블은 만들지 않는다.

### DBI-005 감사로그 필수 이벤트·보존·열람권한

- 상태: OPEN
- 연결 이슈: OI-035, OI-038
- 결정할 것: 필수 action 목록, before/after 허용 필드, 보존기간, 관리자 열람/내보내기 승인 절차.

### DBI-006 개인정보·파일 보존 및 익명화/파기 순서

- 상태: OPEN
- 연결 이슈: OI-034
- 결정할 것: 계정탈퇴, 요청취소, 거래완료, A/S 종료별 보존기간과 파일 파기/업무이력 익명화 기준.

### DBI-007 관리자 세부 권한 모델

- 상태: OPEN
- 연결 이슈: OI-035
- 결정할 것: 승인, 카테고리, 개인정보, 감사, 파일, 분쟁 등 permission 분리 수준과 MFA/재인증 조건.
- 미결정 시 처리: `ADMIN` 역할만 seed 가능하나 전권 API는 구현하지 않는다.

### DBI-008 전문가 승인 문서의 유형·검증 규칙

- 상태: OPEN
- 근거: 자격·안전 시트는 중분류별 요구사항을 설명문으로 제공하지만 업로드 문서 유형 코드, 필수 파일 수, 만료/갱신, 검증 결과 상태를 기계 판독 가능한 값으로 확정하지 않는다.
- 결정할 것: `document_type_code`, 검증 상태, 중분류 정책과 제출 문서의 매핑, 만료 시 승인상태 영향.
- 미결정 시 처리: 파일/연결 스키마만 유지하고 승인 자동판정 규칙은 만들지 않는다.

## P2 — 운영 최적화 전에 결정 가능

### DBI-009 시간 경계와 영업일 캘린더

- 상태: OPEN
- 연결 이슈: OI-037
- 결정할 것: 만료/응답기한/A/S일 계산의 달력일·영업일 여부, 공휴일 기준, Asia/Seoul 경계.

### DBI-010 성능·보관 용량 기준

- 상태: OPEN
- 연결 이슈: OI-036
- 결정할 것: 월 요청/견적/알림/감사/파일 건수, 조회 SLA, DB/파일 백업 RPO·RTO.
- 영향: index include 컬럼, partition/archive 도입 시점, outbox batch 크기.

### DBI-011 Excel 정책 import 운영권한과 승인 흐름

- 상태: OPEN
- 결정할 것: 관리자 직접 업로드 여부, dry-run 결과 승인자, 버전 롤백 방식, 원본 파일 hash 보존 위치.

### DBI-012 카테고리 상위 노드의 외부 코드

- 상태: OPEN
- 근거: Excel은 677개 leaf `카테고리코드`만 제공하고 대/중분류 코드는 제공하지 않는다.
- 결정할 것: 상위 노드도 외부 공개 코드가 필요한지, 필요하면 공식 코드 부여 규칙.
- 미결정 시 처리: 상위는 내부 PK만 사용하고 leaf만 `external_code`를 노출한다.

## 결정 요약

| 우선순위 | 항목 수 | 구현 차단 범위 |
|---|---:|---|
| P0 | 0 | 없음; DBI-001/002 RESOLVED |
| P1 | 6 | 인증, 개인정보, 관리자, 감사, 파기, 전문가 증빙 검증 |
| P2 | 4 | 시간 계산, 성능, import 운영, 상위 공개 코드 |
