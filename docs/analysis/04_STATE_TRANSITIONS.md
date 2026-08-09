# 상태값 및 상태전이

## 1. 공통 규칙

- 아래 상태는 1차 개발 확정 기준이다.
- 계산 가능한 “견적도착”, “결정대기”는 요청 영속상태가 아니라 파생 표시다.
- 전이는 서버가 권한·현재상태·rowVersion을 검증한 뒤 수행한다.
- 상태변경은 이전/이후값, 행위자, 시각, 사유를 감사한다.
- API/JSON에는 정해진 영문 코드를 camelCase 필드로 전달한다.

## 2. 카테고리

관리대장 `코드·설정값`의 실제 코드 사용:

| 상태 | 의미 | 사용자 노출 |
|---|---|---|
| `ACTIVE` | 사용, 신규 요청 노출 | `ONE_TIME`이면 MVP 노출 |
| `PAUSED` | 중지, 신규 노출 중지 | 비노출 |
| `REVIEW` | 검토, 관리자만 조회 | 비노출 |

전이: 관리자가 사유와 함께 상태 변경. 과거 Transaction은 카테고리 스냅샷으로 계속 조회한다.

## 3. Provider

### 승인 상태

`PENDING → APPROVED` 또는 `PENDING → REJECTED`; 관리자는 `APPROVED → SUSPENDED` 가능.

### 활동 상태

`ACTIVE` 또는 `INACTIVE`.

매칭 적격식: `approvalStatus = APPROVED AND activityStatus = ACTIVE`.

상세 자격·보험·본인인증 검증은 후속이므로 MVP 적격식에 강제하지 않는다.

## 4. ProviderServiceArea

| 상태 | 의미 |
|---|---|
| `ACTIVE` | 신규 후보 계산에 사용 |
| `INACTIVE` | 신규 후보 제외, 과거 기록 유지 |

Provider+카테고리+시군구의 활성 중복을 허용하지 않는다.

## 5. ServiceRequest

영속 핵심 상태:

| 상태 | 진입 조건 | 허용 전이 |
|---|---|---|
| `DRAFT` | 고객 임시저장 | `OPEN`, `CANCELLED` |
| `OPEN` | 필수 동적필드 검증 성공·공개 | `ACCEPTED`, `EXPIRED`, `CANCELLED` |
| `ACCEPTED` | 유효 견적 단일 채택과 Transaction 생성 | 종료 |
| `EXPIRED` | 마감 경과, 활성 Transaction 없음 | 종료; 재공개 후속 |
| `CANCELLED` | 채택 전 고객/관리자 취소 | 종료 |

파생 표시:

- `hasQuotes`: 유효 제출 견적 존재
- `decisionPending`: `OPEN`이고 유효 견적 존재
- `noCandidates`: 유효 후보 0명
- `quoteCount`: 유효 견적 수

Transient 검증중/모집준비는 명령 처리/로그로만 남긴다.

## 6. DispatchCandidate

| 상태 | 의미 | 전이 |
|---|---|---|
| `ELIGIBLE` | 매칭 조건 통과 | `DISPATCHED`, `EXPIRED` |
| `INELIGIBLE` | 재검증에서 부적격 | 종료 |
| `DISPATCHED` | 별도 RequestDispatch 생성 | 종료 |
| `EXPIRED` | 요청 종료/마감 | 종료 |

## 7. RequestDispatch

| 상태 | 의미 | 전이 |
|---|---|---|
| `AVAILABLE` | Provider 요청함 노출 | `VIEWED`, `RESPONDED`, `EXPIRED` |
| `VIEWED` | Provider 상세 열람 | `RESPONDED`, `EXPIRED` |
| `RESPONDED` | 견적 제출 또는 거절 | 종료 |
| `EXPIRED` | 요청 채택/취소/만료 | 종료 |

외부 알림톡 상태는 MVP 업무상태가 아니다. 알림 이벤트/로그에 `PENDING/RECORDED/FAILED`만 둘 수 있다.

## 8. Quote와 revision

| 상태 | 의미 | 허용 전이 |
|---|---|---|
| `DRAFT` | Provider 작성중 | `SUBMITTED`, `WITHDRAWN` |
| `SUBMITTED` | 고객 비교 가능 | 새 revision의 `SUBMITTED`, `ACCEPTED`, `NOT_SELECTED`, `WITHDRAWN`, `EXPIRED`, `INVALIDATED` |
| `ACCEPTED` | 유일 채택 견적 | 종료 |
| `NOT_SELECTED` | 다른 견적 채택 | 종료 |
| `WITHDRAWN` | 채택 전 Provider 철회 | 종료 |
| `EXPIRED` | 견적/요청 마감 | 종료 |
| `INVALIDATED` | Provider 상태 등으로 무효 | 종료 |

규칙:

- 채택 전까지만 수정 가능하다.
- 수정 시 기존 revision을 덮어쓰지 않는다.
- 요청+Provider당 최신 활성 견적은 하나다.
- 요청당 `ACCEPTED`는 하나다.

## 9. Transaction

