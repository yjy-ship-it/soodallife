SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.administrative_areas', N'U') IS NULL
        THROW 52440, 'V244 apply failed: administrative_areas table is unavailable.', 1;

    DECLARE @Cities table(area_code varchar(20) NOT NULL PRIMARY KEY, area_name nvarchar(100) NOT NULL);
    INSERT INTO @Cities(area_code,area_name) VALUES
      ('4111000000',N'수원시'),('4113000000',N'성남시'),('4117000000',N'안양시'),
      ('4127000000',N'안산시'),('4128000000',N'고양시'),('4146000000',N'용인시'),
      ('4119000000',N'부천시'),('4159000000',N'화성시'),('4311000000',N'청주시'),
      ('4413000000',N'천안시'),('5211000000',N'전주시'),('4711000000',N'포항시'),
      ('4812000000',N'창원시');

    SELECT area.id
      INTO #TargetCities
      FROM dbo.administrative_areas area
      INNER JOIN @Cities city ON city.area_code=area.area_code AND city.area_name=area.area_name
     WHERE area.area_level_code='SIGUNGU';

    IF (SELECT COUNT(*) FROM #TargetCities)<>13
        THROW 52443, 'V244-R5 apply failed: exactly 13 parent city rows must exist before deletion.', 1;

    /* 저장주소 문자열은 보존하고, 폐기할 상위 시와의 선택용 연결만 해제합니다. */
    UPDATE address
       SET address.administrative_area_id=NULL,
           address.updated_at=SYSUTCDATETIME()
      FROM dbo.customer_addresses address
      INNER JOIN #TargetCities target ON target.id=address.administrative_area_id;

    /* 지역 FK가 필수인 미거래 요청은 하위 이력이 없을 때만 삭제합니다. */
    SELECT request.id
      INTO #TargetRequests
      FROM dbo.service_requests request
      INNER JOIN #TargetCities target ON target.id=request.administrative_area_id;

    IF EXISTS (SELECT 1 FROM #TargetRequests)
    BEGIN
        /* 견적·거래·현장 업무가 시작된 요청은 삭제하지 않습니다. */
        IF EXISTS (SELECT 1 FROM dbo.quotes history INNER JOIN #TargetRequests target ON target.id=history.service_request_id)
            THROW 52447, 'V244-R5 deletion stopped: a target request has quote history.', 1;
        IF EXISTS (SELECT 1 FROM dbo.transactions history INNER JOIN #TargetRequests target ON target.id=history.service_request_id)
            THROW 52448, 'V244-R5 deletion stopped: a target request has transaction history.', 1;
        IF EXISTS (SELECT 1 FROM dbo.interior_projects history INNER JOIN #TargetRequests target ON target.id=history.service_request_id)
            THROW 52449, 'V244-R5 deletion stopped: a target request has interior project history.', 1;
        IF EXISTS (SELECT 1 FROM dbo.site_visit_proposals history INNER JOIN #TargetRequests target ON target.id=history.service_request_id)
            THROW 52450, 'V244-R5 deletion stopped: a target request has site visit history.', 1;
        IF EXISTS (SELECT 1 FROM dbo.emergency_responses history INNER JOIN #TargetRequests target ON target.id=history.service_request_id)
            THROW 52451, 'V244-R5 deletion stopped: a target request has emergency response history.', 1;

        /* 독립적으로 보존할 기록은 요청 연결만 해제합니다. */
        UPDATE row SET row.service_request_id=NULL
          FROM dbo.notifications row INNER JOIN #TargetRequests target ON target.id=row.service_request_id;
        UPDATE row SET row.service_request_id=NULL
          FROM dbo.reports row INNER JOIN #TargetRequests target ON target.id=row.service_request_id;
        UPDATE row SET row.converted_service_request_id=NULL
          FROM dbo.help_posts row INNER JOIN #TargetRequests target ON target.id=row.converted_service_request_id;

        /* 요청 작성·자동 매칭 단계에서 생성된 종속 레코드를 정리합니다. */
        DELETE row FROM dbo.service_request_files row INNER JOIN #TargetRequests target ON target.id=row.service_request_id;
        DELETE row FROM dbo.request_answers row INNER JOIN #TargetRequests target ON target.id=row.service_request_id;
        DELETE row FROM dbo.public_activity_events row INNER JOIN #TargetRequests target ON target.id=row.service_request_id;
        DELETE row FROM dbo.dispatch_candidates row INNER JOIN #TargetRequests target ON target.id=row.service_request_id;
        DELETE row FROM dbo.request_dispatches row INNER JOIN #TargetRequests target ON target.id=row.service_request_id;

        DECLARE @RequestReferenceCheck nvarchar(max)=N'';
        SELECT @RequestReferenceCheck=STRING_AGG(
            CAST(N'SELECT TOP (1) N''' + REPLACE(QUOTENAME(OBJECT_SCHEMA_NAME(fkc.parent_object_id))+N'.'+QUOTENAME(OBJECT_NAME(fkc.parent_object_id)),N'''',N'''''') +
                 N''' AS reference_table FROM ' + QUOTENAME(OBJECT_SCHEMA_NAME(fkc.parent_object_id))+N'.'+QUOTENAME(OBJECT_NAME(fkc.parent_object_id)) +
                 N' child INNER JOIN #TargetRequests target ON child.'+QUOTENAME(COL_NAME(fkc.parent_object_id,fkc.parent_column_id))+N'=target.id' AS nvarchar(max)),
            N' UNION ALL ')
          FROM sys.foreign_key_columns fkc
         WHERE fkc.referenced_object_id=OBJECT_ID(N'dbo.service_requests')
           AND COL_NAME(fkc.referenced_object_id,fkc.referenced_column_id)=N'id';

        IF COALESCE(@RequestReferenceCheck,N'')<>N''
        BEGIN
            DECLARE @RequestReferenceResult table(reference_table nvarchar(517));
            INSERT INTO @RequestReferenceResult(reference_table) EXEC sys.sp_executesql @RequestReferenceCheck;
            DECLARE @RequestReferenceTable nvarchar(517)=(SELECT TOP (1) reference_table FROM @RequestReferenceResult);
            IF @RequestReferenceTable IS NOT NULL
            BEGIN
                DECLARE @RequestReferenceError nvarchar(2048)=N'V244-R5 deletion stopped: a target service request has protected or unknown history in '+@RequestReferenceTable+N'.';
                THROW 52446, @RequestReferenceError, 1;
            END;
        END;

        DELETE request
          FROM dbo.service_requests request
          INNER JOIN #TargetRequests target ON target.id=request.id;
    END;

    /* 모든 FK 참조를 동적으로 확인하고, 참조가 하나라도 있으면 연쇄 삭제 없이 중단합니다. */
    DECLARE @ReferenceCheck nvarchar(max)=N'';
    SELECT @ReferenceCheck=STRING_AGG(
        CAST(N'SELECT TOP (1) N''' + REPLACE(QUOTENAME(OBJECT_SCHEMA_NAME(fkc.parent_object_id))+N'.'+QUOTENAME(OBJECT_NAME(fkc.parent_object_id)),N'''',N'''''') +
             N''' AS reference_table FROM ' + QUOTENAME(OBJECT_SCHEMA_NAME(fkc.parent_object_id))+N'.'+QUOTENAME(OBJECT_NAME(fkc.parent_object_id)) +
             N' child INNER JOIN #TargetCities target ON child.'+QUOTENAME(COL_NAME(fkc.parent_object_id,fkc.parent_column_id))+N'=target.id' AS nvarchar(max)),
        N' UNION ALL ')
      FROM sys.foreign_key_columns fkc
      INNER JOIN sys.foreign_keys fk ON fk.object_id=fkc.constraint_object_id
     WHERE fkc.referenced_object_id=OBJECT_ID(N'dbo.administrative_areas')
       AND COL_NAME(fkc.referenced_object_id,fkc.referenced_column_id)=N'id';

    IF COALESCE(@ReferenceCheck,N'')<>N''
    BEGIN
        DECLARE @ReferenceResult table(reference_table nvarchar(517));
        INSERT INTO @ReferenceResult(reference_table) EXEC sys.sp_executesql @ReferenceCheck;
        DECLARE @ReferenceTable nvarchar(517)=(SELECT TOP (1) reference_table FROM @ReferenceResult);
        IF @ReferenceTable IS NOT NULL
        BEGIN
            DECLARE @ReferenceError nvarchar(2048)=N'V244-R5 deletion stopped: a target city is referenced by '+@ReferenceTable+N'.';
            THROW 52444, @ReferenceError, 1;
        END;
    END;

    DELETE area
      FROM dbo.administrative_areas area
      INNER JOIN #TargetCities target ON target.id=area.id;

    IF @@ROWCOUNT<>13
        THROW 52445, 'V244-R5 apply failed: deleted city count was not 13.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
