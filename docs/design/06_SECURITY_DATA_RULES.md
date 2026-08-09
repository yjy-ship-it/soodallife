# 보안 및 데이터 처리 원칙

## 1. 비밀정보와 인증정보

- 비밀번호 원문, SQL Server 주소/계정/비밀번호, API key, signing key를 소스·설계문서·DB seed에 기록하지 않는다.
- 비밀번호는 검증된 ASP.NET Core Identity 호환 hasher처럼 salt와 work factor를 포함하는 단방향 포맷만 `users.password_hash`에 저장한다. 자체 암호 알고리즘을 만들지 않는다.
- 연결문자열과 외부 채널 자격증명은 개발자 로컬 Secret Manager 또는 환경변수, 운영 secret store로 주입한다.
- access/refresh token 저장 방식은 인증 구현 선택과 함께 DBI-004에서 결정한다. access token 원문을 DB에 저장하지 않는다.
- 로그인 식별자는 원문과 비교용 normalized 값을 분리하고, 로그에 전체 값을 남기지 않는다.

## 2. 개인정보 분류

| 등급 | 예 | 저장/표시 원칙 |
|---|---|---|
| 고위험 인증정보 | password hash, token verifier | 최소 저장, 애플리케이션 로그/감사 before-after에서 제외 |
| 직접 식별정보 | 이름, 전화, 이메일, 상세주소 | 최소 컬럼, 권한 기반 조회, 채택 전 공급자 비공개 |
| 준식별/위치 | SIGUNGU, 서비스 지역 | 매칭에 필요한 범위만 공개; 상세주소와 분리 |
| 공급자 증빙 | 사업자/자격/보험 문서 | private object storage, 관리자/본인만 접근, 다운로드 감사 |
| 업무 파일 | 요청/완료/A/S 사진 | private 저장, 업무 FK로 권한 판정, 임시 signed URL |
| 운영 데이터 | 상태, 금액, 정책 snapshot | 무결성/감사 대상; 공개 ID를 통해서만 API 식별 |

## 3. 계정·권한

- 권한은 `users → user_roles → roles`로 판정하고 역할 변경 이력을 보존한다.
- 고객은 본인 요청·견적·거래·이력·A/S만, 공급자는 자신에게 배포된 요청과 자신의 견적/거래만 접근한다.
- 공급자는 채택 전 고객 성명·연락처·상세주소를 조회할 수 없다. `request_answers.provider_visibility_code` 및 마스킹 규칙을 서버가 적용한다.
- 관리자는 목적별 권한, MFA, 재인증 정책이 OI-035로 열려 있다. 확정 전 “ADMIN이면 모든 데이터” 방식으로 구현하지 않는다.
- object-level authorization은 URL의 `public_id`를 내부 PK로 해석한 뒤 소유권/배포/역할을 함께 검사한다.

## 4. 민감 컬럼과 암호화

- TLS를 통한 전송 암호화를 필수로 한다.
- 저장소 암호화는 SQL Server TDE/볼륨 암호화와 object storage 암호화를 운영환경에서 적용한다.
- 전화·이메일·상세주소의 애플리케이션 컬럼 암호화 또는 Always Encrypted 적용 범위는 검색/운영 요구와 함께 DBI-003에서 결정한다.
- 암호화하더라도 검색용 normalized 값이나 hash가 재식별 위험을 만들 수 있으므로 생성 근거와 접근권한을 문서화한다.
- `audit_logs.before_json/after_json`에는 비밀번호 hash, token, 파일 URL, 전체 전화/이메일/상세주소를 저장하지 않는다. 필요 시 변경 여부 또는 마스킹된 값만 기록한다.

## 5. 파일 저장

- 파일 바이너리는 SQL Server에 저장하지 않고 private object storage에 둔다. DB에는 opaque storage key, 크기, MIME, hash, 상태만 저장한다.
- 원본 파일명은 사용자 표시용으로 취급해 경로 조합에 사용하지 않는다.
- 업로드는 PENDING → 악성코드/형식 검사 → ACTIVE 순서로 활성화한다. 실패/위험 파일은 QUARANTINED 처리한다.
- 다운로드는 업무 연결 FK와 소유권을 확인한 뒤 짧은 만료의 signed URL 또는 서버 스트림으로 제공한다.
- 사진 EXIF의 GPS 등 불필요한 메타데이터 제거 여부는 개인정보 정책 확정 때 결정한다.
- SHA-256은 무결성/중복 보조값이며 접근권한을 대체하지 않는다.

## 6. 공개 ID와 열거 방지

- 내부 `BIGINT` PK는 API 응답/URL에 노출하지 않는다.
- 외부 식별자는 애플리케이션에서 만든 UUID v4를 저장하고 unique index를 둔다.
- UUID는 비밀 토큰이 아니다. 조회마다 인가를 수행한다.
- 하위 상세행이 독립 API 식별자가 아니면 별도 공개 ID를 두지 않고 부모 공개 ID와 line/revision 번호로 접근한다.

## 7. 로그·감사·추적

- 공급자 승인, 상태 변경, 견적 채택, 완료 확정/수정요청, 개인정보 조회, 파일 다운로드, 관리자 변경은 감사 후보이다.
- `audit_logs`는 append-only이며 actor, action, 대상 public ID, UTC 시각, correlation ID, 결과, 최소 메타데이터를 기록한다.
- 요청/응답 body 전체를 로그에 남기지 않는다. 로그 필터에서 Authorization, Cookie, password, token, phone, email, detail address, file URL을 제거한다.
- 감사로그와 일반 애플리케이션 로그의 보존기간/열람권한은 OI-038 결정사항이다.
- outbox payload에도 최소 식별자만 넣고 개인정보를 복제하지 않는다.

## 8. 데이터 보존·파기

- OI-034가 OPEN이므로 물리 삭제 일정, 탈퇴 후 보존, 법적 보관기간을 이번 설계에서 확정하지 않는다.
- 확정 전에는 업무/이력 데이터의 cascade delete를 금지하고, 상태 변경과 접근 차단으로 보수적으로 운용한다.
- 삭제가 결정되면 FK 순서를 무시한 직접 DELETE 대신 승인된 retention job으로 익명화, 파일 파기, 업무 데이터 보존을 구분한다.
- 백업/복구본의 개인정보 파기 전파 절차도 보존정책에 포함해야 한다.

## 9. 동시성·중복 처리

- 변경 가능한 aggregate에는 `row_version ROWVERSION`을 두고 EF Core concurrency token으로 사용한다.
- 견적 제출, 요청 배포, 채택, 완료 제출, 고객 확인, history 생성, outbox 발행은 idempotency key/unique index를 사용한다.
- SQL transaction과 unique 제약을 최종 방어선으로 삼으며 UI 비활성만으로 중복을 막지 않는다.
- outbox consumer는 at-least-once 전달을 전제로 멱등 처리한다.

## 10. DB 접근

- 애플리케이션 runtime 계정과 Migration/deploy 계정을 분리한다.
- runtime 계정은 필요한 schema의 CRUD만, deploy 계정만 DDL 권한을 갖는다.
- 개발 DB `SOODAL_LIFE_DEV`도 비밀 연결정보를 저장소에 두지 않는다.
- 이번 단계에서는 DB에 접속하지 않았고 SQL 또는 Migration을 실행하지 않았다.
