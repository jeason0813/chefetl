USE [msdb]
GO

/* ==========================================================================================
	File			: CHEF_Controller.sql
	Name		: CHEF_Controller 
	Author	: balajim	
	Date		: 4th Aug 2010
	Description	: This job is responsible to watch the Action Queue and the Executor Job in an endless loop with a waittime in between.
						  Whenever there is an Action requested and the Executor Job is not running it would invoke the Executor Job to run
 
	Pseudocode: 
	1.	Run in an WHILE loop continuously with a breather in between 
	2. Monitor the CHEF.InsertRequestQueue table and the CHEF_Executor Job
	3. If the CHEF_Executor Job is not running and there is a new request pending in CHEF.InsertRequestQueue table, start the CHEF_Executor job
		
	Unit Test: 
	1.	

	Verify Results:
	1.	
	2.	 

	Note:
	1.	
	2.	

	Change History:
	-------- --------- -----------------------------------------------------------------------
	Date		Name            Description
	-------- --------- -----------------------------------------------------------------------
	23-9-2010  jyotira	       Updated the start step to add the source
========================================================================================== */
BEGIN TRY

DECLARE 
	@Msg VARCHAR(1000)
	,@Err VARCHAR(1000)
    ,@ReturnCode INT

    SELECT @ReturnCode = 0	
	
	BEGIN TRANSACTION
			/****** Object:  JobCategory [CHEFJobs]   ******/
			IF NOT EXISTS (SELECT name FROM msdb.dbo.syscategories WHERE name=N'CHEFJobs' AND category_class=1)
			BEGIN
				EXEC @ReturnCode = msdb.dbo.sp_add_category @class=N'JOB', @type=N'LOCAL', @name=N'CHEFJobs'
			END

			/*				Delete existing Job			*/
			IF  EXISTS (SELECT job_id FROM msdb.dbo.sysjobs_view WHERE name = N'CHEF_Controller')
			EXEC @ReturnCode = msdb.dbo.sp_delete_job @job_name=N'CHEF_Controller', @delete_unused_schedule=1

			/*				Add new Job			*/
			DECLARE @jobId BINARY(16)
			EXEC @ReturnCode =  msdb.dbo.sp_add_job @job_name=N'CHEF_Controller', 
					@enabled=1, 
					@notify_level_eventlog=0, 
					@notify_level_email=0, 
					@notify_level_netsend=0, 
					@notify_level_page=0, 
					@delete_level=0, 
					@description=N'CHEF_Controller', 
					@category_name=N'CHEFJobs', 
					@owner_login_name=N'sa', @job_id = @jobId OUTPUT

	        EXEC @ReturnCode = msdb.dbo.sp_add_jobstep @job_id=@jobId, @step_name=N'SQLJobStart', 
			        @step_id=1, 
			        @cmdexec_success_code=0, 
			        @on_success_action=4, 
			        @on_success_step_id=4, 
			        @on_fail_action=4, 
			        @on_fail_step_id=3, 
			        @retry_attempts=3, 
			        @retry_interval=1, 
			        @os_run_priority=0, @subsystem=N'TSQL', 
					@command=N'EXEC CHEF.UpdateJobStatus @Stage = ''Started'' ,@source=''Controller'';',				--This Database needs to be dynamically updated during deployment
					@database_name=N'$(DBName)',																								--This Database needs to be dynamically updated during deployment
					@output_file_name=N'$(OutputPath)\Log\CHEF_Controller.log',									--This path needs to be dynamically updated during deployment
			        @flags=4
	        IF @ReturnCode <> 0
                RAISERROR('Error Creating job Steps',0,17) WITH NOWAIT

	        EXEC @ReturnCode = msdb.dbo.sp_add_jobstep @job_id=@jobId, @step_name=N'SQLJobFinish', 
			        @step_id=2, 
			        @cmdexec_success_code=0, 
			        @on_success_action=1, 
			        @on_success_step_id=0, 
			        @on_fail_action=4, 
			        @on_fail_step_id=3, 
			        @retry_attempts=3, 
			        @retry_interval=1, 
			        @os_run_priority=0, @subsystem=N'TSQL', 
					@command=N'RAISERROR (''Controller Job Finished'',16,1);', 			--This Database needs to be dynamically updated during deployment
					@database_name=N'$(DBName)',																								--This Database needs to be dynamically updated during deployment
					@output_file_name=N'$(OutputPath)\Log\CHEF_Controller.log',									--This path needs to be dynamically updated during deployment
			        @flags=6
	        IF @ReturnCode <> 0
                RAISERROR('Error Creating job Steps',0,17) WITH NOWAIT

	        EXEC @ReturnCode = msdb.dbo.sp_add_jobstep @job_id=@jobId, @step_name=N'SQLJobFail', 
			        @step_id=3, 
			        @cmdexec_success_code=0, 
			        @on_success_action=2, 
			        @on_success_step_id=0, 
			        @on_fail_action=2, 
			        @on_fail_step_id=0, 
			        @retry_attempts=3, 
			        @retry_interval=1, 
			        @os_run_priority=0, @subsystem=N'TSQL', 
					@command=N'RAISERROR (''Controller Job Failed'',16,1);', 		--This Database needs to be dynamically updated during deployment
					@database_name=N'$(DBName)',																								--This Database needs to be dynamically updated during deployment
					@output_file_name=N'$(OutputPath)\Log\CHEF_Controller.log',									--This path needs to be dynamically updated during deployment
			        @flags=6
	        IF @ReturnCode <> 0
                RAISERROR('Error Creating job Steps',0,17) WITH NOWAIT

			EXEC @ReturnCode = msdb.dbo.sp_add_jobstep @job_id=@jobId, @step_name=N'Controller', 
		            @step_id=4, 
		            @cmdexec_success_code=0, 
		            @on_success_action=4, 
		            @on_success_step_id=2, 
		            @on_fail_action=4, 
		            @on_fail_step_id=3, 
		            @retry_attempts=0, 
		            @retry_interval=0, 
					@os_run_priority=0, @subsystem=N'TSQL', 
					@command=N'
