# 전국 행정구역 개발 DB 적재

## 기준 자료

- 제공기관: 국토교통부 국가공간정보센터
- 원천: 행정표준코드관리시스템 법정동 코드
- 파일 기준일: 2026-06-30
- 원본 파일: `source/MOLIT_Beopjeongdong_20260630.csv`
- 원본 SHA-256: `E5657D4B53A16F72E42E9C0D91E84FF2D647E70DC56B9408DA6FD057D0DA45C3`
- 공공데이터포털: https://www.data.go.kr/tcs/dss/selectFileDataDetailView.do?publicDataPk=15063424

## 서비스 지역 범위

- 활성 시·도: 16개
- 원본의 시·군·구 레벨 행: 269개
- 자치시 하위 일반구: 39개 제외
- 공급자 출장지역으로 사용하는 서비스 지역: 230개

230개는 기초자치단체를 기본으로 하며, 전국 서비스 공백을 방지하기 위해 기초자치단체가 없는 세종특별자치시와 제주특별자치도의 제주시·서귀포시를 포함한다. 수원시장안구처럼 자치시 하위의 일반구는 공급자 선택 단위에서 제외한다.

## 안전 원칙

- 기존 행정구역 행을 삭제하지 않는다.
- 같은 활성 공식 코드는 ID와 PublicId를 유지한 채 갱신한다.
- 기존 개발용 가상 지역 3개는 이력과 FK를 보존하고 비활성화한다.
- 지역을 아직 선택하지 않은 요청의 `administrative_area_id=NULL`은 정상 상태로 허용하고, 값이 있는 FK의 실제 손상만 오류로 판정한다.
- 예상 개수, 부모 FK, 활성 코드 중복을 한 트랜잭션 안에서 검증하며 실패하면 전체 롤백한다.
- 운영 DB가 아닌 `SOODAL_LIFE_DEV`에만 실행한다.

## 생성

```powershell
& .\Generate-AdministrativeAreaSql.ps1
```

## 실행 전 필수 확인

1. `SOODAL_LIFE_DEV` 전체 백업과 `RESTORE VERIFYONLY`를 완료한다.
2. SQL 파일 SHA-256을 기록한다.
3. 현재 활성 지역 수와 참조 건수를 조회한다.
4. `sqlcmd`의 `-d SOODAL_LIFE_DEV`를 다시 확인한다.
5. 한글 보존을 위해 입력 코드페이지 옵션 `-f 65001`을 사용한다.

## 서버 실행

압축을 푼 폴더에서 관리자 PowerShell로 다음을 실행한다. 이 스크립트는 대상 DB를 `SOODAL_LIFE_DEV`로 고정하고, 백업과 `RESTORE VERIFYONLY`가 성공한 뒤에만 적재를 시작한다.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Apply-DevelopmentDatabase.ps1
```

## 기능 검증

1. DB에서 활성 `SIDO=16`, `SIGUNGU=230`, 개발용 활성 지역 `0`을 확인한다.
2. 모든 활성 SIGUNGU가 활성 SIDO 부모를 가지는지 확인한다.
3. 로그인 후 `GET /api/v1/administrative-areas/sidos`가 16개를 반환하는지 확인한다.
4. 각 시·도별 `GET /api/v1/administrative-areas/sigungu?parentId=...` 합계가 230인지 확인한다.
5. 공급자 화면 `/provider/areas`에서 시·도 선택, 시·군·구 체크, 저장, 새로고침 후 유지 여부를 확인한다.
6. 테스트 서비스 요청 지역과 공급자 출장지역이 같을 때만 후보가 되는지 확인한다.
