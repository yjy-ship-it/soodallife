SOODAL LIFE V165 - 수달 도움방 자체 사진 안전처리

- JPEG·PNG만 허용하고 실제 파일 구조·PNG CRC를 서버에서 확인
- 장당 5MB, 최대 가로·세로 12,000px, 최대 4천만 화소 제한
- JPEG EXIF·GPS·주석·APP 메타데이터 제거
- PNG 텍스트·EXIF 등 불필요한 부가 청크 제거
- 파일 뒤에 덧붙은 데이터 제거 후 안전 사본만 저장
- 업로드 원래 파일명은 저장하지 않고 help-photo-N 형식으로 변경
- 안전 사본은 비공개 저장소에 저장하고 인증된 도움방 회원에게만 제공
- 같은 글의 동일 안전 사본 중복 업로드 차단
- 회원 신고 즉시 사진 공개 보류, 관리자 검수 후 재공개 또는 차단
- 외부 이미지 분석 업체 및 별도 계약 불필요

주의: 음란·폭력 AI 판별, 사진 속 개인정보 OCR, 얼굴·차량번호 자동 모자이크는 포함하지 않습니다.

실행: 관리자 PowerShell에서 D:\SOODALLIFE\Deploy-HelpRoomPhotoSafetyV165.ps1 -ConfirmProductionDeployment
