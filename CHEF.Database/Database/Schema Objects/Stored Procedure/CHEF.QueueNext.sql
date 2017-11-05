
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON

RAISERROR('-------------------------------------------------------Procedure CHEF.QueueNext ---------------------------------------------------------------------',0,1) WITH NOWAIT;
DECLARE @DBName sysname = DB_NAME()
RAISERROR('Dropping and Creating Stored procedure CHEF.QueueNext in Database %s',0,1,@DBName) WITH NOWAIT
IF OBJECT_ID('CHEF.QueueNext','P') IS NOT NULL
BEGIN
	RAISERROR('Dropping stored procedure CHEF.QueueNext',0,1) WITH NOWAIT
	DROP PROCEDURE CHEF.QueueNext
	RAISERROR('Dropped stored procedure CHEF.QueueNext',0,1) WITH NOWAIT
END
RAISERROR('Creating stored procedure CHEF.QueueNext',0,1) WITH NOWAIT
GO

/* ==========================================================================================
	File		: CHEF.QueueNext.sql
	Name		: CHEF.QueueNext
	Author		: guvijaya
	Date		: 8th May 2013
	Description	: Using the Next Requestion value for the current executing queue schedules subsecnt process

	@ReuseScheduledDate - Value 1 schedule subseqent ETL with schedule date of prior, 0 uses GETDATE
						   
	Issue: The duplicate Process ID is not supported in the queue.

	Unit Test:
		1.	EXEC CHEF.QueueNext
		2.	EXEC CHEF.QueueNext 1
		3.	EXEC CHEF.QueueNext 0
		4.	EXEC CHEF.QueueNext 0 - While there exists running Queue (Status 1)
		5.	EXEC CHEF.QueueNext 0 - While there no running Queue (No Status 1)
		5.	EXEC CHEF.QueueNext   - While there is subsseqent ETL to queue
*/

CREATE PROC CHEF.QueueNext(@ReuseScheduledDate BIT = 1)
AS
BEGIN
	DECLARE @ProcessId       INT
	       ,@CalendarMonth TINYINT
           ,@CalendarYear  SMALLINT
		   ,@NextProcess   SMALLINT
		   ,@ProcessOption XML
		   ,@ScheduledDate DATETIME
		   ,@Log           VARCHAR(MAX)
		   ,@NewQueue      INT
		   ,@LogStatus     INT

	DECLARE @ProcessOptionList TABLE 
    (
	  RNum      INT IDENTITY(1,1), 
      ProcessId INT
    )

	SELECT @ProcessId     = ProcessId
	      ,@CalendarMonth = CalendarMonth
	      ,@CalendarYear  = CalendarYear
		  ,@ProcessOption = ProcessOption
		  ,@ScheduledDate = IIF(@ReuseScheduledDate =1, ScheduledDate, GETDATE())
	  FROM CHEF.RequestQueue
	 WHERE RequestStatus = 1
	 ORDER BY QueueId DESC

	 IF @ProcessOption IS NULL 
		RETURN

	 IF @ProcessId IS NULL
	 BEGIN
	    RAISERROR (N'No queue found with status 1,  QueueNext can be called only from active ETL process', 16,1)
		RETURN
	 END

	 INSERT INTO @ProcessOptionList(ProcessId) 
	 SELECT Process.value('@ID', 'int')                              ProcessId
	 FROM   @ProcessOption.nodes('/Request/Process') AS A(Process) 

	SELECT @NextProcess = ProcessId
	  FROM @ProcessOptionList 
	 WHERE RNum = (SELECT RNum FROM @ProcessOptionList WHERE ProcessID = @ProcessID) + 1

	SET @ProcessOption.modify('delete (Request/Process[@ID=sql:variable("@ProcessID")])') 

	IF @NextProcess IS NOT NULL
	BEGIN
	    SELECT @Log = CONCAT('Auto queued with NextProcess: ', @NextProcess,' CalendarMonth: ', @CalendarMonth, ' CalendarYear: ', @CalendarYear, ' ScheduledDate: ', @ScheduledDate,' ProcessOption: ', CAST(@ProcessOption AS VARCHAR(MAX)))
		EXEC CHEF.InsertLog @ProcessStep = 'Auto Queue'
		                   ,@Description = @Log

	   	EXEC @NewQueue = CHEF.InsertRequestQueue @ProcessID = @NextProcess
		                            ,@CalendarMonth = @CalendarMonth
								    ,@CalendarYear  = @CalendarYear
								    ,@ScheduledDate = @ScheduledDate
	                                ,@ProcessOption = @ProcessOption

        SET @Log = IIF(@NewQueue=0,'Auto queue dishonored, same process already waiting in queue', CONCAT('Successfully Auto queued. Queue id: ', @NewQueue))

		SET @LogStatus = IIF(@NewQueue>0,2,4) -- 2 success, 4 failed

		EXEC CHEF.InsertLog @ProcessStep = 'Auto Queue'
		                   ,@Description = @Log
						   ,@StatusID = @LogStatus
	END
END

