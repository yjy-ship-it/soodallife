# Excel 관리대장 Import Mapping

## 1. 분석 대상과 검증 방법

- 파일: `docs/source/수달_라이프_전체_서비스_카테고리_및_수수료_관리대장_v1.2.xlsx`
- 검증 SHA-256: `9043553C1FC4875A6B82BAF4726340A81F0B09E10B33905F085C22314EBA3530`
- 분석 방식: 원본을 수정하지 않고 XLSX OOXML의 workbook, relationship, shared string, worksheet XML을 직접 읽어 시트명, 실제 헤더, 데이터 행, 값 분포를 확인했다.
- 이번 단계에서는 import 코드, SQL, seed, Migration을 만들지 않았다.

## 2. 실제 시트 구조

| 시트 | 실제 헤더 기준 데이터/내용 행 | 실제 컬럼 | DB 처리 |
|---|---:|---|---|
| 안내·요약 | 20 | 항목, 값, 대분류, 중분류 수, 하위 서비스 수, 평균 기본요금, 평균 견적수수료 | 설명/검증용, 직접 import 안 함 |
| 카테고리 마스터 | 677 | 40개 | category hierarchy + category policy import |
| 견적수수료 정책 | 17 | 8개 | 실제 정책 13행만 import; 내부 헤더/해설 4행 제외 |
| 요청 필수필드 | 837 | 12개 | field definition + assignment import |
| 자격·안전 정책 | 82 | 13개 | middle-category qualification policy import |
| 운영 규칙 | 10 | 6개 | 설계/검증 기준, 직접 import 안 함 |
| 코드·설정값 | 13 | 4개 | enum/check/seed 역할 검증, 범용 코드 테이블로 import 안 함 |
| 분류 요약 | 6 | 3개 | import 후 집계 검증용, 직접 import 안 함 |

## 3. `카테고리 마스터` 매핑

### 3.1 식별·계층

| Excel 컬럼 | 대상 테이블.컬럼 | 변환/검증 |
|---|---|---|
| ID | service_categories.source_record_id | 원본 행 식별자 보존, UNIQUE |
| 카테고리코드 | service_categories.external_code | leaf SERVICE 코드, 677개 모두 고유 |
| 대분류 | service_categories.name | MAJOR 노드를 distinct 생성; 상위 코드 생성 안 함 |
| 중분류 | service_categories.name | 해당 MAJOR 아래 MIDDLE 노드를 distinct 생성 |
| 하위 서비스 | service_categories.name | 해당 MIDDLE 아래 SERVICE 노드 생성 |
| 사용여부 | service_categories.status_code | 사용→ACTIVE; PAUSED/REVIEW는 코드 시트 기준 수용 |

계층 import 검증값은 MAJOR 6, MIDDLE 82, SERVICE 677이며 총 노드 수는 765다. `(parent_id, name)` 중복이 없어야 하고 SERVICE `external_code`는 677개여야 한다.

### 3.2 버전 정책

아래 대상은 모두 `category_policies`이며 `category_id`는 같은 행에서 만든 SERVICE 노드를 참조한다.

