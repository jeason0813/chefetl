SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

RAISERROR('-------------------------------------------------------Procedure CHEF.InsertLog ---------------------------------------------------------------------',0,1) WITH NOWAIT;
DECLARE @DBName sysname = DB_NAME()
RAISERROR('Dropping and Creating Stored procedure CHEF.InsertLog in Database %s',0,1,@DBName) WITH NOWAIT
IF OBJECT_ID('CHEF.InsertLog','P') IS NOT NULL
BEGIN
	RAISERROR('Dropping stored procedure CHEF.InsertLog',0,1) WITH NOWAIT
	DROP PROCEDURE CHEF.InsertLog
	RAISERROR('Dropped stored procedure CHEF.InsertLog',0,1) WITH NOWAIT
END
RAISERROR('Creating stored procedure CHEF.InsertLog',0,1) WITH NOWAIT
GO

/* ==========================================================================================
	File		: CHEF.InsertLog.sql
	Name		: CHEF.InsertLog
	Author		: balajim
	Date			: 2nd-Aug-2010
	Description	: Create the proc to insert rows to the CHEF.Log table
						   This table would be created as part of the Deployment Script into the selected Database where CHEF resources would be maintained
	
	Unit Test:
		1.	TRUNCATE TABLE CHEF.[Log] 
		 
			exec CHEF.InsertLog @ProcessStep = 'Load_ACCCAT1010'
			exec CHEF.InsertLog @ProcessStep = 'Load_ACCCAT1010', @StatusID = 3
			exec CHEF.InsertLog @ProcessStep = 'Load_ACCCAT1010', @StatusID = 3,@RowsAffected = 50
			exec CHEF.InsertLog @ProcessStep = 'Load_ACCCAT1010', @StatusID = 4,@RowsAffected = 100, @Description = 'Failed. Column length exceeded.'
			exec CHEF.InsertLog @ProcessStep = 'Load_ACCCAT1010', @StatusID = 1,@RowsAffected = 0, @Description = NULL, @QueueID = 1
			exec CHEF.InsertLog @ProcessStep = 'Load_ACCCAT1010', @StatusID = 2,@RowsAffected = 100, @Description = NULL, @QueueID = 1, @ProcessDate = '2010/08/02'
		2.   

	Verify:
		1.	select * from CHEF.Log
		2. 
			
	Note:
		1.	No try catch as it is expected to throw error and crash which would be handled by the calling method
		
	Change History
	-------- --------- -----------------------------------------------------------------------
	Date		Name            Description
	-------- --------- -----------------------------------------------------------------------
	17-Sep-2010	balajim		Corrected the Status value
========================================================================================== */
	
CREATE PROCEDURE CHEF.InsertLog
		 @ProcessStep varchar(255)		--Unique for ProcessID e.g.,Load_ACCCAT1010,Load_EXRATE01Currency,Truncate_ACCCAT1010,Update_ACCCAT1010
		,@StatusID tinyint = 1					--e.g., 1:Started, 2:Finished, 3:Stopped, 4:Failed 
		,@RowsAffected bigint  = 0
		,@Description varchar(500) = NULL
		,@QueueID int = NULL					--from the QueueID, ProcessID can be picked from the CHEF.RequestQueue table
		,@ProcessDate datetime = NULL 
AS
		INSERT CHEF.[Log](ProcessStep,QueueID,ProcessDate,StatusID,RowsAffected,[Description]) 
		SELECT 
			 @ProcessStep ProcessStep
			,ISNULL(@QueueID, (SELECT MIN(QueueID) FROM CHEF.RequestQueue WHERE RequestStatus = 1)) AS QueueID		--Pick the row for which the status is Started, at the end it changes to Finished
			,ISNULL(@ProcessDate,GETDATE()) ProcessDate
			,@StatusID StatusID
			,@RowsAffected RowsAffected
			,ISNULL(@Description, @ProcessStep +
													CASE @StatusID  WHEN 1 THEN ' Started. '
																	WHEN 2 THEN ' Finished. '
																	WHEN 3 THEN ' Stopped. '
																	ELSE ' Failed. '
														+'Estimated Rows('+CAST(@RowsAffected as varchar(20))+')'
													END)
			AS [Description]
		
GO

IF OBJECT_ID('CHEF.InsertLog','P') IS NOT NULL
	RAISERROR('Created stored procedure CHEF.InsertLog',0,1) WITH NOWAIT
ELSE
	RAISERROR('Failed to Create stored procedure CHEF.InsertLog',0,1) WITH NOWAIT

RAISERROR('-------------------------------------------------------Procedure CHEF.InsertLog ---------------------------------------------------------------------',0,1) WITH NOWAIT;
PRINT ''	--insert a blank line
GO