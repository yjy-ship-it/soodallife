# 코드 및 상태 정의

## 1. 저장 전략

- 상태/코드는 사람이 읽을 수 있는 대문자 ASCII `VARCHAR(20~50)`로 저장한다.
- 고정된 상태는 C# enum을 문자열 변환하고 DB CHECK 제약을 함께 둔다. enum ordinal 숫자는 저장하지 않는다.
- Excel의 표시명(한국어)은 import 시 코드로 변환하되 원문이 필요한 정책 설명은 별도 `*_text` 컬럼에 보존한다.
- 역할은 데이터 행(`roles`)으로 관리한다. 상태 전이 자체는 애플리케이션 도메인 서비스가 검증하고 DB는 허용값/unique/FK를 방어한다.
- 아래에 없는 새 값은 임의 추가하지 않고 `OPEN`으로 승인받는다.

## 2. 사용자·공급자

| 그룹 | 코드 | 의미 |
|---|---|---|
| USER_STATUS | ACTIVE | 정상 계정 |
| USER_STATUS | SUSPENDED | 로그인/업무 제한 |
| USER_STATUS | WITHDRAWN | 탈퇴 처리된 계정 |
| ROLE | CUSTOMER | 고객 권한 |
| ROLE | PROVIDER | 공급자 권한 |
| ROLE | ADMIN | 관리자 권한 |
| PROVIDER_APPROVAL | PENDING | 승인 대기 |
| PROVIDER_APPROVAL | APPROVED | 승인 |
| PROVIDER_APPROVAL | REJECTED | 반려 |
| PROVIDER_APPROVAL | SUSPENDED | 승인 정지 |
| PROVIDER_ACTIVITY | ACTIVE | 활동 중 |
| PROVIDER_ACTIVITY | INACTIVE | 활동 중지 |
| APPROVAL_ACTION | APPROVE | 승인 결정 |
| APPROVAL_ACTION | REJECT | 반려 결정 |
| APPROVAL_ACTION | SUSPEND | 정지 결정 |
| APPROVAL_ACTION | RESUME | 승인 상태 복구 |

`USER_STATUS`와 승인 이벤트 action 명칭은 물리 설계 제안이다. 탈퇴 상세 전이는 OI-034 결정 전 최소 상태만 사용한다.

## 3. 카테고리·정책

| 그룹 | 코드 | Excel/의미 |
|---|---|---|
| CATEGORY_LEVEL | MAJOR | 대분류 |
| CATEGORY_LEVEL | MIDDLE | 중분류 |
| CATEGORY_LEVEL | SERVICE | 하위 서비스 |
| CATEGORY_STATUS | ACTIVE | 사용 |
| CATEGORY_STATUS | PAUSED | 중지 |
| CATEGORY_STATUS | REVIEW | 검토 |
| TRANSACTION_TYPE | ONE_TIME | 일회성 견적 |
| TRANSACTION_TYPE | SUBSCRIPTION | 구독정산 |
| TRANSACTION_TYPE | PROJECT | 인테리어 프로젝트 |
| SAFETY_GRADE | NORMAL | 일반 |
| SAFETY_GRADE | MEDIUM | 중 |
| SAFETY_GRADE | HIGH | 고 |
| AREA_LEVEL | SIDO | 시·도 상위지역 |
| AREA_LEVEL | SIGUNGU | 시·군·구 매칭 단위 |
| COMPLETION_PHOTO_ROLE | BEFORE | 작업 전 사진 |
| COMPLETION_PHOTO_ROLE | AFTER | 작업 후 사진 |
| COMPLETION_PHOTO_ROLE | OTHER | 작업과정·상세부위·제품명판·손상부위·자재 등 추가증빙 |
| FIELD_TYPE | LONG_TEXT | 장문 |
| FIELD_TYPE | FILE | 파일 |
| FIELD_TYPE | DATETIME | 일시 |
| FIELD_TYPE | MONEY | 금액 |
| FIELD_TYPE | TEXT | 텍스트 |
| FIELD_TYPE | ADDRESS | 주소 |
| FIELD_TYPE | SELECT | 선택 |
| FIELD_TYPE | NUMBER | 숫자 |
| FIELD_TYPE | PERIOD | 기간 |
| FIELD_TYPE | RECURRENCE | 반복일정 |
| FIELD_SCOPE | MIDDLE | 중분류 전체 적용 |
| FIELD_SCOPE | SERVICE | 특정 하위 서비스 적용 |