| Excel 컬럼 | 대상 컬럼 | 변환/비고 |
|---|---|---|
| 거래유형 | transaction_type_code | 일회성 견적→ONE_TIME, 구독정산→SUBSCRIPTION, 인테리어 프로젝트→PROJECT |
| 요청방식 | request_method_text | 원문 보존; 별도 enum은 기준 미확정 |
| 출장서비스 | onsite_requirement_text | 원문 `필수/선택` 보존 |
| 긴급출동 허용 | is_emergency_allowed | 허용→1, 불가→0 |
| 정기구독 허용 | subscription_option_text | 원문 보존; MVP 실행 안 함 |
| 표준 작업단위 | standard_work_unit_text | 원문 보존 |
| 기본요금(원) | base_price_amount | DECIMAL(19,4), KRW |
| 가격방식 | price_method_text | 예약가/견적형 원문 보존 |
| 부가세 표시 | vat_display_rule_text | 원문 보존 |
| 최소 희망예산(원) | minimum_budget_amount | DECIMAL(19,4), KRW |
| 최대 견적수 | max_quote_count | SMALLINT; 실제 전 행 5 |
| 견적 유효시간 | quote_validity_minutes | `72시간` 값 72→4320분, 값 2→120분 |
| 수수료 정책코드 | fee_policy_id | `fee_policies.code` lookup FK |
| 예상 견적수수료(원) | estimated_quote_fee_amount | DECIMAL(19,4), 표시/스냅샷용 |
| 실제 차감시점 | fee_charge_timing_text | 원문 보존; MVP 금융 실행 범위 아님 |
| 수수료 복원조건 | fee_restore_condition_text | 원문 보존 |
| 알림 매칭지역 | matching_area_rule_text | 전 행 서비스 주소의 시·구·군 |
| 알림톡 발송 | notification_target_rule_text | 원문 보존; 외부 연동 미활성 |
| 공급자 응답기한 | provider_response_deadline_minutes | 30분→30, 24시간→1440, 48시간→2880 |
| 요청 필수필드 요약 | request_field_summary_text | 사람이 읽는 원문; 필드 정의의 원장으로 사용하지 않음 |
| 필수사진 수 | required_completion_photo_count | SMALLINT; 0/2/5 |
| 필수 자격·증빙 | required_qualification_summary_text | 원문 보존; 상세는 qualification policy 참조 |
| 보험 확인 | insurance_requirement_text | 원문 보존 |
| 안전등급 | safety_grade_code | 일반→NORMAL, 중→MEDIUM, 고→HIGH |
| 완료 필수증빙 | completion_evidence_rule_text | 원문 보존; 역할별 최소수량으로 추정 변환하지 않음 |
| 기본 A/S일 | default_warranty_days | SMALLINT; 0/3/30 |
| 수달신뢰점수 표시 | trust_score_display_text | 원문 보존 |
| 기본 정렬 | default_sort_code | 수달신뢰점수 높은 순→CREDIT_DESC |
| 출장지역 설정단위 | service_area_level_code | 전국 기초자치단체 시·구·군→SIGUNGU |
| 대표 근거 URL | reference_url | NVARCHAR(2048), 외부 URL 원문 |
| 적용 시작일 | effective_from | DATE |
| 적용 종료일 | effective_to | 빈 값→NULL; 실제 677행 모두 빈 값 |
| 정책버전 | policy_version | 실제 전 행 v1.1 |
| 관리 메모 | admin_note | 원문 보존 |

### 3.3 실제 분포 검증

| 항목 | 실제 분포 |
|---|---|
| 사용여부 | 사용 677 |
| 거래유형 | ONE_TIME 대상 545, PROJECT 87, SUBSCRIPTION 45 |
| 요청방식 | 고객 요청→공급자 견적→고객 채택 632, 공급자 지원→고객 채택 45 |
| 출장서비스 | 필수 617, 선택 60 |
| 긴급출동 | 허용 50, 불가 627 |
| 가격방식 | 예약가 458, 견적형 219 |
| 견적 유효시간 | 72시간 627, 2시간 50 |
| 공급자 응답기한 | 24시간 582, 30분 50, 48시간 45 |
| 보험 확인 | 권장 507, 필수 170 |
| 안전등급 | NORMAL 420, MEDIUM 87, HIGH 170 |
| 기본 A/S일 | 0일 329, 3일 81, 30일 267 |
| 수수료 코드 사용 | FEE-Q1 171, Q2 221, Q3 90, Q4 56, Q6 7, FEE-I1 87, FEE-S1 45 |

## 4. `요청 필수필드` 매핑

