SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

	
		 
RAISERROR('-------------------------------------------------------Procedure CHEF.CloseOpenLogEnteries ---------------------------------------------------------------------',0,1) WITH NOWAIT;
DECLARE @DBName sysname = DB_NAME()
RAISERROR('Dropping and Creating Stored procedure CHEF.CloseOpenLogEnteries in Database %s',0,1,@DBName) WITH NOWAIT
IF OBJECT_ID('CHEF.CloseOpenLogEnteries','P') IS NOT NULL
BEGIN
	RAISERROR('Dropping stored procedure CHEF.CloseOpenLogEnteries',0,1) WITH NOWAIT
	DROP PROCEDURE CHEF.CloseOpenLogEnteries
	RAISERROR('Dropped stored procedure CHEF.CloseOpenLogEnteries',0,1) WITH NOWAIT
END
RAISERROR('Creating stored procedure CHEF.CloseOpenLogEnteries',0,1) WITH NOWAIT
GO

/* ==========================================================================================
	File				: CHEF.CloseOpenLogEnteries.sql
	Name			: CHEF.CloseOpenLogEnteries
	Author		: jyotira
	Date			: 25th-Aug-2010
	Description	: Create the proc to update statue in the CHEF.RequestQueue table

	Scenario-I: When the ControlJob starts after stopped or when the Process which was running is Stopped abruptly
		Action: Insert log entries for all the processes which were started under the processid & update the status in RequestQueue
				with Status = 3 i.e., Stopped

	Scenario-I: When the Executorjob fails in between
		Action: Insert log entries for all the processes which were started under the processid & update the status in RequestQueue
				with Status = 4 i.e., Failed
						  
	Unit Test:
			exec CHEF.CloseOpenLogEnteries @ProcessStep = 'Started'
			exec CHEF.CloseOpenLogEnteries @ProcessStep = 'Stopped'
			exec CHEF.CloseOpenLogEnteries @ProcessStep = 'Failed'
			
	

	Verify:
		1.	select * from CHEF.RequestQueue
		2. 
			
	Note:
		1.	No try catch as it is expected to throw error and crash which would be handled by the calling method
		
	Change History
	-------- --------- -----------------------------------------------------------------------
	Date		  Name            Description
	-------- --------- -----------------------------------------------------------------------
	17-Sep-2010	balajim		Corrected the Status values for failed and stopped. Added the detailed note on the actions
							Added Stage "Stopped" to correctly indicate the scenario 	

    23-Sep-2010 jyotira     Added order for package execution complete logging,added a variable @queueuid
========================================================================================== */
		 
CREATE PROCEDURE [CHEF].[CloseOpenLogEnteries]
   @Stage varchar(20),
   @queueid int
As

DECLARE 
 	 @processname varchar(255)
	,@processstep varchar(255)
	,@packageexeutionstepname varchar(255)


   IF (@Stage ='Started' OR @Stage ='Stopped')
   BEGIN

---To check if there is a process with status=1 due to stopping of SQL Agent and for which executor is not running
		IF NOT EXISTS(SELECT 1 FROM msdb..sysjobactivity ja 
							   JOIN msdb..sysjobs j 
								 ON ja.job_id = j.job_id 
							  WHERE j.enabled = 1 
							    AND j.name = 'CHEF_Executor' 
							    AND ja.stop_execution_date IS NULL
								AND ja.start_execution_date IS NOT NULL
							    AND ja.session_id = (SELECT MAX(session_id) FROM msdb..syssessions))
			
		BEGIN
		
			Select  @processname=ProcessName from CHEF.MetaData where 
			ProcessID=(Select ProcessID from CHEF.RequestQueue where QueueID=@queueid)	

			WHILE(EXISTS(Select ProcessStep from CHEF.Log where QueueID=@queueid and ProcessStep<> @processname and ProcessStep not like '%PackageExecution' group by
			ProcessStep having COUNT(StatusID)<>2))
				BEGIN
					Select @processstep =ProcessStep from CHEF.Log where QueueID=@queueid and ProcessStep<> @processname and ProcessStep not like '%PackageExecution' group by
					ProcessStep having COUNT(StatusID)<>2
					EXEC  CHEF.InsertLog @processstep ,@StatusID=3,@QueueID=@queueid
				END
	        IF(exists(Select ProcessStep from CHEF.Log where QueueID=@queueid and ProcessStep<> @processname group by
					ProcessStep having COUNT(StatusID)<>2))
					BEGIN
					Select @packageexeutionstepname =ProcessStep from CHEF.Log where QueueID=@queueid and ProcessStep<> @processname group by
					ProcessStep having COUNT(StatusID)<>2
					EXEC  CHEF.InsertLog @packageexeutionstepname ,@StatusID=3,@QueueID=@queueid
					END
	        EXEC  CHEF.InsertLog @processname ,@StatusID=3,@QueueID=@queueid
		END	 
END
	 
IF @Stage ='Failed' 
BEGIN
           
            Select  @processname=ProcessName from CHEF.MetaData where 
			ProcessID=(Select ProcessID from CHEF.RequestQueue where QueueID=@queueid)	

		WHILE(EXISTS(Select ProcessStep from CHEF.Log where QueueID=@queueid and ProcessStep<> @processname  and ProcessStep not like '%PackageExecution' group by
			ProcessStep having COUNT(StatusID)<>2))
				BEGIN

					Select @processstep =ProcessStep from CHEF.Log where QueueID=@queueid and ProcessStep<> @processname and ProcessStep not like '%PackageExecution' group by
					ProcessStep having COUNT(StatusID)<>2
					EXEC  CHEF.InsertLog @processstep ,@StatusID=4,@QueueID=@queueid
				END
			if(exists(Select ProcessStep from CHEF.Log where QueueID=@queueid and ProcessStep<> @processname group by
					ProcessStep having COUNT(StatusID)<>2))
					begin
					Select @packageexeutionstepname =ProcessStep from CHEF.Log where QueueID=@queueid and ProcessStep<> @processname group by
					ProcessStep having COUNT(StatusID)<>2
					EXEC  CHEF.InsertLog @packageexeutionstepname ,@StatusID=4,@QueueID=@queueid
					end
			EXEC  CHEF.InsertLog @processname ,@StatusID=4,@QueueID=@queueid


END