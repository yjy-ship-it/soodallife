# DB 구현 체크리스트

## A. 구현 착수 게이트

- [x] DBI-001 완료사진 역할별 최소수량 결정을 문서에 반영했다.
- [x] DBI-002 행정안전부 행정표준코드 원천과 변경이력 결정을 문서에 반영했다.
- [x] OPEN 상태의 P0 DB 이슈가 없음을 확인했다.
- [ ] OI-034~OI-038 중 스키마/운영에 직접 영향을 주는 결정을 확인한다.
- [ ] 테이블명, 컬럼명, 공개 ID 대상, 상태 코드 목록을 승인한다.
- [ ] Excel v1.1 파일 hash와 승인된 import 버전을 기록한다.
- [ ] 행정구역 기준 데이터와 버전/변경이력 출처를 승인한다.
- [ ] DB 연결정보는 Secret Manager/환경변수로만 준비한다.

## B. EF Core 모델링

- [ ] SQL Server provider의 .NET 10 호환 버전을 공식 지원표로 확인한다.
- [ ] entity별 `BIGINT IDENTITY(1,1)` PK와 필요한 UUID v4 `public_id`를 구성한다.
- [ ] `row_version`을 concurrency token/value generated on add or update로 구성한다.
- [ ] `decimal(19,4)`, `datetime2(7)`, Unicode/ASCII 길이를 명시한다.
- [ ] 모든 FK의 delete behavior를 `Restrict/NoAction`으로 명시하고 모델 snapshot에서 검증한다.
- [ ] enum은 문자열 conversion과 CHECK constraint를 함께 구성한다.
- [ ] JSON 컬럼에 `ISJSON` CHECK를 구성하고 핵심 관계/금액/상태가 JSON에만 존재하지 않는지 확인한다.
- [ ] filtered unique index의 SQL Server 문법과 EF 생성 SQL을 검토한다.
- [ ] unique index에 필요한 normalized 값과 NULL 의미를 검증한다.
- [ ] `files.storage_key_hash BINARY(32)`가 storage key의 UTF-8 SHA-256으로 함께 저장되고 unique인지 검증한다.
- [ ] `service_categories(parent_id, name)` unique index에 필터가 없고 최상위 이름 중복도 차단하는지 검증한다.
- [ ] 설계에서 DESC로 명시한 인덱스의 열 방향이 Migration SQL과 일치하는지 검증한다.
- [ ] UTC 변환 규칙과 `DateTimeKind.Utc` 방어 로직을 구성한다.

## C. Migration 생성 전 리뷰

- [ ] 생성 예정 Migration의 SQL을 별도 파일로 검토하되 아직 DB에 적용하지 않는다.
- [ ] 모든 PK/FK/unique/check/default/index 이름을 명시적으로 확인한다.
- [ ] cascade path가 0건인지 확인한다.
- [ ] `nvarchar(max)` 사용이 JSON/긴 본문에만 제한되는지 확인한다.
- [ ] 외부 공개 ID unique index가 모두 존재하는지 확인한다.
- [ ] append-only 테이블에 일반 update/delete 경로가 없는지 검토한다.
- [ ] 개인정보/감사 컬럼에 민감값이 복제되지 않는지 검토한다.
- [ ] 예상 최대 크기와 주요 조회 index를 검증한다.

## D. Excel import 구현