| Excel 컬럼 | 대상 테이블.컬럼 | 변환/검증 |
|---|---|---|
| 필드ID | category_field_definitions.source_field_id | Excel의 명시적 `FLD-00000` 형식을 원문 보존. 837개 모두 고유해야 하며 누락·형식오류·중복이면 전체 import 중단. 행 번호·정렬 순서에서 생성하지 않음 |
| 대분류 + 중분류 | owner_middle_category_id | hierarchy의 MIDDLE lookup FK |
| 적용 서비스 | category_field_assignments.target_category_id | 실제 837행 모두 `해당 중분류 전체`; 해당 MIDDLE로 연결 |
| 필드키 | category_field_definitions.field_key | Excel 업무 키 원문 보존. 동일 owner middle 내 중복 허용; import 식별 또는 답변 식별에 사용하지 않음 |
| 화면 라벨 | category_field_definitions.label | NVARCHAR(200) |
| 입력유형 | category_field_definitions.field_type_code | 아래 코드 매핑 |
| 필수여부 | category_field_definitions.is_required | 필수→1, 선택→0 |
| 선택값·단위 | category_field_definitions.options_or_unit_text | 빈 261건은 NULL, 나머지 원문 |
| 공급자 공개 | category_field_definitions.provider_visibility_code | 공개→FULL, 시·구·군까지만→AREA_ONLY |
| 채택 전 마스킹 | category_field_definitions.pre_accept_masking_code | 해당 없음→NONE, 상세주소 마스킹→DETAIL_ADDRESS |
| 검증 규칙 | category_field_definitions.validation_rule_text | 사람이 읽는 원문; 임의 정규식으로 변환하지 않음 |

입력유형 원본 분포: LONG_TEXT 179, FILE 132, DATETIME 115, MONEY 99, TEXT 89, ADDRESS 86, SELECT 84, NUMBER 35, PERIOD 17, RECURRENCE 1. 필수 550/선택 287, 공급자 전체공개 755/시·구·군만 82, 상세주소 마스킹 82건이다. 옵션이 비어 있는 승인 대상 SELECT 18건은 MVP import 시 TEXT로 저장하며 원본 `필드ID`로 추적한다. 공식 선택값이 추가되면 동일 `source_field_id`를 갱신하여 SELECT로 복원한다.

동일 `(대분류, 중분류, 필드키)`가 중복된 원본은 17그룹 34행이며 모두 `budget` 키다. 각 쌍은 라벨·유형·필수여부 또는 검증 규칙이 다른 별개 질문이므로 병합하지 않는다. 837개 정의를 모두 보존하고 `category_field_definitions.id`/`public_id`로 질문과 답변을 식별한다.

## 5. `견적수수료 정책` 매핑

### 5.1 고정 견적/지원 정책 9행

| Excel 컬럼 | fee_policies 컬럼 |
|---|---|
| 정책코드 | code |
| 적용 거래 | applies_to_text + transaction_type_code 변환 |
| 기본요금 하한(원) | min_base_amount |
| 기본요금 상한(원) | max_base_amount |
| 견적 화면 표시액(원) | display_fee_amount |
| 실제 차감 시점 | charge_timing_text |
| 복원 원칙 | restore_rule_text |
| 비고 | note |

실제 정책은 FEE-Q1~FEE-Q7, FEE-S1, FEE-I1이다. `카테고리 마스터`는 이 중 Q1/Q2/Q3/Q4/Q6/S1/I1만 참조하며 Q5/Q7도 정책 원본으로 import한다.

### 5.2 구독 정책 4행

시트 15행은 두 번째 헤더(`구독 정책코드/수수료 방식/기본 수수료율/월 정액/회차 정액/적용 시점`)이므로 데이터가 아니다. SUB-RATE, SUB-MONTH, SUB-VISIT, SUB-MIX 4행은 다음처럼 의미를 바꿔 읽는다.

| 위치상 Excel 컬럼 | fee_policies 컬럼 |
|---|---|
| 정책코드 | code |
| 적용 거래 | calculation_method_text |
| 기본요금 하한(원) | rate (0.15, 0.1 등 비율) |
| 기본요금 상한(원) | monthly_amount |
| 견적 화면 표시액(원) | per_visit_amount |
| 실제 차감 시점 | charge_timing_text |

21~23행의 `정책 해설`, `화면 표시`, `실제 차감`은 설명문이므로 import하지 않는다. 따라서 화면상 내용 행 17개 중 실제 정책은 총 13개다.

## 6. `자격·안전 정책` 매핑

| Excel 컬럼 | qualification_policies 컬럼 |
|---|---|
| 정책ID | source_policy_id |
| 대분류 + 중분류 | middle_category_id lookup FK |
| 본인인증 | identity_verification_rule_text |
| 사업자등록 | business_registration_rule_text |
| 필수 자격·면허 | required_license_text |
| 보험 | insurance_rule_text |
| 장비·시설 | equipment_facility_rule_text |
| 신원검증 | background_check_rule_text |
| 안전등급 | safety_grade_code |
| 긴급출동 | emergency_rule_text |
| 승인주기 | review_cycle_text |
| 관리자 확인사항 | admin_checklist_text |

