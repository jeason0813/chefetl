SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

RAISERROR('-------------------------------------------------------Procedure CHEF.GetMetaData ---------------------------------------------------------------------',0,1) WITH NOWAIT;
DECLARE @DBName sysname = DB_NAME()
RAISERROR('Dropping and Creating Stored procedure CHEF.GetMetaData in Database %s',0,1,@DBName) WITH NOWAIT
IF OBJECT_ID('CHEF.GetMetaData','P') IS NOT NULL
BEGIN
	RAISERROR('Dropping stored procedure CHEF.GetMetaData',0,1) WITH NOWAIT
	DROP PROCEDURE CHEF.GetMetaData
	RAISERROR('Dropped stored procedure CHEF.GetMetaData',0,1) WITH NOWAIT
END
RAISERROR('Creating stored procedure CHEF.GetMetaData',0,1) WITH NOWAIT
GO
/* ==========================================================================================
	File		: CHEF.GetMetaData.sql
	Name		: CHEF.GetMetaData
	Author		: ramsingh
	Date		: 18th-Aug-2010
	Description	: Return the metadata for the following
				  0. Active Request in the RequestQueue table
				  1. Global Config
				  2. XSD 
	
	Unit Test:
		1.	EXEC CHEF.GetMetaData 0
			
		2.  EXEC CHEF.GetMetaData 1

		3.  EXEC CHEF.GetMetaData 2

	Verify:
		1.	select * from CHEF.RequestQueue
		2. 
			
	Note:
		1. No try catch as it is expected to throw error and crash which would be handled by the calling method
		2. At any given time there will be only one Request with Status=Started, still used TOP & ORDER By to ensure that only one row is returned under any circumstances
		
	Change History
	-------- --------- -----------------------------------------------------------------------
	Date		Name            Description
	-------- --------- -----------------------------------------------------------------------
	21-Sep-2010	balajim		Changed the SP by removing the input params, to read the Queue data from RequestQueue table and return results.
	12-Oct-2010 ramsingh	Added column LineageID
	13-Oct-2010 balajim		Added FiscalMonth and FiscalYear
	08-May-2013 ramsingh	Added the functionality for running the ETL for SPECIFIC/Exculed/END steps and queuing next process
========================================================================================== */

CREATE PROCEDURE [CHEF].[GetMetaData]
	@MetadataTypeID tinyint = 0	--Default: 0 = Process
AS

