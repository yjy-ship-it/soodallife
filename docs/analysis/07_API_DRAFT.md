# REST API 초안

## 1. 공통 규약

- Base path: `/api/v1`
- JSON: camelCase
- 내부 DB PK는 노출하지 않고 UUID `publicId` 사용
- 인증 후 역할+객체관계 검사
- 변경 API는 rowVersion/If-Match 적용
- 채택·완료확정·이력/A·S 사건은 `Idempotency-Key` 적용
- 오류: `businessCode`, `message`, `fieldErrors`, `traceId`
- 목록: cursor pagination

아래 표의 Path는 모두 `/api/v1` 이후 경로다.

## 2. 인증·내 정보

| Method | Path | 권한 | 기능 |
|---|---|---|---|
| POST | `/auth/login` | 공개 | 기본 계정 로그인 |
| POST | `/auth/refresh` | 로그인 | 세션/토큰 갱신 방식에 따라 |
| POST | `/auth/logout` | 로그인 | 로그아웃 |
| GET | `/me` | 로그인 | 계정, 역할, Provider 승인/활성 |

소셜/OIDC/본인인증/MFA는 후속이다.

## 3. 카테고리·동적필드

| Method | Path | 권한 | 기능 |
|---|---|---|---|
| GET | `/categories` | 공통 | `ACTIVE+ONE_TIME` 카테고리 목록 |
| GET | `/categories/{categoryId}` | 공통 | 카테고리와 정책버전 |
| GET | `/categories/{categoryId}/request-fields` | 공통 | 적용 동적필드 스키마·검증규칙 |
| GET | `/admin/categories` | 관리자 | 모든 거래유형/상태 포함 |
| POST | `/admin/categories/import` | 관리자 | 관리대장 반영 방식 확정 시; 임의 카테고리 생성 금지 |
| POST | `/admin/categories` | 카테고리관리 | 승인된 기준값 신규 등록 |
| PATCH | `/admin/categories/{categoryId}` | 카테고리관리 | 마스터 수정+감사 |
| PATCH | `/admin/categories/{categoryId}/status` | 카테고리관리 | `ACTIVE/PAUSED/REVIEW` |
| GET | `/admin/category-fields` | 관리자 | 837개 필드 조회 |
| POST | `/admin/category-fields` | 카테고리관리 | 승인 필드 등록 |
| PATCH | `/admin/category-fields/{fieldId}` | 카테고리관리 | 변경감사·버전 |

## 4. 행정구역·Provider 공급범위

| Method | Path | 권한 | 기능 |
|---|---|---|---|
| GET | `/areas?level=SIGUNGU&parentId=...` | 공통 | 공식 시·군·구 조회 |
| GET | `/providers/me` | Provider | 본인 프로필·승인·활성 |
| GET | `/providers/me/service-categories` | Provider | 공급 카테고리 |
| PUT | `/providers/me/service-categories` | 승인 Provider | 일괄 저장 |
| GET | `/providers/me/service-areas` | Provider | `ProviderServiceArea` 조회 |
| PUT | `/providers/me/service-areas` | 승인 Provider | 카테고리별 시군구 일괄 저장 |
| GET | `/admin/providers` | 관리자 | Provider 검색·상태 |
| POST | `/admin/providers/{providerId}/approve` | 심사관리 | 승인 |
| POST | `/admin/providers/{providerId}/reject` | 심사관리 | 반려 |
| POST | `/admin/providers/{providerId}/suspend` | 권한관리자 | 정지 |

## 5. 요청·후보·배포

| Method | Path | 권한 | 기능 |
|---|---|---|---|
| POST | `/requests` | 고객 | `DRAFT` 생성, 동적응답 포함 가능 |
| GET | `/requests` | 고객 | 본인 요청 목록 |
| GET | `/requests/{requestId}` | 관계자 | 역할별 마스킹 상세 |
| PATCH | `/requests/{requestId}` | 고객 | `DRAFT` 수정 |
| POST | `/requests/{requestId}/publish` | 고객 | 필드 검증, `OPEN`, 후보 이벤트 |
| POST | `/requests/{requestId}/cancel` | 고객/관리자 | 채택 전 취소 |
| POST | `/internal/requests/{requestId}/candidates` | 시스템 | 후보 계산/멱등 저장 |
| GET | `/admin/requests/{requestId}/candidates` | 관리자 | 후보와 근거 조회 |
| POST | `/internal/requests/{requestId}/dispatches` | 시스템/운영 | 후보에서 실제 배포 생성 |
| GET | `/admin/requests/{requestId}/dispatches` | 관리자 | 노출/열람/응답 조회 |
| GET | `/providers/me/matched-requests` | Provider | 본인 배포 요청함 |
| GET | `/providers/me/matched-requests/{requestId}` | Provider | 마스킹 상세 |

실제 카카오 알림톡 API는 없다. 내부 알림 이벤트/로그만 생성한다.

## 6. 견적·채택