- [ ] dry-run parser가 정확한 8개 시트명과 헤더를 검증한다.
- [ ] 6 MAJOR, 82 MIDDLE, 677 SERVICE 계층 결과를 검증한다.
- [ ] 837 field definition/assignment, 82 qualification policy, 13 fee policy를 검증한다.
- [ ] Excel 명시적 `필드ID`가 `FLD-00000` 형식이며 837건 모두 고유한지 검증하고, 행 순서가 아닌 이 값을 `source_field_id`로 사용한다.
- [ ] 동일 중분류의 동일 `field_key` 17쌍을 병합하지 않고 별개 필드 정의로 보존하며, import 2회 실행 시 `source_field_id` 기준으로 중복 생성되지 않는지 검증한다.
- [ ] 완료사진 역할 BEFORE/AFTER/OTHER와 카테고리 정책별 0장/2장/5장 요구사항을 검증한다.
- [ ] fee 시트의 두 번째 헤더 1행과 해설 3행을 데이터에서 제외한다.
- [ ] unknown code, 중복 ID, lookup 실패, 변환 실패 시 전체 import를 중단한다.
- [ ] 원본 행 번호와 source ID를 오류 메시지에 포함한다.
- [ ] OI-039 및 DBI-001에 따라 0장은 역할 행 없음, 2장은 BEFORE 1+AFTER 1, 5장은 BEFORE 1+AFTER 1+OTHER 3으로 적재한다.
- [ ] import 단위 transaction과 재실행 idempotency를 검증한다.
- [ ] import 후 source counts/distributions reconciliation report를 만든다.

## E. 행정구역 별도 import

- [ ] 행정안전부 행정표준코드관리시스템 원천 파일과 기준일을 기록한다.
- [ ] SIDO/SIGUNGU 계층과 상위지역 FK를 검증한다.
- [ ] 원천 생성일, 폐지일, 폐지구분, 상위지역코드를 보존한다.
- [ ] 폐지 코드를 물리삭제하지 않고 유효기간 종료/비활성 처리한다.
- [ ] 기존 요청의 지역 FK가 과거 version을 계속 참조하는지 검증한다.
- [ ] 행정구역 적재/갱신을 schema Migration과 분리한다.

## F. 도메인 무결성 테스트

- [ ] 한 사용자가 CUSTOMER/PROVIDER 다중 역할을 가질 수 있다.
- [ ] 미승인/정지 공급자는 매칭/견적 제출에서 제외된다.
- [ ] 카테고리 + 정확한 SIGUNGU 교집합만 후보가 된다.
- [ ] candidate 생성과 dispatch 기록이 분리된다.
- [ ] 채택 전 상세주소/전화/성명이 공급자 조회에 노출되지 않는다.
- [ ] 요청당 공급자별 논리 견적 1개, revision 다건이 보장된다.
- [ ] 견적 채택 경쟁에서 거래가 최대 1개만 생성된다.
- [ ] quote/completion revision이 update되지 않고 새 revision으로 추가된다.
- [ ] 완료 정책 snapshot의 총수량과 BEFORE/AFTER/OTHER 역할별 최소수량을 만족해야 제출된다.
- [ ] 고객 확정과 COMPLETION history/outbox가 원자적으로 생성된다.
- [ ] 동일 idempotency key 재요청이 중복 history/알림을 만들지 않는다.
- [ ] A/S 상태 전이와 action history가 보존된다.

## G. 보안·운영 검증

- [ ] DB 계정 최소권한과 runtime/deploy 계정 분리를 확인한다.
- [ ] 로그 redaction과 감사로그 민감정보 제외 테스트를 수행한다.
- [ ] 파일은 private storage에 저장되고 signed URL 만료/인가가 적용된다.
- [ ] 백업 암호화, 복구 테스트, RPO/RTO를 OI-036 결정에 맞춰 구성한다.
- [ ] 개인정보 보존/파기 job을 OI-034 결정 후 구현한다.
- [ ] 관리자 MFA/세분 권한/재인증을 OI-035 결정 후 구현한다.
- [ ] 성능 기준 데이터량으로 후보 검색, 견적 목록, history 조회 실행계획을 검증한다.

## H. 명시적 금지사항

- [ ] 설계 승인 전 `SOODAL_LIFE_DEV`에 접속/적용하지 않는다.
- [ ] 설계 승인 전 Migration을 생성하지 않는다.
- [ ] 비밀번호, SQL 주소/계정, API key를 소스나 문서에 넣지 않는다.
- [ ] 구독/PG/정산/GPS/QR/AI 테이블을 현재 MVP에 선행 생성하지 않는다.
- [ ] OI-001~OI-033 및 OI-039의 RESOLVED 결론을 임의 변경하지 않는다.
