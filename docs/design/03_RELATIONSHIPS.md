# 관계 및 무결성 정의

## 1. 공통 FK·삭제 원칙

- 모든 FK는 SQL Server `ON DELETE NO ACTION`을 기본값으로 한다.
- 업무, 승인, 견적, 거래, 완료, 이력, A/S, 알림, 감사 데이터는 부모가 존재하는 동안 물리 삭제하지 않는다.
- 개인정보 보존기간이 확정되면 OI-034에 따른 익명화/파기 작업을 별도 애플리케이션 서비스로 수행한다. cascade delete로 대체하지 않는다.
- `created_by_user_id`, `updated_by_user_id`, `actor_user_id`는 시스템 처리 또는 탈퇴 계정 보존을 위해 NULL 가능하며 FK는 `users.id`를 참조한다.
- 자기참조 `service_categories.parent_id`도 `NO ACTION`으로 하여 하위 노드 존재 시 상위 삭제를 막는다.

## 2. 주요 cardinality와 불변식

| 부모 | 자식 | 관계 | DB 제약/보완 |
|---|---|---|---|
| users | user_roles | 1:N | 활성 역할 중복은 filtered unique index |
| users | customer_profiles | 1:0..1 | `customer_profiles.user_id` UNIQUE |
| users | provider_profiles | 1:0..1 | `provider_profiles.user_id` UNIQUE |
| provider_profiles | provider_approval_events | 1:N | append-only, 결정 순서 index |
| service_categories | service_categories | 1:N | 3단계 계층, 필터 없는 `(parent_id, name)` unique로 최상위와 각 부모 아래의 형제 이름 중복 금지 |
| service_categories | category_policies | 1:N | 정책 버전/유효기간 중첩은 서비스 검증 |
| category_policies | category_completion_photo_requirements | 1:N | 정책 버전별 역할 최소수량 |
| completion_photo_roles | category_completion_photo_requirements | 1:N | BEFORE/AFTER/OTHER 및 확장 역할 |
| middle category | category_field_definitions | 1:N | Excel 현재 837행은 모두 중분류에 귀속. `source_field_id`만 원본 식별 unique이며 동일 중분류의 동일 `field_key`를 허용 |
| field definition | category_field_assignments | 1:N | 정의를 중분류 전체 또는 leaf에 적용 |
| provider_profiles | provider_service_categories | 1:N | 활성 `(provider, category)` 중복 금지 |
| provider_service_categories | provider_service_areas | 1:N | 활성 `(provider service, area)` 중복 금지 |
| administrative_areas | administrative_areas | 1:N | SIDO 부모와 SIGUNGU 자식 계층 |
| customer_profiles | service_requests | 1:N | 요청 소유권 FK |
| service_requests | request_answers | 1:N | `(request, field_definition)` unique |
| request_answers | request_answer_files | 1:N | FILE 입력도 정규 FK로 연결 |
| service_requests | dispatch_candidates | 1:N | `(request, provider)` unique |
| service_requests | request_dispatches | 1:N | `(request, provider)` unique |
| request_dispatches | notifications | 1:N | 알림 생성 원인 추적 |
| notifications | notification_deliveries | 1:N | 채널별 시도 append-only |
| service_requests | quotes | 1:N | `(request, provider)` unique; 수정은 revision |
| quotes | quote_revisions | 1:N | `(quote, revision_no)` unique, immutable |
| quote_revisions | quote_items | 1:N | `(revision, line_no)` unique |
| service_requests | transactions | 1:0..1 | MVP `transactions.service_request_id` UNIQUE |
| quote_revisions | transactions | 1:0..1 | 채택 revision 재사용 금지 UNIQUE |
| transactions | work_completions | 1:0..1 | `work_completions.transaction_id` UNIQUE |
| work_completions | work_completion_revisions | 1:N | `(completion, revision_no)` unique |
| completion revision | completion_evidence_files | 1:N | `(revision, file)` unique |
| completion_photo_roles | completion_evidence_files | 1:N | 모든 완료사진에 역할 FK 지정 |
| transaction | customer_confirmations | 1:N | 수정요청 반복 허용; 완료 확정은 최대 1회 |
| transaction | service_history_entries | 1:N | 이벤트별 append-only, idempotency unique |
| service_history_entry | service_history_items | 1:N | 이력 표시용 구조화 항목 |
| customer | service_assets | 1:N | 장비/설비/서비스 대상 |
| transaction + asset | transaction_asset_links | N:M | 활성 연결 중복 금지, 정정은 상태로 보존 |
| transaction | after_service_cases | 1:N | 최소 A/S 접수 구조 |
| after_service_case | after_service_actions | 1:N | 상태 변경/메모 append-only |
| files | 업무별 파일 연결 | 1:N | provider/request/completion/A/S 별 FK 연결 |

## 3. 사용자·역할·승인

1. `users`는 인증 주체다.
2. `user_roles`로 `CUSTOMER`, `PROVIDER`, `ADMIN`을 동시에 부여할 수 있다.
3. 전문가 역할만으로 견적 제출이 가능하지 않다. `provider_profiles.approval_status_code = APPROVED` 및 `activity_status_code = ACTIVE`가 추가로 필요하다.
4. 승인/반려/정지는 `provider_approval_events`에 먼저 append하고 같은 트랜잭션에서 현재 상태를 `provider_profiles`에 반영한다.
5. 관리자별 별도 업무 데이터가 기준문서에 없으므로 `admin_profiles`는 만들지 않는다.

## 4. 카테고리·정책·동적 필드