## 4. 요청·매칭·배포

| 그룹 | 코드 |
|---|---|
| REQUEST_STATUS | DRAFT, OPEN, ACCEPTED, EXPIRED, CANCELLED |
| CANDIDATE_STATUS | ELIGIBLE, INELIGIBLE, DISPATCHED, EXPIRED |
| DISPATCH_STATUS | AVAILABLE, VIEWED, RESPONDED, EXPIRED |

`hasQuotes`, `quoteCount`, `latestQuoteAt`, `hasActiveTransaction`은 파생값이며 상태 컬럼으로 저장하지 않는다.

## 5. 견적·거래·완료

| 그룹 | 코드 |
|---|---|
| QUOTE_STATUS | DRAFT, SUBMITTED, ACCEPTED, NOT_SELECTED, WITHDRAWN, EXPIRED, INVALIDATED |
| TRANSACTION_STATUS | CREATED, IN_PROGRESS, COMPLETION_SUBMITTED, REVISION_REQUESTED, COMPLETED, DISPUTED, CANCELLED |
| COMPLETION_STATUS | DRAFT, SUBMITTED, REVISION_REQUESTED, SUPERSEDED, CONFIRMED, DISPUTED |
| CONFIRMATION_RESULT | COMPLETED, REVISION_REQUESTED, DISPUTED |

허용 전이는 `docs/analysis/04_STATE_TRANSITIONS.md`를 그대로 따른다. DB CHECK는 값 집합만 제한하고 전이는 애플리케이션과 감사로그가 보장한다.

## 6. 이력·자산·A/S

| 그룹 | 코드 |
|---|---|
| HISTORY_EVENT | COMPLETION, AFTER_SERVICE_RECEIVED, AFTER_SERVICE_STARTED, AFTER_SERVICE_COMPLETED, ASSET_LINKED, ASSET_CORRECTED |
| ASSET_STATUS | ACTIVE, INACTIVE |
| ASSET_LINK_STATUS | ACTIVE, CORRECTED |
| AFTER_SERVICE_STATUS | RECEIVED, IN_PROGRESS, COMPLETED |

`PAYMENT`, `REFUND`, `DISPUTE`, `REVIEW` 이력 이벤트는 현재 예약하지 않고 기능 도입 시 추가한다.

## 7. 파일·알림·공통 처리

| 그룹 | 코드 | 비고 |
|---|---|---|
| FILE_STATUS | PENDING, ACTIVE, QUARANTINED, DELETED | 업로드/검사/논리 파기 상태 |
| FILE_PURPOSE | PROVIDER_DOCUMENT, REQUEST_ANSWER, COMPLETION_EVIDENCE, AFTER_SERVICE | 연결 테이블과 일치 |
| NOTIFICATION_STATUS | PENDING, RECORDED, PROCESSING, SENT, PARTIALLY_FAILED, FAILED | 외부 채널 미연결 시 RECORDED까지 사용 |
| DELIVERY_STATUS | PENDING, SENT, FAILED, SKIPPED | 채널별 시도 |
| CHANNEL | IN_APP, ALIMTALK | ALIMTALK은 공급자 연동 전 비활성 |
| OUTBOX_STATUS | PENDING, PROCESSING, PUBLISHED, FAILED | 재시도 가능한 발행 상태 |

완료 사진 역할은 `completion_photo_roles`의 데이터로 관리한다. 초기 역할은 BEFORE/AFTER/OTHER이며, 향후 DETAIL/SERIAL/PROCESS 등을 데이터로 추가할 수 있으므로 C# enum이나 고정 CHECK 목록으로 제한하지 않는다. 정책별 최소수량은 `category_completion_photo_requirements`에 저장한다.

## 8. 감사 action 예시

`CREATE`, `UPDATE`, `STATUS_CHANGE`, `APPROVE`, `REJECT`, `SUSPEND`, `LOGIN_SUCCESS`, `LOGIN_FAILURE`, `VIEW_SENSITIVE`, `DOWNLOAD_FILE`, `EXPORT`를 문자열로 기록할 수 있다. 이는 상태 enum이 아니라 감사 이벤트 명칭이며, 구체적인 필수 기록 범위와 보존기간은 OI-035/OI-038 결정 대상이다.
