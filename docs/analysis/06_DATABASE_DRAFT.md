# MS-SQL 데이터베이스 초안

## 1. 확정 물리 기준

- DBMS: MS-SQL
- 내부 PK: BIGINT (`IDENTITY` 사용 여부는 구현 상세)
- 외부 식별자: `public_id UNIQUEIDENTIFIER` UNIQUE
- API JSON: camelCase, DB 컬럼: snake_case
- 업무 시간: UTC 저장, 화면 Asia/Seoul
- 동시성: `rowversion`
- 금액: `DECIMAL(19,4)` 및 통화코드
- 거래 테이블명: `transactions`
- Provider 출장지역: `provider_service_areas`

이 문서는 후보 데이터사전이며 DB 스크립트가 아니다.

## 2. IAM

### `user_accounts`

`id`, `public_id`, `login_id`, `password_hash`, `status_code`, `last_login_at`, 공통 감사컬럼.

### `roles`, `user_roles`

고객/Provider/관리자 역할. 사용자+활성 역할 유일.

### `customer_profiles`

`id`, `user_id`, 최소 고객 프로필.

### `provider_profiles`

`id`, `public_id`, `user_id`, `display_name`, `approval_status`, `activity_status`, `approved_at`, `suspension_reason`.

## 3. 카탈로그·지역

### `service_categories`

`id`, `public_id`, `parent_id`, `category_code`, `major_name`, `middle_name`, `service_name`, `transaction_type`, `request_mode`, `status_code`, `sort_no`, `policy_version`, `effective_from`, `effective_to`, `row_version`.

- 관리대장 코드 유일
- 상태 `ACTIVE/PAUSED/REVIEW`
- 거래유형 `ONE_TIME/SUBSCRIPTION/PROJECT`

### `category_field_definitions`

`id`, `field_code`, `category_id` 또는 적용 중분류, `field_key`, `label`, `input_type`, `is_required`, `options_or_unit_json`, `provider_visibility`, `mask_before_acceptance`, `validation_rule_json`, `policy_version`.

관리대장 837개 정의를 임의 축약하지 않는다.

### `category_policies`

`id`, `category_id`, `quote_validity_hours`, `max_quote_count`, `required_photo_count`, `required_photo_roles_json`, `completion_evidence_rule`, `default_warranty_days`, `fee_policy_code`, `safety_level`, 기타 관리대장 정책, `policy_version`.

`required_photo_count`는 0 이상의 정책값이며 0/2/5 또는 향후 숫자를 코드 제약으로 열거하지 않는다. `required_photo_roles_json`은 관리대장 정책에 역할이 정의된 경우 역할별 최소 개수를 표현할 수 있는 확장 필드다.

수수료·자격·신뢰 필드는 저장하되 MVP 실행범위에 따라 적용 여부를 분리한다.

### `administrative_areas`

`id`, `area_code`, `area_name`, `area_level`, `parent_id`, `status_code`, `effective_from`, `effective_to`.

### `provider_service_categories`

`id`, `provider_id`, `category_id`, `status_code`, 공통컬럼. 활성 중복 금지.

### `provider_service_areas`

`id`, `provider_service_category_id`, `area_id`, `status_code`, `effective_from`, `effective_to`. Provider+카테고리+지역 활성 중복 금지.

## 4. 요청·매칭

### `service_requests`

`id`, `public_id`, `customer_id`, `category_id`, `title`, `description`, `service_area_id`, `address_private_enc`, `desired_at`, `desired_budget`, `status_code`, `published_at`, `quote_deadline_at`, `max_quote_count`, `category_snapshot_json`, `row_version`.

### `request_answers`

`id`, `request_id`, `field_definition_id`, `value_text`, `value_number`, `value_json`, `definition_version`, `confirmed_at`. 요청+필드+버전 유일.

### `request_media`

`id`, `request_id`, `media_file_id`, `sort_no`, `visibility_code`.

### `dispatch_candidates`

`id`, `request_id`, `provider_id`, `status_code`, `category_match`, `area_match`, `eligibility_snapshot_json`, `calculated_at`, `expires_at`. 요청+Provider 후보 유일.

### `request_dispatches`

`id`, `candidate_id`, `request_id`, `provider_id`, `status_code`, `available_at`, `viewed_at`, `responded_at`, `expires_at`. 요청+Provider 활성 배포 유일.

### `notification_events`, `notification_logs`

외부 알림톡은 호출하지 않는다. eventType, recipient, payload/template version, 상태, idempotencyKey를 기록한다.

## 5. 견적

### `quotes`

`id`, `public_id`, `request_id`, `provider_id`, `status_code`, `current_revision_no`, `submitted_at`, `expires_at`, `row_version`.

### `quote_revisions`

`id`, `quote_id`, `revision_no`, `total_amount`, `vat_included`, `service_scope`, `exclusion_terms`, `additional_cost_terms`, `available_at`, `estimated_minutes`, `warranty_days`, `provider_snapshot_json`, `submitted_at`, `idempotency_key`.

