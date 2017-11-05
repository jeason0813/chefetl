SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON

RAISERROR('-------------------------------------------------------Procedure CHEF.InsertRequestQueue ---------------------------------------------------------------------',0,1) WITH NOWAIT;
DECLARE @DBName sysname = DB_NAME()
RAISERROR('Dropping and Creating Stored procedure CHEF.InsertRequestQueue in Database %s',0,1,@DBName) WITH NOWAIT
IF OBJECT_ID('CHEF.InsertRequestQueue','P') IS NOT NULL
BEGIN
	RAISERROR('Dropping stored procedure CHEF.InsertRequestQueue',0,1) WITH NOWAIT
	DROP PROCEDURE CHEF.InsertRequestQueue
	RAISERROR('Dropped stored procedure CHEF.InsertRequestQueue',0,1) WITH NOWAIT
END
RAISERROR('Creating stored procedure CHEF.InsertRequestQueue',0,1) WITH NOWAIT
GO

/* ==========================================================================================
	File		: CHEF.InsertRequestQueue.sql
	Name		: CHEF.InsertRequestQueue
	Author		: balajim
	Date		: 2nd-Aug-2010
	Description	: Create the proc to insert rows to the RequestQueue table
						   
	
	Unit Test:
		1.	DELETE FROM CHEF.[Log] WHERE EXISTS(SELECT * FROM CHEF.RequestQueue WHERE QueueID = CHEF.[Log].QueueID)
			DELETE CHEF.RequestQueue
			exec CHEF.InsertRequestQueue @ProcessID = 1
			exec CHEF.InsertRequestQueue @ProcessID = 1, @CalendarMonth = 1
			exec CHEF.InsertRequestQueue @ProcessID = 1, @CalendarMonth = 1, @CalendarYear = 2009
			exec CHEF.InsertRequestQueue @ProcessID = 2, @CalendarMonth = 5, @CalendarYear = 2010, @StartStepID = 6
			exec CHEF.InsertRequestQueue @ProcessID = 1, @CalendarMonth = 1, @CalendarYear = 2009, @StartStepID = 2, @RequestStatus = 1
			exec CHEF.InsertRequestQueue @ProcessID = 1, @CalendarMonth = 1, @CalendarYear = 2009, @StartStepID = 2, @RequestStatus = 1,@LineageID = '36CFA4E7-7248-49C6-8149-BE780C7ED808'
			exec CHEF.InsertRequestQueue @ProcessID = 1, @CalendarMonth = 1, @CalendarYear = 2009, @StartStepID = 2, @RequestStatus = 1,@LineageID = '36CFA4E7-7248-49C6-8149-BE780C7ED808',@RequestedBy = 'balajim'
			exec CHEF.InsertRequestQueue @ProcessID = 1, @CalendarMonth = 1, @CalendarYear = 2009, @StartStepID = 2, @RequestStatus = 1,@LineageID = '36CFA4E7-7248-49C6-8149-BE780C7ED808',@RequestedBy = 'balajim', @RequestedDate = '2010/08/02 20:20:20'
		2.   
		exec CHEF.InsertRequestQueue @ProcessID = 10000,@NextRequest='<Request>
									<Process ID="10000">
										<Steps UsageType="EXCLUDE">
										  <StepID>10010</StepID>
										  <StepID>10020</StepID>
										</Steps>
									</Process>
							</Request>'

	Verify:
		1.	select * from CHEF.RequestQueue
			select * from CHEF.Log where exists(select * from CHEF.RequestQueue where QueueID = CHEF.Log.LogID)
		2. 
			
	Note:
		1. No try catch as it is expected to throw error and crash which would be handled by the calling method
		2. The place from where this SP will be invoked should have ANSI_PADDING ON
		
	Change History
	-------- --------- -----------------------------------------------------------------------
	Date		Name            Description
	-------- --------- -----------------------------------------------------------------------
	11 Aug 2010	kaicho		Added return statement to return Scope_identity()

	            jyotira     Added code not to allow a process to be queued if the for the same 
				            values of the process id ,Calender Month and Year there already exists 
							a request in the queue with status either queued or running.
	17-Sep-2010	balajim		Corrected the Status values in the example to avoid confusion. 
							Added step to fetch the min StartStepID when it is null from the XML metadata		
							Added ORIGINAL_LOGIN() instead of SUSER_SNAME() as default requester
    13-Jan-2011 ramsingh    Modified the code for UT : For CY- 2011 and -CM 1 RequestQ gets an entry of zero in CM.
========================================================================================== */
	
