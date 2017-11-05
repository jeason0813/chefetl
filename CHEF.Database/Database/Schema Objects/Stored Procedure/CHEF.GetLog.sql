SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

RAISERROR('-------------------------------------------------------Procedure CHEF.GetLog ---------------------------------------------------------------------',0,1) WITH NOWAIT;
DECLARE @DBName sysname = DB_NAME()
RAISERROR('Dropping and Creating Stored procedure CHEF.GetLog in Database %s',0,1,@DBName) WITH NOWAIT
IF OBJECT_ID('CHEF.GetLog','P') IS NOT NULL
BEGIN
	RAISERROR('Dropping stored procedure CHEF.GetLog',0,1) WITH NOWAIT
	DROP PROCEDURE CHEF.GetLog
	RAISERROR('Dropped stored procedure CHEF.GetLog',0,1) WITH NOWAIT
END
RAISERROR('Creating stored procedure CHEF.GetLog',0,1) WITH NOWAIT
GO

/* ==========================================================================================
	File			: CHEF.GetLog.sql
	Name			: CHEF.GetLog
	Author			: kaicho
	Date			: 2nd-Aug-2010
	Description		: Get Log information for UI
						   
	
	Unit Test:
		chef.GetLog 

	Verify:
		
			
	Note:
		1.	
		
	Change History
	-------- --------- -----------------------------------------------------------------------
	Date		Name            Description
	
========================================================================================== */
	
CREATE PROCEDURE CHEF.GetLog
	 @CY smallint = NULL
	,@CM smallint = NULL
	,@ProcessID smallint = NULL
	,@StepID smallint = NULL
	
		
AS

if @ProcessID is null and @StepID is null

BEGIN
SELECT * FROM CHEF.DataLoadStatus(@CY,@CM,NULL) 
END

ELSE

BEGIN
--DECLARE @QueueID INT
--SELECT @QueueID = QUEUEID FROM CHEF.RequestQueue WHERE ProcessID=@ProcessID AND StartStepID= @StepID AND CalendarMonth=@CM AND CalendarYear = @CY 
--SELECT * FROM CHEF.DataLoadStatus(@CY,@CM,@QueueID)
-- Modified the logic to fetch all log records of a process-step for a particular month-year.
SELECT * FROM CHEF.DataLoadStatus(@CY,@CM,Null)
where StartLogID in 
		(
			select LogID
			from CHEF.Log
			where QueueID in (select QueueID from CHEF.RequestQueue where ProcessID = @ProcessID and StartStepID = @StepID)
		)


END
GO

IF OBJECT_ID('CHEF.GetLog','P') IS NOT NULL
	RAISERROR('Created stored procedure CHEF.GetLog',0,1) WITH NOWAIT
ELSE
	RAISERROR('Failed to Create stored procedure CHEF.GetLog',0,1) WITH NOWAIT

RAISERROR('-------------------------------------------------------Procedure CHEF.GetLog ---------------------------------------------------------------------',0,1) WITH NOWAIT;
PRINT ''	--insert a blank line
GO



