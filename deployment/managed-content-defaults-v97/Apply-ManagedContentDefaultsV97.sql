SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.managed_contents', N'U') IS NULL OR OBJECT_ID(N'dbo.managed_content_versions', N'U') IS NULL
        THROW 51000, N'운영 콘텐츠 테이블이 없습니다. API DB 마이그레이션을 먼저 적용해 주세요.', 1;

    DECLARE @Now datetime2(7) = SYSUTCDATETIME();
    DECLARE @Seed TABLE (ContentPublicId uniqueidentifier NOT NULL, VersionPublicId uniqueidentifier NOT NULL, ContentTypeCode varchar(30) NOT NULL, DisplayOrder int NOT NULL, Title nvarchar(300) NOT NULL, BodyText nvarchar(max) NOT NULL);
    INSERT INTO @Seed (ContentPublicId, VersionPublicId, ContentTypeCode, DisplayOrder, Title, BodyText) VALUES
    ('d9700001-0000-4000-8000-000000000001','d9710001-0000-4000-8000-000000000001','SAFETY_GUIDE',10,N'방문 작업 전 안전 확인',N'공급자 방문 전 작업 장소 주변을 정리하고 작업에 방해가 되는 물건을 치워 주세요. 고객은 어린이·고령자·반려동물이 작업 구역에 접근하지 않도록 보호하고, 공급자는 작업 범위와 위험요소를 고객에게 먼저 설명한 뒤 작업을 시작해야 합니다.'),
    ('d9700002-0000-4000-8000-000000000002','d9710002-0000-4000-8000-000000000002','SAFETY_GUIDE',20,N'전기·가스·수도 작업 안전수칙',N'전기·가스·수도 관련 작업은 필요한 자격과 경험을 갖춘 공급자가 수행해야 합니다. 작업 전 차단기·밸브·계량기 위치를 확인하고 필요하면 공급을 차단하세요. 누전, 가스 냄새, 누수 확대 등 즉시 위험한 상황에서는 작업을 중단하고 119 또는 관계기관에 먼저 연락해 주세요.'),
    ('d9700003-0000-4000-8000-000000000003','d9710003-0000-4000-8000-000000000003','SAFETY_GUIDE',30,N'사다리·고소 작업 안전수칙',N'사다리와 고소 작업 장비는 평탄하고 미끄럽지 않은 곳에 설치하고, 작업 구역 아래에는 사람이 지나가지 않도록 표시해야 합니다. 비·강풍 등 기상 상태가 좋지 않거나 안전 장비를 갖추지 못한 경우 작업을 진행하지 마세요.'),
    ('d9700004-0000-4000-8000-000000000004','d9710004-0000-4000-8000-000000000004','SAFETY_GUIDE',40,N'작업 중 사진·개인정보 보호',N'사진에는 작업 확인에 필요한 범위만 담고 주민등록번호, 금융정보, 현관 비밀번호와 가족의 얼굴 등 불필요한 개인정보가 노출되지 않도록 확인해 주세요. 개인정보가 포함된 자료는 채팅이나 첨부파일로 전달하지 않는 것이 안전합니다.'),
    ('d9700005-0000-4000-8000-000000000005','d9710005-0000-4000-8000-000000000005','SAFETY_GUIDE',50,N'작업 완료 후 안전 점검',N'작업이 끝나면 전원·가스·수도 연결 상태, 누수·누전·파손 여부와 작업 주변 정리 상태를 고객과 공급자가 함께 확인해 주세요. 사용 방법과 주의사항, 보증 및 A/S 조건도 확인하고 문제가 있으면 완료 처리 전에 기록을 남겨 주세요.'),
    ('d9700011-0000-4000-8000-000000000011','d9710011-0000-4000-8000-000000000011','CATEGORY_GUIDE',10,N'서비스 요청을 정확하게 작성하는 방법',N'필요한 서비스, 증상, 수량과 규모, 현장 조건, 희망 일정과 지역을 구체적으로 작성하면 더 정확한 견적을 받을 수 있습니다. 사진은 전체 모습과 문제 부분이 함께 보이도록 촬영하되 개인정보가 노출되지 않도록 확인해 주세요.'),
    ('d9700012-0000-4000-8000-000000000012','d9710012-0000-4000-8000-000000000012','CATEGORY_GUIDE',20,N'견적 비교와 공급자 선택 안내',N'견적 금액만 보지 말고 작업 범위, 포함·제외 항목, 자재, 일정, 보증과 A/S 조건, 공급자 승인 상태와 리뷰를 함께 비교해 주세요. 이해되지 않는 내용은 채팅으로 확인하고 합의한 내용은 기록으로 남기는 것이 좋습니다.'),
    ('d9700013-0000-4000-8000-000000000013','d9710013-0000-4000-8000-000000000013','CATEGORY_GUIDE',30,N'방문 작업 전 확인사항',N'방문일시와 주소, 주차·출입 방법, 현장 연락 담당자, 작업 공간과 필요한 전기·수도 사용 가능 여부를 미리 확인해 주세요. 일정이나 작업 범위가 바뀌면 작업 시작 전에 상대방과 다시 합의해야 합니다.'),
    ('d9700014-0000-4000-8000-000000000014','d9710014-0000-4000-8000-000000000014','CATEGORY_GUIDE',40,N'작업 완료 확인과 거래 종료',N'작업 결과가 견적과 합의 내용에 맞는지, 추가 비용이 있다면 사전에 동의했는지 확인해 주세요. 이상이 없으면 완료 처리하고, 문제나 미완료 항목이 있으면 사진과 설명을 남긴 뒤 공급자와 보완 일정을 정해 주세요.'),
    ('d9700015-0000-4000-8000-000000000015','d9710015-0000-4000-8000-000000000015','CATEGORY_GUIDE',50,N'A/S·분쟁 신청 안내',N'완료 후 문제가 발생하면 마이수달의 A/S·분쟁 메뉴에서 원 거래를 검색해 접수할 수 있습니다. 증상, 발생일시, 요청사항과 사진 등 확인 자료를 등록하고 공급자와 처리 일정을 협의하세요. 합의되지 않으면 분쟁 절차를 이용할 수 있습니다.'),
    ('d9700021-0000-4000-8000-000000000021','d9710021-0000-4000-8000-000000000021','PRICE_REFERENCE',10,N'가격 참고자료 이용 안내',N'가격 참고자료는 일반적인 작업 조건을 기준으로 이해를 돕기 위한 정보이며 최종 가격이 아닙니다. 실제 견적은 작업 범위, 현장 상태, 지역, 일정, 자재와 공급자 조건에 따라 달라질 수 있으므로 견적서의 세부 항목을 확인해 주세요.'),
    ('d9700022-0000-4000-8000-000000000022','d9710022-0000-4000-8000-000000000022','PRICE_REFERENCE',20,N'견적 금액에 포함되는 항목',N'견적을 받을 때 인건비, 출장비, 자재비, 장비 사용료, 폐기물 처리비, 부가세와 A/S 조건이 포함되었는지 확인해 주세요. 포함되지 않은 항목은 작업 시작 전에 예상 금액과 산정 기준을 확인하는 것이 좋습니다.'),
    ('d9700023-0000-4000-8000-000000000023','d9710023-0000-4000-8000-000000000023','PRICE_REFERENCE',30,N'추가 비용이 발생할 수 있는 경우',N'현장 확인 후 숨은 손상이나 추가 작업이 발견되거나 고객 요청으로 범위·자재·일정이 바뀌면 추가 비용이 발생할 수 있습니다. 공급자는 추가 작업 전에 사유와 금액을 안내하고 고객 동의를 받은 뒤 진행해야 합니다.'),
    ('d9700024-0000-4000-8000-000000000024','d9710024-0000-4000-8000-000000000024','PRICE_REFERENCE',40,N'출장비·자재비 확인 방법',N'출장비의 적용 지역과 재방문 조건, 자재의 규격·수량·단가와 반품 가능 여부를 견적서에서 확인해 주세요. 고가 자재나 주문 제작품은 발주 전 제품명, 규격, 금액과 취소 조건을 별도로 확인하는 것이 안전합니다.'),
    ('d9700025-0000-4000-8000-000000000025','d9710025-0000-4000-8000-000000000025','PRICE_REFERENCE',50,N'결제·취소·환불 금액 확인',N'서비스 대금은 고객이 선택한 공급자에게 직접 지급합니다. 지급 전 최종 작업 범위와 금액을 확인하고 영수증 등 증빙을 보관해 주세요. 취소·환불 금액은 작업 진행 정도, 이미 사용한 자재와 실제 발생 비용, 관계 법령 및 합의 내용에 따라 달라질 수 있습니다.');

    UPDATE c
       SET c.content_type_code=s.ContentTypeCode,c.audience_type_code='ALL',c.status_code='ACTIVE',c.review_status_code='APPROVED',
           c.display_order=s.DisplayOrder,c.end_at=NULL,c.approved_at=@Now,c.rejection_reason=NULL,c.updated_at=@Now
    FROM dbo.managed_contents c INNER JOIN @Seed s ON s.ContentPublicId=c.public_id;

    UPDATE v
       SET v.title=s.Title,v.body_text=s.BodyText,v.question_text=NULL,v.answer_text=NULL,v.file_id=NULL,v.link_text=NULL,
           v.destination_type_code='NONE',v.destination_value=NULL,v.change_reason=N'기본 콘텐츠 한글 인코딩 복구'
    FROM dbo.managed_content_versions v
    INNER JOIN dbo.managed_contents c ON c.id=v.content_id AND v.version_no=c.current_version_no
    INNER JOIN @Seed s ON s.ContentPublicId=c.public_id;

    INSERT INTO dbo.managed_contents (public_id,content_type_code,audience_type_code,status_code,review_status_code,current_version_no,display_order,start_at,end_at,approved_by_user_id,approved_at,rejection_reason,created_at,created_by_user_id,updated_at,updated_by_user_id)
    SELECT s.ContentPublicId,s.ContentTypeCode,'ALL','ACTIVE','APPROVED',1,s.DisplayOrder,@Now,NULL,NULL,@Now,NULL,@Now,NULL,@Now,NULL
    FROM @Seed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.managed_contents c WHERE c.public_id=s.ContentPublicId)
      AND NOT EXISTS (SELECT 1 FROM dbo.managed_contents c INNER JOIN dbo.managed_content_versions v ON v.content_id=c.id AND v.version_no=c.current_version_no WHERE c.content_type_code=s.ContentTypeCode AND v.title=s.Title);

    INSERT INTO dbo.managed_content_versions (public_id,content_id,version_no,title,body_text,question_text,answer_text,file_id,link_text,destination_type_code,destination_value,change_reason,created_at,created_by_user_id)
    SELECT s.VersionPublicId,c.id,1,s.Title,s.BodyText,NULL,NULL,NULL,NULL,'NONE',NULL,N'운영 기본 콘텐츠 안전 등록',@Now,NULL
    FROM @Seed s INNER JOIN dbo.managed_contents c ON c.public_id=s.ContentPublicId
    WHERE NOT EXISTS (SELECT 1 FROM dbo.managed_content_versions v WHERE v.content_id=c.id AND v.version_no=1);

    IF (SELECT COUNT(*) FROM @Seed s WHERE EXISTS (SELECT 1 FROM dbo.managed_contents c INNER JOIN dbo.managed_content_versions v ON v.content_id=c.id AND v.version_no=c.current_version_no WHERE c.content_type_code=s.ContentTypeCode AND v.title=s.Title)) <> 15
        THROW 51001, N'기본 운영 콘텐츠 15건 검증에 실패했습니다.', 1;

    COMMIT TRANSACTION;
    SELECT c.content_type_code,COUNT(*) AS registered_count FROM dbo.managed_contents c INNER JOIN dbo.managed_content_versions v ON v.content_id=c.id AND v.version_no=c.current_version_no INNER JOIN @Seed s ON s.ContentTypeCode=c.content_type_code AND s.Title=v.title GROUP BY c.content_type_code ORDER BY c.content_type_code;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
