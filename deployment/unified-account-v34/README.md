# 수달 라이프 통합계정·휴대폰 본인인증·비밀번호 정책 v34

## 포함 범위

- 고객과 공급자가 하나의 아이디·비밀번호를 사용하는 통합계정 구조
- 공급자 신규 가입 시 `CUSTOMER`와 `PROVIDER` 역할 및 고객 프로필을 함께 생성
- 기존 고객 계정에 공급자 역할을 추가하는 등록 흐름
- 기존 공급자 계정에 고객 역할과 고객 프로필을 비파괴 방식으로 보정
- 고객 화면의 `공급자 등록`·`공급자 홈`, 공급자 화면의 `고객 홈` 이동
- 휴대전화 본인인증 필수 Gate와 미연동 상태의 `NOT_INTEGRATED`/Fail Closed 처리
- 휴대전화 번호 자동 하이픈 표시
- 비밀번호: 6자 이상, 영문자·숫자·특수문자 필수, 영문 대문자는 선택
- 운영 약관 문서 표시 및 로그인 상태 홈 안내 문구 개선

## 안전 원칙

- 운영 DB를 reset/delete 하지 않습니다.
- 기존 계정·비밀번호 해시·고객 데이터·공급자 데이터·Wallet/Fee/Trust/Privacy 의미를 변경하지 않습니다.
- 마이그레이션은 누락된 고객 역할과 고객 프로필만 추가합니다.
- 외부 휴대전화 본인인증 업체가 연동되기 전에는 가입 성공을 가짜로 만들지 않습니다.
- PG, 알림톡/SMS/Email/Push, Malware/OCR, GPS/자동 ETA, 전자서명은 기존 `NOT_INTEGRATED` 또는 대기 상태를 유지합니다.

## 서버 적용

관리자 PowerShell에서 압축을 푼 배포 폴더로 이동한 다음 실행합니다.

```powershell
Set-ExecutionPolicy -Scope Process Bypass
& .\Publish-Production.ps1
```

스크립트는 다음 순서로 작업합니다.

1. IIS 구성 백업
2. 현재 API·고객·공급자·관리자 배포 폴더 백업
3. `SOODAL_LIFE_DEV` COPY_ONLY DB 백업 및 CHECKSUM 검증
4. 앱풀 중지
5. 통합계정 보정 마이그레이션 적용
6. API와 세 화면 동기화 배포
7. 앱풀·사이트 시작
8. 역할·프로필 누락 건수 검증

## 배포 후 확인

- `https://api.soodallife.kr/api/v1/system/health` → HTTP 200, `status: ok`
- `https://api.soodallife.kr/api/v1/system/ready` → HTTP 200, `status: ready`
- 고객·공급자·관리자 화면 및 SSL 인증서 정상
- 세 Origin의 CORS preflight 정상
- 기존 고객·공급자 로그인 및 새로고침 후 세션 유지
- 기존 고객의 공급자 등록 화면 진입
- 승인된 공급자의 고객 홈·공급자 홈 이동
- 기존 공급자 누락 고객 역할·프로필 0건
- 비밀번호 `abc1!x` 허용, 문자·숫자·특수문자 누락 및 6자 미만 거부
- 본인인증 외부 연동 전 신규 가입은 `NOT_INTEGRATED`로 안전하게 차단

정책 문서는 `docs` 폴더의 v1.1 문서를 참조합니다.