- `service_categories.level_code`는 `MAJOR`, `MIDDLE`, `SERVICE`다.
- 정책과 실제 요청은 leaf `SERVICE` 카테고리를 참조한다.
- `category_field_definitions.owner_middle_category_id`는 `MIDDLE` 노드만 허용한다.
- `category_field_assignments.target_category_id`는 현재 중분류 전체를 나타내는 `MIDDLE` 노드 1개로 적재한다. 향후 특정 leaf 적용 시 동일 구조를 사용한다.
- 필드 정의의 입력유형, 필수성, 공개/마스킹 규칙은 요청 생성 시 해석하고, `request_answers`에는 당시 정의 식별자와 실제 값을 보존한다.
- `completion_photo_roles`의 초기 데이터는 BEFORE/AFTER/OTHER이며 역할을 코드에 고정하지 않는다.
- `category_completion_photo_requirements`는 정책별 역할 최소수량을 보관한다. 총 0장은 행 없음, 총 2장은 BEFORE 1 + AFTER 1, 총 5장은 BEFORE 1 + AFTER 1 + OTHER 3이다.
- 역할별 최소수량 합은 `category_policies.required_completion_photo_count`와 일치해야 한다.
- 정책 유효기간은 `[effective_from, effective_to)` 반개구간으로 취급하는 것을 제안하나, 날짜 경계의 업무 시간대는 OI-037 결정 전 구현하지 않는다.

## 5. 행정구역

- 공식 원천은 행정안전부 행정표준코드관리시스템(`code.go.kr`)의 법정동/지역코드 자료다.
- `administrative_areas`에는 SIDO/SIGUNGU 계층, 원천 생성일·폐지일·폐지구분·상위지역코드를 보존한다.
- 서비스 매칭은 활성 SIGUNGU만 사용한다. 폐지 코드는 물리삭제하지 않는다.
- `service_requests.administrative_area_id`는 요청 당시 지역 version 행을 계속 참조하여 과거 지역코드를 보존한다.
- 행정구역 import/갱신은 schema Migration과 분리하고, 신규 요청부터 새로운 활성 코드를 적용한다.
- 구코드→신코드가 다대다인 경우 별도 매핑 테이블을 추가할 수 있으며 현재 임의 매핑은 만들지 않는다.

## 6. 요청·매칭·배포

- `dispatch_candidates`는 계산/판정 결과이고 `request_dispatches`는 실제 노출/발송 사실이다. 두 개념을 합치지 않는다.
- 후보 조건은 승인 전문가 + 활성 카테고리 + 활성 SIGUNGU 지역의 교집합이다.
- 후보/배포 시점에는 고객 성명, 전화, 상세주소를 복사하지 않는다. 전문가 화면은 `service_requests.administrative_area_id`와 공개 가능한 답변만 사용한다.
- 요청 1건당 최대 견적 수는 선택된 `category_policies.max_quote_count`의 스냅샷으로 검증한다.

## 7. 견적·거래 원자성

- 견적 본체는 전문가/요청의 논리적 단위이고, 제출 내용은 `quote_revisions`와 `quote_items`에 immutable하게 저장한다.
- 채택 트랜잭션에서 다음을 함께 수행한다: 대상 revision과 상태/유효기간 검증 → `transactions` 생성 → 채택 견적 상태 변경 → 나머지 견적 `NOT_SELECTED` → 요청 `ACCEPTED` → outbox 적재.
- MVP에서는 요청이 `ACCEPTED` 후 재개방되지 않으므로 `transactions.service_request_id`를 전체 UNIQUE로 둔다. 취소 후 재매칭 요구가 생기면 별도 상태정책 결정 후 변경한다.
- `transactions`의 정책/카테고리/전문가 정보는 과거 재현용 스냅샷이며 원본 정책 변경으로 갱신하지 않는다.

## 8. 완료·이력·A/S

- `work_completions`는 현재 상태, `work_completion_revisions`는 제출 버전이다.
- 거래 또는 작업 시작 시 총수량, 역할별 최소수량, 정책 버전을 `transactions.completion_policy_snapshot_json`에 고정한다.
- 완료 제출 가능 여부는 snapshot의 총수량·역할별 최소수량과 현재 revision의 `completion_evidence_files.photo_role_id`별 수량을 비교한다.
- 고객 응답은 `customer_confirmations`에 append한다. 수정요청은 다음 completion revision을 유도하고 이전 revision은 `SUPERSEDED`가 된다.
- 고객 확정과 `service_history_entries.event_type_code = COMPLETION` 생성은 한 트랜잭션이며 idempotency key로 중복을 막는다.
- `service_history_entries`는 거래 완료/A/S 이벤트의 원장이며 update/delete하지 않는다. 정정은 새 이벤트 또는 `transaction_asset_links.status_code = CORRECTED`로 표현한다.
- A/S는 접수 → 진행 → 완료만 포함한다. SLA, 비용, 보증판정, 자동배정은 별도 정책 확정 전 추가하지 않는다.

## 9. 순환 FK 방지

- `quotes.current_revision_id`, `work_completions.current_revision_id`는 두지 않는다. 최신 revision은 `(parent_id, revision_no DESC)` index로 조회한다.
- history가 생성한 asset 연결은 `transaction_asset_links.source_history_entry_id` 단방향 FK로 둔다.
- 감사로그의 대상은 범용 `entity_type/entity_public_id` 문자열로 저장하고 업무 테이블로 FK를 걸지 않는다. 감사로그가 모든 테이블과 순환 의존하는 것을 방지한다.