| Method | Path | 권한 | 기능 |
|---|---|---|---|
| POST | `/requests/{requestId}/quotes` | 적격 Provider | 견적+첫 revision 제출 |
| GET | `/providers/me/quotes` | Provider | 본인 견적 목록 |
| GET | `/quotes/{quoteId}` | 관계자 | 견적과 revision 조회 |
| POST | `/quotes/{quoteId}/revisions` | Provider | 채택 전 새 revision |
| POST | `/quotes/{quoteId}/withdraw` | Provider | 채택 전 철회 |
| GET | `/requests/{requestId}/quotes` | 요청 고객 | 최신 유효 revision 비교 |
| POST | `/quotes/{quoteId}/accept` | 요청 고객 | 단일 채택+Transaction 생성 |

채택은 수수료, 충전금, 결제 API를 호출하지 않는다.

## 7. Transaction·완료

| Method | Path | 권한 | 기능 |
|---|---|---|---|
| GET | `/transactions` | 고객/Provider | 관계 Transaction 목록 |
| GET | `/transactions/{transactionId}` | 당사자/관리자 | 거래 상세·타임라인 |
| POST | `/transactions/{transactionId}/start` | Provider | `IN_PROGRESS` |
| POST | `/transactions/{transactionId}/completions` | Provider | Transaction 정책 스냅샷의 완료사진 수·역할을 검증하고 완료 revision 제출 |
| GET | `/transactions/{transactionId}/completions` | 당사자 | 차수·증빙 조회 |
| POST | `/transactions/{transactionId}/confirm-completion` | 고객 | 정상/보완/분쟁 |

완료확인 요청 필드: `completionRevisionId`, `result`, `reason`, `rowVersion`.

완료 제출 응답은 적용 `categoryPolicyVersion`, `requiredPhotoCount`, 역할별 요구사항과 검증결과를 제공한다. 서버는 현재 카테고리 정책이 아니라 Transaction에 보존된 완료증빙 정책 스냅샷을 사용하며, 0/2/5와 향후 숫자를 동일한 데이터 규칙으로 처리한다.

## 8. 서비스 이력·시설·제품

| Method | Path | 권한 | 기능 |
|---|---|---|---|
| GET | `/service-history` | 고객 | 본인 전체 이력 |
| GET | `/service-history/{historyId}` | 고객 | 원 거래·증빙 연결 |
| GET | `/providers/me/work-history` | Provider | 본인 수행 이력 |
| GET | `/admin/service-history` | 관리자 | 권한·감사 조회 |
| POST | `/service-assets` | 고객 | 최소 시설·제품 등록 |
| GET | `/service-assets` | 고객 | 본인 자산 목록 |
| GET | `/service-assets/{assetId}` | 소유고객/거래Provider | 권한 범위 상세 |
| GET | `/service-assets/{assetId}/history` | 소유고객/거래Provider | 권한 범위 시간순 이력 |
| POST | `/transactions/{transactionId}/asset-links` | 고객 | 본인 거래-자산 연결 |
| POST | `/transactions/{transactionId}/asset-links/{linkId}/correct` | 고객/관리자 | 정정+감사 |

## 9. 최소 A/S

| Method | Path | 권한 | 기능 |
|---|---|---|---|
| POST | `/transactions/{transactionId}/after-services` | 고객 | A/S 접수 |
| GET | `/after-services` | 고객/Provider | 본인 관계 A/S 목록 |
| GET | `/after-services/{afterServiceId}` | 당사자/관리자 | 상태·증빙·원 거래 |
| POST | `/after-services/{afterServiceId}/start` | 해당 Provider | 진행 시작 |
| POST | `/after-services/{afterServiceId}/complete` | 해당 Provider | 처리결과·증빙 완료 |

보증 시작일은 고객 완료확정일이며 `warrantyDays`로 `warrantyTo`를 계산한다.

## 10. 파일·감사

| Method | Path | 권한 | 기능 |
|---|---|---|---|
| POST | `/files` | 로그인 | 추상화 저장소 업로드 |
| GET | `/files/{fileId}` | 관계자 | 권한 확인 후 파일 |
| GET | `/admin/audit-logs` | 감사권한 | 상태·정정 이력 |

개발환경 storageProvider는 `LOCAL`을 허용한다.

## 11. 오류코드 후보

`CATEGORY_NOT_ACTIVE`, `DYNAMIC_FIELD_REQUIRED`, `PROVIDER_NOT_APPROVED`, `SERVICE_AREA_MISMATCH`, `REQUEST_NOT_OPEN`, `MAX_QUOTES_REACHED`, `QUOTE_EXPIRED`, `QUOTE_REVISION_CONFLICT`, `REQUEST_ALREADY_ACCEPTED`, `TRANSACTION_STATE_CONFLICT`, `COMPLETION_REVISION_CONFLICT`, `COMPLETION_PHOTO_COUNT_NOT_MET`, `COMPLETION_PHOTO_ROLE_NOT_MET`, `ASSET_ACCESS_DENIED`, `AFTER_SERVICE_NOT_ELIGIBLE`, `ROW_VERSION_CONFLICT`.
