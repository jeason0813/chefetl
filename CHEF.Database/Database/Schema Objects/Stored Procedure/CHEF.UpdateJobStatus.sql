SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

	
		 
RAISERROR('-------------------------------------------------------Procedure CHEF.UpdateJobStatus ---------------------------------------------------------------------',0,1) WITH NOWAIT;
DECLARE @DBName sysname = DB_NAME()
RAISERROR('Dropping and Creating Stored procedure CHEF.UpdateJobStatus in Database %s',0,1,@DBName) WITH NOWAIT
IF OBJECT_ID('CHEF.UpdateJobStatus','P') IS NOT NULL
BEGIN
	RAISERROR('Dropping stored procedure CHEF.UpdateJobStatus',0,1) WITH NOWAIT
	DROP PROCEDURE CHEF.UpdateJobStatus
	RAISERROR('Dropped stored procedure CHEF.UpdateJobStatus',0,1) WITH NOWAIT
END
RAISERROR('Creating stored procedure CHEF.UpdateJobStatus',0,1) WITH NOWAIT
GO

/* ==========================================================================================
	File		: CHEF.UpdateJobStatus.sql
	Name		: CHEF.UpdateJobStatus
	Author		: jyotira
	Date		: 25th-Aug-2010
	Description	: Create the proc to update statue in the CHEF.RequestQueue table
						  
	Unit Test:
			exec CHEF.UpdateJobStatus @Stage = 'Started'
			exec CHEF.UpdateJobStatus @Stage = 'Finished'
			exec CHEF.UpdateJobStatus @Stage = 'Failed'
			
	

	Verify:
		1.	select * from CHEF.RequestQueue
		2. 
			
	Note:
		1.	No try catch as it is expected to throw error and crash which would be handled by the calling method
		
	Change History
	-------- --------- -----------------------------------------------------------------------
	Date		  Name            Description
	-------- --------- -----------------------------------------------------------------------
	 
	17-Sep-2010		balajim		Added validatation in the SP to allow only any of the inputs viz., Started,Finished,Stopped,Failed 
								Changed the update statements in RequestQueue to update by QueueID (which is PK)
								StatusID used from view chef.vwStatus

   23-Sep-2010      jyotira   Made logical sepeartions between different updates on Request queue
                               Added parameters:-@qid and @source.
   09-May-2013      ramsingh   Added CHEF Notification.
========================================================================================== */

CREATE PROCEDURE [CHEF].[UpdateJobStatus]
   @Stage varchar(20), --e.g., Started,Finished,Stopped,Failed 
   @qid int =NULL,---optional Queue id
   @source varchar(30)=NULL---optional.Required to know which of the two- contoller/executor is calling 
As

Declare 
     @ProcessStep varchar(255)
	,@ProcessID int 
	,@StatusID tinyint
	,@ProcessName varchar(255)
	,@queueid int

--get the @RequestStatus
select @StatusID = StatusID from CHEF.vwStatus where StatusName = @Stage

SELECT @ProcessName=ProcessName from CHEF.MetaData M where exists
	(SELECT ProcessID from CHEF.RequestQueue R where processId = M.processID and exists 
	(SELECT QueueID from CHEF.RequestQueue where queueID = R.queueid and  RequestStatus = 1))

--validate the input data
if @StatusID is null
begin
	raiserror('Invalid Status! The Status should be from the values as it is in CHEF.vwStatus',11,1)
	return
end

IF @Stage='Started' and @source='Controller'
BEGIN
	IF EXISTS(SELECT * FROM CHEF.RequestQueue where RequestStatus=1) --added jyoti call from Controller
	BEGIN
	SELECT @queueid=Queueid from CHEF.RequestQueue where RequestStatus=1
	UPDATE CHEF.RequestQueue set RequestStatus=3 where RequestStatus=1
	EXEC [CHEF].[CloseOpenLogEnteries] @Stage,@queueid
	END
END
    
ELSE IF @Stage='Started' and @source='Executor'
	BEGIN

	SELECT @queueid=MIN(QueueID) from CHEF.RequestQueue where RequestStatus=0 -- call from Executor
	and ScheduledDate<=GETDATE() ---Process Picked from queue in First Come First basis
	SELECT @ProcessStep=ProcessName from CHEF.MetaData where ProcessID=
	(SELECT ProcessID from CHEF.RequestQueue where QueueID=@queueid)
	UPDATE CHEF.RequestQueue set RequestStatus=@StatusID where QueueID=@queueid  
	EXEC CHEF.InsertLog @ProcessStep = @ProcessStep, @StatusID = @StatusID----Inserting Process Start in Chef.Log table 
  
    END
		
ELSE IF @Stage='Stopped'
	BEGIN
	SELECT @ProcessStep=ProcessName from CHEF.MetaData where ProcessID=
	(SELECT ProcessID from CHEF.RequestQueue where QueueID=@qid)
	UPDATE CHEF.RequestQueue set RequestStatus=@StatusID where QueueID=@qid
	END
	
	
ELSE IF @Stage='Finished'
	BEGIN
	SELECT @queueid =QueueID from CHEF.RequestQueue where RequestStatus = 1
	SELECT @ProcessStep=ProcessName from CHEF.MetaData where ProcessID=
	(SELECT ProcessID from CHEF.RequestQueue where QueueID=@queueid)
	UPDATE CHEF.RequestQueue set RequestStatus=@StatusID where QueueID=@queueid  
	EXEC CHEF.InsertLog @ProcessStep = @ProcessStep, @StatusID = @StatusID,@QueueID=@queueid   ----Inserting Process Finish in Chef.Log table
	END


	
ELSE IF @Stage='Failed'
	BEGIN
	SELECT @queueid=QueueID from CHEF.RequestQueue where RequestStatus=1
	UPDATE CHEF.RequestQueue set RequestStatus=@StatusID where QueueID = @queueid
	EXEC [CHEF].[CloseOpenLogEnteries] @Stage,@queueid --To UPDATE the request queue and close open logs with failed status/
	END


IF @Stage = 'Finished' OR @Stage = 'Failed' OR @Stage='Stopped'
BEGIN
	EXEC CHEF.SendStatusMail @Stage,@ProcessName
END