CREATE PROCEDURE CHEF.InsertRequestQueue
		 @ProcessID smallint									--e.g.,1-OPUS Loading, 2-Excise Loading etc.; as it is in the CHEF XML metadata file, comes as per request from UI
		,@CalendarMonth tinyint =  NULL							--e.g., 1-Jan, 2-Feb        
		,@CalendarYear smallint  = NULL							--e.g., 2009, 2008 
		,@StartStepID smallint  = NULL						    --as it is in the CHEF XML metdata file. By default all the steps in a process will be executed starting from 1st step, optionally it may be started from any other step
		,@ProcessOption XML(CHEF.RequestSteps)  = NULL			--as it is in the CHEF XML metdata file. By default all the steps in a process will be executed starting for SPECIFIC/Exculed/END steps and queuing next proces
		,@RequestStatus tinyint  = 0							--0:Queued, 1:Started, 2:Finished, 3:Stopped, 4:Failed 
		,@LineageID uniqueidentifier = NULL						--e.g., 36CFA4E7-7248-49C6-8149-BE780C7ED808 
		,@RequestedBy nvarchar(25)  = NULL
		,@RequestedDate datetime  = NULL
		,@ScheduledDate datetime  = NULL
AS

--fetch the min StartStepID when null is passed i.e., it should process starting from 1st Step
IF NOT EXISTS (SELECT 1 FROM CHEF.Metadata WHERE ProcessID=@ProcessID and [Type]=0 )
	BEGIN
		RAISERROR('Process ID :%d does not exist in CHEF.Metadata table',16,1,@ProcessID) WITH NOWAIT
	END
ELSE
	BEGIN
	
		IF @StartStepID IS NULL
			SELECT @StartStepID = MIN(StepID) FROM CHEF.vwProcessSteps WHERE ProcessID  = @ProcessID 
		if(@CalendarMonth=0)
			Set @CalendarMonth=1 --Fixed the bug #90768 -UT : For CY- 2011 and -CM 1 RequestQ gets an entry of zero in CM.
		IF NOT EXISTS(SELECT * FROM CHEF.RequestQueue WHERE ProcessID=@ProcessID AND StartStepID=@StartStepID AND RequestStatus in(0,1) AND CalendarMonth=ISNULL(@CalendarMonth,MONTH(GETDATE())) AND CalendarYear=ISNULL(@CalendarYear,YEAR(GETDATE())))
		BEGIN
			INSERT CHEF.RequestQueue(CalendarMonth, CalendarYear, ProcessID, StartStepID, ProcessOption,RequestStatus, LineageID, RequestedBy, RequestedDate,ScheduledDate) 
			VALUES(ISNULL(@CalendarMonth,MONTH(GETDATE())), ISNULL(@CalendarYear,YEAR(GETDATE())), @ProcessID, @StartStepID, @ProcessOption,@RequestStatus, ISNULL(@LineageID,NEWID()), ISNULL(@RequestedBy,LEFT(ORIGINAL_LOGIN(),25)), ISNULL(@RequestedDate,GETDATE()),ISNULL(@ScheduledDate,GETDATE()))

			RETURN SCOPE_IDENTITY();
		END
	END
GO

IF OBJECT_ID('CHEF.InsertRequestQueue','P') IS NOT NULL
	RAISERROR('Created stored procedure CHEF.InsertRequestQueue',0,1) WITH NOWAIT
ELSE
	RAISERROR('Failed to Create stored procedure CHEF.InsertRequestQueue',0,1) WITH NOWAIT

RAISERROR('-------------------------------------------------------Procedure CHEF.InsertRequestQueue ---------------------------------------------------------------------',0,1) WITH NOWAIT;
PRINT ''	--insert a blank line
GO