quote+revision 유일, idempotencyKey 유일. 채택 후 새 revision 금지.

### `quote_items`

`id`, `quote_revision_id`, `item_type`, `name`, `description`, `quantity`, `unit`, `unit_price`, `amount`, `sort_no`, `is_optional`.

## 6. Transaction·완료

### `transactions`

`id`, `public_id`, `request_id`, `accepted_quote_revision_id`, `customer_id`, `provider_id`, `status_code`, `accepted_at`, `started_at`, `completed_at`, `category_policy_version`, `completion_policy_snapshot_json`, `transaction_snapshot_json`, `row_version`.

- 요청당 활성 Transaction 최대 1개
- 채택 revision과 request/provider 일치
- `completion_policy_snapshot_json`에는 적용 완료증빙 규칙, `requiredPhotoCount`, 선택적 역할별 최소조건을 보존하며 관리대장 변경으로 갱신하지 않음

### `work_completion_revisions`

`id`, `transaction_id`, `revision_no`, `status_code`, `work_summary`, `actual_amount`, `started_at`, `ended_at`, `warranty_days`, `submitted_at`, `confirmed_at`.

Transaction+revision 유일, 최종 `CONFIRMED` 최대 1개.

### `completion_media`

`id`, `completion_revision_id`, `media_file_id`, `role_code`(`BEFORE/AFTER/DETAIL/SERIAL/DAMAGE/OTHER` 등 정책 코드), `sort_no`.

완료 제출 시 애플리케이션 계층이 Transaction의 `completion_policy_snapshot_json`을 기준으로 전체 최소 개수와 역할별 최소 개수를 검증한다. 공통 전·후 사진은 권장값이며 카테고리 정책값 0을 DB 제약으로 2로 올리지 않는다.

### `customer_confirmations`

`id`, `transaction_id`, `completion_revision_id`, `customer_id`, `result_code`, `reason`, `confirmed_at`, `idempotency_key`.

## 7. 서비스 이력·시설·A/S

### `service_history_entries`

`id`, `public_id`, `customer_id`, `provider_id`, `transaction_id`, `asset_id`, `after_service_case_id`, `event_type`, `occurred_at`, `summary_snapshot_json`, `source_revision`, `idempotency_key`.

- idempotencyKey 유일
- append-only
- Transaction+확정 완료차수의 `COMPLETION` 한 건

### `service_history_items`

`id`, `history_id`, `item_type`, `name`, `quantity`, `unit`, `amount`, `warranty_to`, `source_quote_item_id`.

### `service_assets`

`id`, `public_id`, `customer_id`, `asset_type`, `name`, `manufacturer`, `model_no`, `serial_hash`, `installed_at`, `address_id`, `status_code`, `row_version`.

### `transaction_asset_links`

`id`, `transaction_id`, `asset_id`, `asset_snapshot_json`, `status_code`, `linked_at`, `linked_by`, `corrected_at`. 동일 Transaction+asset 활성 연결 유일.

### `after_service_cases`

`id`, `public_id`, `transaction_id`, `customer_id`, `provider_id`, `status_code`, `symptom`, `warranty_start_date`, `warranty_days`, `warranty_to`, `received_at`, `started_at`, `completed_at`, `resolution`, `row_version`.

### `after_service_actions`, `after_service_media`

A/S 상태변경·조치·사진 증빙을 보존한다.

## 8. 파일·감사·이벤트

### `media_files`

`id`, `public_id`, `storage_provider`, `storage_key`, `original_name`, `mime_type`, `size_bytes`, `content_hash`, `owner_user_id`, `access_level`, `created_at`.

저장 인터페이스는 추상화하고 개발환경 `LOCAL` provider를 허용한다.

### `audit_logs`

행위자, 역할, actionCode, entityType/id, before/after, reason, traceId, occurredAt. append-only이며 민감정보 정책은 OI-038 OPEN.

### `outbox_events`

aggregate, eventType, payload, occurredAt, publishedAt, retryCount. 완료이력/A·S 이력 생성의 유실과 중복을 방지한다.

## 9. MVP 제외 테이블

- wallets, wallet_ledger, fee_charges
- payment_confirmations/PG payment
- 구독·회차·월말정산·지급
- 인테리어 프로젝트
- 신뢰점수 계산 이력
- 복잡한 appointments/visits 예약 모델

관리대장 수수료 값은 category policy로 보존하되 위 실행 테이블은 만들지 않는다.

## 10. OPEN 영향

- OI-034: 개인정보별 보존/파기 컬럼·배치
- OI-035: 관리자 권한/MFA/재인증
- OI-036: 인덱스·파티션·복구목표
- OI-037: 만료·보증·무응답 시간 계산
- OI-038: 감사로그 마스킹·보존
