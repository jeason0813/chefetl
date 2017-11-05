SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

RAISERROR('-------------------------------------------------------Procedure CHEF.StopProcess ---------------------------------------------------------------------',0,1) WITH NOWAIT;
DECLARE @DBName sysname = DB_NAME()
RAISERROR('Dropping and Creating Stored procedure CHEF.StopProcess in Database %s',0,1,@DBName) WITH NOWAIT
IF OBJECT_ID('CHEF.StopProcess','P') IS NOT NULL
BEGIN
	RAISERROR('Dropping stored procedure CHEF.StopProcess',0,1) WITH NOWAIT
	DROP PROCEDURE CHEF.StopProcess
	RAISERROR('Dropped stored procedure CHEF.StopProcess',0,1) WITH NOWAIT
END
RAISERROR('Creating stored procedure CHEF.StopProcess',0,1) WITH NOWAIT
GO

/* ==========================================================================================
	File			: CHEF.StopProcess.sql
	Name			: CHEF.StopProcess
	Author			:kaicho
	Date			: 2nd-Aug-2010
	Description		: Kills a running process and updates the RequestQueue and Log Tables
						   
	
	Unit Test:
		

	Verify:
		
			
	Note:
		1.	
		
	Change History
	-------- --------- -----------------------------------------------------------------------
	Date		Name            Description
	17-Sep-2010	balajim		ProcessID datatype changed to smallint; Removed the CHEF:Dbname as this sp has to be connected to the DB having CHEF objects
							Corrected the Stopped Status to 3
							Corrected the query and update statement to take care of Process that was already Started (Status=1), and requested to stop apart from the one queued (Status=0)
							Passed the Stage as Stopped to correctly indicate the process

	23 Sept 2010  jyotira  Added calls to updatejobstatus sp to update the status and seperated checks for status 1 and  0
========================================================================================== */
	
CREATE PROCEDURE CHEF.StopProcess
	 @ProcessID smallint					--e.g.,1010-OPUS Loading, 1020-Excise Loading etc.; as it is in the CHEF XML metadata file, comes as per request from UI
	,@CalendarMonth tinyint					--e.g., 1-Jan, 2-Feb        
	,@CalendarYear smallint					--e.g., 2009, 2008 
	,@StartStepID smallint			
AS

Declare @queueid int 

IF EXISTS (SELECT 1 FROM CHEF.RequestQueue WHERE ProcessID = @ProcessID AND CalendarMonth = @CalendarMonth AND CalendarYear = @CalendarYear AND StartStepID=@StartStepID AND RequestStatus IN (0))
BEGIN
SELECT @queueid=QueueID FROM CHEF.RequestQueue WHERE ProcessID = @ProcessID AND CalendarMonth = @CalendarMonth AND CalendarYear = @CalendarYear AND StartStepID=@StartStepID AND RequestStatus IN (0)	--At any point in time, for the same ProcessID the Status could be either Queued or Started but not both
exec CHEF.UpdateJobStatus @Stage = 'Stopped',@qid=@queueid
END

ELSE IF EXISTS(SELECT 1 FROM msdb..sysjobactivity ja 
							   JOIN msdb..sysjobs j 
								 ON ja.job_id = j.job_id 
							  WHERE j.enabled = 1 
							    AND j.name = 'CHEF_Executor' 
							    AND ja.stop_execution_date IS NULL
								AND ja.start_execution_date IS NOT NULL
							    AND ja.session_id = (SELECT MAX(session_id) FROM msdb..syssessions))
								AND EXISTS (SELECT 1 FROM CHEF.RequestQueue WHERE ProcessID = @ProcessID AND CalendarMonth = @CalendarMonth AND CalendarYear = @CalendarYear AND StartStepID=@StartStepID AND RequestStatus=1)
BEGIN
EXEC msdb.dbo.sp_stop_job N'CHEF_Executor';
WHILE EXISTS(SELECT 1 FROM msdb..sysjobactivity ja 
							   JOIN msdb..sysjobs j 
								 ON ja.job_id = j.job_id 
							  WHERE j.enabled = 1 
							    AND j.name = 'CHEF_Executor' 
							    AND ja.stop_execution_date IS NULL
								AND ja.start_execution_date IS NOT NULL
							    AND ja.session_id = (SELECT MAX(session_id) FROM msdb..syssessions))
BEGIN

	WAITFOR DELAY '00:00:10'
END
SELECT @queueid=QueueID FROM CHEF.RequestQueue WHERE ProcessID = @ProcessID AND CalendarMonth = @CalendarMonth AND CalendarYear = @CalendarYear AND StartStepID=@StartStepID AND RequestStatus =1	
--At any point in time, for the same ProcessID the Status could be either Queued or Started but not both
    EXEC CHEF.UpdateJobStatus @Stage = 'Stopped',@qid=@queueid
	EXEC CHEF.CloseOpenLogEnteries @Stage = 'Stopped',@queueid=@queueid;
END
GO

IF OBJECT_ID('CHEF.StopProcess','P') IS NOT NULL
	RAISERROR('Created stored procedure CHEF.StopProcess',0,1) WITH NOWAIT
ELSE
	RAISERROR('Failed to Create stored procedure CHEF.StopProcess',0,1) WITH NOWAIT

RAISERROR('-------------------------------------------------------Procedure CHEF.StopProcess ---------------------------------------------------------------------',0,1) WITH NOWAIT;
PRINT ''	--insert a blank line
GO



