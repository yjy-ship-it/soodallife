# 실제 구현 권장 순서

## 0. 착수 자료 확보

- Excel 관리대장 v1.1의 승인된 import/동기화 방식
- 공식 시·군·구 데이터 파일과 갱신 Source
- 개발/테스트 계정 및 Provider 승인 샘플
- API/ERD 최종 리뷰 승인

## 1. 공통 기반

- MS-SQL BIGINT/publicId/rowversion 공통 규칙
- 기본 계정 로그인과 역할+객체 권한
- 오류·감사·outbox·멱등성
- 파일저장 추상화와 개발환경 LOCAL 구현

## 2. 관리대장 카탈로그

- ServiceCategory, CategoryFieldDefinition, CategoryPolicy
- 677개 카테고리와 837개 필드 검증 import
- 카테고리별 완료증빙, `requiredPhotoCount`, 선택적 사진 역할 정책 import
- `ACTIVE/PAUSED/REVIEW`, 거래유형 필터
- 실제 행 수·코드 유일성·정책버전 대사

완료 기준: 임시 카테고리 없이 `ACTIVE+ONE_TIME`만 공개 조회.

## 3. Provider·시군구

- Provider 최소 프로필·관리자 승인/활성
- 공식 시군구
- ProviderServiceCategory/ProviderServiceArea

완료 기준: 승인+활성 Provider만 공급범위 저장/매칭.

## 4. 고객 요청·동적필드

- DRAFT/OPEN
- 카테고리별 스키마 렌더링 계약
- 서버 필수·유형·마스킹 검증
- 카테고리별 견적시간/최대견적 정책

## 5. 후보·배포·알림로그

- DispatchCandidate 계산
- RequestDispatch 별도 생성
- Provider 요청함
- 외부 전송 없는 NotificationEvent/Log

완료 기준: 지역 불일치 노출 0, 재처리 중복 0.

## 6. 견적 revision·비교

- Quote/QuoteRevision/QuoteItem
- 채택 전 수정과 이력
- 고객 비교, “신규·평가중” 표시

## 7. 채택·Transaction

- 단일 채택과 Transaction 원자 생성
- 나머지 견적 종료
- 채택 후 민감정보 최소공개

완료 기준: 동시채택 한 건, 수수료/결제 의존 없음.

## 8. 작업·완료확정

- IN_PROGRESS
- completion revision, 실제금액, 작업내용, 증빙
- Transaction에 적용 완료증빙 정책버전/스냅샷 고정
- 정책값 기반 최소 사진 수와 선택적 역할별 필수조건 서버 검증
- 고객 정상/보완/분쟁

사진 0/2/5를 분기문으로 고정하지 않고 향후 숫자와 역할 정책을 데이터로 처리한다. 공통 전·후 사진은 카테고리 정책을 덮어쓰지 않는다.

## 9. ServiceHistoryEntry

- 완료확정 outbox
- 멱등 이력 영속 생성
- 고객/Provider/관리자 조회

## 10. 시설·제품

- 최소 등록·조회
- TransactionAssetLink
- 자산별 이력과 Provider 제한조회
- 정정감사

## 11. 최소 A/S

- 접수→진행→완료
- 고객 확정일+warrantyDays
- 접수/진행/완료 이력 사건
- 보증 재방문과 새 유상거래 분리

## 12. 안정화·인수

필수 E2E:

1. 관리대장 카테고리/동적필드 대사
2. 기본 정상 폐쇄루프
3. 지역 불일치·후보 0명
4. 견적 revision/철회/만료
5. 동시 채택
6. 채택 전 개인정보 비노출
7. 완료 보완 revision
8. 이력 재처리 멱등성
9. 시설 연결·권한·정정
10. A/S 상태·보증·이력
11. 카테고리 PAUSED 후 과거 거래 조회
12. `requiredPhotoCount` 0/2/5/기타 값과 역할별 증빙 검증
13. 관리대장 정책변경 후 기존 Transaction 완료조건 불변

## 13. 이후 순서

1. OI-034~038 운영·보안 결정 반영
2. 분쟁 전체 운영, 후기, A/S 고도화
3. 수수료·충전금, 신뢰점수
4. AI
5. 정기구독·GPS/QR·PG·월말정산
6. 인테리어
7. 웹 안정화 후 모바일 앱