실제 82행은 6개 대분류/82개 중분류와 1:1로 대응하며 13개 컬럼에 빈 값이 없다.

## 7. 안내·운영·코드·요약 시트

| 시트 | 사용 방법 |
|---|---|
| 안내·요약 | 사용자 안내와 집계 교차검증. 행 구조가 두 표를 나란히 둔 형태라 import 금지 |
| 운영 규칙 | R-001~R-010을 도메인/서비스 검증 기준으로 사용. 범용 규칙 테이블을 만들지 않음 |
| 코드·설정값 | CATEGORY_STATUS, TRANSACTION_TYPE, SAFETY_GRADE, CREDIT_DESC, SIGUNGU 등 enum/CHECK와 표시명 검증 |
| 분류 요약 | import 후 대분류별 중/하위 개수 대조: 집수리 18/111, 인테리어 17/87, 청소·위생 14/69, 제품수리·설치 12/62, 생활서비스 9/303, 정기구독 12/45 |

## 8. OI-039 import 규칙

- `필수사진 수`를 `required_completion_photo_count`에 그대로 import한다.
- `완료 필수증빙`을 `completion_evidence_rule_text`에 원문 그대로 보존한다.
- 300행은 0장/“수행내용, 전자확인”, 290행은 2장/“작업 전·후 사진…”, 87행은 5장/동일 문구다.
- DBI-001 결정에 따라 `completion_photo_roles`에 BEFORE/AFTER/OTHER를 초기 적재한다.
- 0장 정책은 `category_completion_photo_requirements` 행을 만들지 않는다.
- 2장 정책은 BEFORE 1, AFTER 1을 적재한다.
- 5장 정책은 BEFORE 1, AFTER 1, OTHER 3을 적재한다.
- OTHER는 작업과정, 상세부위, 제품명판, 손상부위, 자재 등의 추가증빙이다. 향후 DETAIL/SERIAL/PROCESS 등으로 세분화할 때 기존 정책을 소급 변경하지 않고 새 정책 버전으로 적재한다.
- 거래에는 선택된 정책의 count와 원문 증빙을 immutable JSON으로 snapshot한다.

## 9. 행정구역 별도 import

- 행정구역은 본 Excel의 import 대상이 아니다.
- 공식 원천은 행정안전부 행정표준코드관리시스템(`code.go.kr`)의 법정동/지역코드 자료다.
- 별도 import가 `area_code`, `area_name`, `area_level_code`, `parent_area_id`, `source_created_date`, `source_abolished_date`, `abolition_type_code`, `source_parent_area_code`, `effective_from/to`, `is_active`를 적재한다.
- 공급자 출장지역과 서비스 요청 매칭에는 활성 SIGUNGU만 사용한다.
- 폐지 코드는 update로 덮거나 delete하지 않고 기존 version을 비활성/종료하며, 신규 요청부터 신규 활성 코드를 사용한다.
- 행정구역 초기 적재와 갱신은 EF Core schema Migration과 분리한다.

## 10. Import 사전 검증 조건

1. workbook version/hash 기록 방식을 정한다.
2. 정확한 시트명과 헤더가 위 목록과 일치하지 않으면 전체 import를 중단한다.
3. 6/82/677 hierarchy, 837 fields, 82 qualification rows, 13 fee policies를 검증한다.
4. leaf code, `FLD-00000` 형식의 명시적 field ID, policy code 중복을 거부한다. field key 중복은 허용한다.
5. FK lookup 실패, 알 수 없는 enum, 숫자/날짜 변환 실패를 행 번호와 함께 보고하고 부분 반영하지 않는다.
6. import는 단일 DB 트랜잭션과 dry-run 검증을 지원해야 한다.
7. 역할별 최소수량 합과 `required_completion_photo_count`가 일치하는지 검증한다.
8. 실제 seed 실행은 본 설계 승인 및 Migration 단계 이후에만 수행한다.