IF @MetadataTypeID = 0
	BEGIN
		DECLARE @Metadata XML 
		DECLARE @RequestMetadata XML(CHEF.RequestSteps) 
		DECLARE @ProcessID INT 
		DECLARE @UsageType VARCHAR(40)  

		SELECT TOP 1 
			 @Metadata = MD.Metadata
			,@ProcessID = MD.ProcessID
			,@RequestMetadata=RQ.ProcessOption
		FROM   chef.MetaData MD 
				INNER JOIN chef.RequestQueue RQ 
						ON MD.ProcessID = RQ.ProcessID 
		WHERE  RQ.RequestStatus = 1  
		IF(CAST(@RequestMetadata as varchar(max))!='')
		BEGIN
			IF Object_id('tempdb..#tmpRequest') IS NOT NULL 
			  DROP TABLE #tmprequest  

			SELECT r.value('@ID', 'int') AS PROCESSID, 
				   steps.value('@UsageType', 'varchar(40)') 
				   USAGETYPE, 
				   stepid.value('.', 'int') STEPID 
			INTO   #tmprequest 
			FROM   @RequestMetadata.nodes('/Request/Process') AS X(r) 
				   CROSS apply r.nodes('Steps')B(steps) 
				   CROSS apply steps.nodes('StepID')C(stepid) 

			SELECT @UsageType = UsageType FROM   #tmprequest 	WHERE  ProcessID = @ProcessID 

			DECLARE @i INT;  
			IF(@UsageType='EXCLUDE')
				BEGIN
					DECLARE Steps_Cursor CURSOR FOR
						SELECT R.value('@ID','int') AllSteps FROM @Metadata.nodes('CHEFMetaData/Process/Step') AS x(R)
						WHERE R.value('@ID','int') IN (SELECT StepID FROM #tmpRequest WHERE ProcessID=@ProcessID)
					OPEN Steps_Cursor;
					FETCH NEXT FROM Steps_Cursor INTO @i;
					WHILE @@FETCH_STATUS = 0
						BEGIN
							SET @MetaData.modify('delete (CHEFMetaData/Process/Step[@ID=sql:variable("@i")])') 
						 FETCH NEXT FROM Steps_Cursor INTO @i;
						END
					CLOSE Steps_Cursor;
					DEALLOCATE Steps_Cursor;
				END
			ELSE IF(@UsageType='END')
				BEGIN
					DECLARE Steps_Cursor CURSOR FOR
						SELECT R.value('@ID','int') AllSteps FROM @Metadata.nodes('CHEFMetaData/Process/Step') AS x(R)
						WHERE R.value('@ID','int') > (SELECT StepID FROM #tmpRequest WHERE ProcessID=@ProcessID)
					OPEN Steps_Cursor;
					FETCH NEXT FROM Steps_Cursor INTO @i;
					WHILE @@FETCH_STATUS = 0
						BEGIN
							SET @MetaData.modify('delete (CHEFMetaData/Process/Step[@ID=sql:variable("@i")])') 
						 FETCH NEXT FROM Steps_Cursor INTO @i;
						END
					CLOSE Steps_Cursor;
					DEALLOCATE Steps_Cursor;
				END
			ELSE IF(@UsageType='SPECIFIC')
				BEGIN
					DECLARE Steps_Cursor CURSOR FOR
					SELECT R.value('@ID','int') AllSteps FROM @Metadata.nodes('CHEFMetaData/Process/Step') AS x(R) 
					WHERE R.value('@ID','int') NOT IN (SELECT StepID FROM #tmpRequest WHERE ProcessID=@ProcessID)
					OPEN Steps_Cursor;
					FETCH NEXT FROM Steps_Cursor INTO @i;
					WHILE @@FETCH_STATUS = 0
						BEGIN
							SET @MetaData.modify('delete (CHEFMetaData/Process/Step[@ID=sql:variable("@i")])') 
						 FETCH NEXT FROM Steps_Cursor INTO @i;
						END
					CLOSE Steps_Cursor;
					DEALLOCATE Steps_Cursor;
				END
		END

		SELECT TOP 1
				 md.ProcessID
				,md.ProcessName
				,rq.CalendarMonth
				,rq.CalendarYear
				,rq.StartStepID
				,t.ProcessTypeID
				,t.ProcessTypeName
				,mt.MetadataTypeID
				,mt.MetadataTypeName
				,@Metadata as MetaData
				,rq.LineageID
			FROM CHEF.MetaData md 
	  INNER JOIN CHEF.RequestQueue rq
			  ON md.ProcessID = rq.ProcessID
	  INNER JOIN CHEF.vwProcessSteps ps
			  ON rq.ProcessID = ps.ProcessID
			 AND rq.StartStepID = ps.StepID
	  INNER JOIN CHEF.vwProcessTypes t
			  ON ps.TypeID = t.ProcessTypeID        		  
	  INNER JOIN CHEF.vwMetadataTypes mt
			  ON mt.MetadataTypeID = md.[Type]
		 WHERE rq.RequestStatus = 1	   --Started
			 AND md.[Type] = @MetadataTypeID
		ORDER BY rq.QueueID
	END
ELSE	--for Global Config and XSD
	BEGIN
		SELECT 
			 md.ProcessID
			,md.ProcessName
			,MONTH(GETDATE()) AS CalendarMonth
			,YEAR(GETDATE()) AS CalendarYear
			,0 AS FiscalMonthNumber
			,0 AS FiscalYearNumber
			,0 AS StartStepID
			,0 AS ProcessTypeID
			,'' AS ProcessTypeName
			,mt.MetadataTypeID
			,mt.MetadataTypeName
			,md.MetaData
			,'' AS LineageID
		FROM CHEF.MetaData md
		INNER JOIN CHEF.vwMetadataTypes mt
          ON mt.MetadataTypeID = md.[Type]
	   WHERE md.[Type] = @MetadataTypeID
	END

IF OBJECT_ID('CHEF.GetMetaData','P') IS NOT NULL
	RAISERROR('Created stored procedure CHEF.GetMetaData',0,1) WITH NOWAIT
ELSE
	RAISERROR('Failed to Create stored procedure CHEF.GetMetaData',0,1) WITH NOWAIT

RAISERROR('-------------------------------------------------------Procedure CHEF.GetMetaData ---------------------------------------------------------------------',0,1) WITH NOWAIT;
PRINT ''	--insert a blank line
GO