복잡한 예약상태는 없다.

| 상태 | 진입 조건 | 허용 전이 |
|---|---|---|
| `CREATED` | 견적 채택과 원자 생성 | `IN_PROGRESS`, `CANCELLED`, `DISPUTED` |
| `IN_PROGRESS` | Provider 작업 시작 | `COMPLETION_SUBMITTED`, `DISPUTED` |
| `COMPLETION_SUBMITTED` | 완료자료 제출 | `COMPLETED`, `REVISION_REQUESTED`, `DISPUTED` |
| `REVISION_REQUESTED` | 고객 보완요청 | `IN_PROGRESS`, `COMPLETION_SUBMITTED`, `DISPUTED` |
| `COMPLETED` | 고객 정상완료 확정 | 종료; A/S 별도 사건 가능 |
| `DISPUTED` | 고객 분쟁 선택 | MVP에서는 상태·증빙 보존 |
| `CANCELLED` | 허용된 취소 | 종료 |

고객 무응답 자동완료는 정의하지 않는다. 운영환경 전 OI-037 시간정책과 함께 결정한다.

## 10. WorkCompletion

| 상태 | 의미 |
|---|---|
| `DRAFT` | Provider 작성중 |
| `SUBMITTED` | 고객 확인대기 |
| `REVISION_REQUESTED` | 보완요청됨 |
| `SUPERSEDED` | 새 revision으로 대체 |
| `CONFIRMED` | 고객 정상완료가 참조한 최종차수 |
| `DISPUTED` | 분쟁 증빙으로 보존 |

Transaction당 여러 revision을 허용하지만 `CONFIRMED`는 하나다.

`DRAFT → SUBMITTED` 조건:

- 작업내용과 실제 작업금액이 존재한다.
- 서버가 Transaction의 카테고리와 고정된 완료증빙 정책버전/스냅샷을 조회한다.
- 첨부 사진 수가 스냅샷의 `requiredPhotoCount` 이상이다. 0이면 사진 없이 전이할 수 있고, 2와 5를 포함한 모든 숫자는 데이터로 평가한다.
- 정책에 사진별 역할이 정의되어 있으면 `BEFORE/AFTER/DETAIL/SERIAL/DAMAGE` 등의 역할별 최소조건도 충족해야 한다.
- 현재 카테고리 정책이 변경되었더라도 기존 Transaction의 스냅샷을 소급 갱신하지 않는다.

## 11. CustomerConfirmation

| 결과 | Transaction 상태 | 이력 생성 |
|---|---|---|
| `COMPLETED` | `COMPLETED` | `COMPLETION` 사건 생성 |
| `REVISION_REQUESTED` | `REVISION_REQUESTED` | 생성하지 않음 |
| `DISPUTED` | `DISPUTED` | 완료 사건 생성 보류 |

## 12. ServiceHistoryEntry

append-only 사건 유형:

| eventType | 트리거 | MVP |
|---|---|---|
| `COMPLETION` | 고객 완료확정 | 포함 |
| `AFTER_SERVICE_RECEIVED` | A/S 접수 | 포함 |
| `AFTER_SERVICE_STARTED` | A/S 진행 시작 | 포함 |
| `AFTER_SERVICE_COMPLETED` | A/S 완료 | 포함 |
| `ASSET_LINKED` | 거래-시설·제품 연결 | 포함 |
| `ASSET_LINK_CORRECTED` | 연결 정정 | 포함 |
| `REVISIT/REPLACEMENT/INSPECTION` | 고도화 | 후속 |

`COMPLETION` 멱등키는 Transaction+확정 completion revision을 식별해야 한다.

## 13. ServiceAsset·TransactionAssetLink

- `ServiceAsset`: `ACTIVE/INACTIVE`
- `TransactionAssetLink`: `ACTIVE/CORRECTED`
- 동일 Transaction+asset 활성 연결은 하나다.
- 정정은 기존 연결을 물리삭제하지 않는다.

## 14. AfterServiceCase

| 상태 | 의미 | 전이 |
|---|---|---|
| `RECEIVED` | 고객 접수 | `IN_PROGRESS` |
| `IN_PROGRESS` | Provider 처리/재방문 | `COMPLETED` |
| `COMPLETED` | 처리결과·증빙 등록 | 종료 |

보증 시작일은 고객 완료확정일이다. `warrantyDays`는 채택/완료 스냅샷 값을 사용한다. 보증 범위 밖 유상 추가작업은 새 요청/Transaction으로 처리한다.

## 15. 검증 체크리스트

- 종료 요청에 새 견적이 생기지 않는가
- 후보 없이 배포가 생성되지 않는가
- Transaction 없는 완료·이력·A/S가 존재하지 않는가
- 고객 확인 전 `COMPLETION` 이력이 생성되지 않는가
- 완료 제출이 Transaction 정책 스냅샷의 동적 사진 개수·역할 조건을 위반하지 않는가
- revision 재시도에 이력이 중복되지 않는가
- 카테고리/지역 비활성화가 과거 조회를 깨지 않는가