/* 
	-------- --------- -----------------------------------------------------------------------
	Date		Name            Description
	-------- --------- -----------------------------------------------------------------------
	04-Aug-10	balajim			Created
	02-dec-10  ramsingh			updated
	09-oct-11   ramsingh		updated
	12-oct-12	ramsingh        updated  wait time 1=>30 sec

========================================================================================== */

DECLARE 
	 @WaitTime varchar(10) = ''00:00:30''			--This polling time may be increased/reduced later if needed and may be based on the Configuration from XML metadata file
	,@IsIRFinished bit
	,@ExpectedIRDurationInSeconds int

	--loop continuously and poll after every 1 min to check if the Executor job needs to be started
	WHILE (1=1)
	BEGIN
		WAITFOR DELAY @WaitTime	--breather	

		--Start the next Process if the Executor is not running and there is an Action Item in the Queue
IF NOT EXISTS
		(SELECT 1 FROM msdb..sysjobactivity ja 
							    JOIN msdb..sysjobs j 
								ON ja.job_id = j.job_id 
							    WHERE j.enabled = 1 
							    AND j.name = ''CHEF_Executor''
							    AND ja.stop_execution_date IS NULL
								AND ja.start_execution_date IS NOT NULL
							    AND ja.session_id = (SELECT MAX(session_id) FROM msdb..syssessions))
		AND NOT EXISTS(SELECT 1 FROM CHEF.RequestQueue WITH (NOLOCK) WHERE RequestStatus = 1)	--process started but not finished
		AND EXISTS(SELECT 1 FROM CHEF.RequestQueue WITH (NOLOCK) WHERE RequestStatus = 0 and ScheduledDate<=GETDATE())	--process queued

			EXEC msdb.dbo.sp_start_job N''CHEF_Executor''
		
	END ' , 
					@database_name=N'$(DBName)',															--This DatabaseName needs to be updated during installation 
		            @output_file_name=N'$(OutputPath)\Log\CHEF_Controller.log',			--This path needs to be updated during installation
		            @flags=34

	        IF @ReturnCode <> 0
                RAISERROR('Error Creating job Steps',0,17) WITH NOWAIT

			EXEC @ReturnCode = msdb.dbo.sp_update_job @job_id = @jobId, @start_step_id = 1

			EXEC @ReturnCode = msdb.dbo.sp_add_jobserver @job_id = @jobId, @server_name = N'(local)'

	PRINT 'CHEF_Controller SQL Job Created Successfully'

	COMMIT TRANSACTION

END TRY
BEGIN CATCH
        
       SET @Err = '<Error>
					<Number>'+ CAST(ERROR_NUMBER() as varchar(10)) +'</Number>
					<Message>'+ ERROR_MESSAGE() +'</Message>
					<Line>'+ CAST(ERROR_LINE() as varchar(10)) +'</Line>
				</Error>'
		SET @Msg = ' Error Detail: ' + @Err

		RAISERROR(@Msg,0,17) WITH NOWAIT
	
		IF (@@TRANCOUNT > 0)
		BEGIN 
			ROLLBACK TRANSACTION
		END	
END CATCH
