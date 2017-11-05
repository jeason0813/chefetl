USE [msdb]
GO

/* ==========================================================================================
	File			: CHEF_Executor.sql
	Name		: CHEF_Executor
	Author	: balajim
	Date		: 4th Aug 2010
	Description	: This Job will invoke the CHEF Engine to create a Dynamic SSIS Package for the selected Process and Steps and run it 
 
	Pseudocode: 
	1.	
	2. 
		
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
	23-9-2010  jyotira          Updated start step by adding source
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

    /*
        Delete existing Job
    */
    IF  EXISTS (SELECT job_id FROM msdb.dbo.sysjobs_view WHERE name = N'CHEF_Executor')
    EXEC @ReturnCode = msdb.dbo.sp_delete_job @job_name=N'CHEF_Executor', @delete_unused_schedule=1

    /*
        Add new Job
    */
    DECLARE @jobId BINARY(16)
    EXEC  msdb.dbo.sp_add_job @job_name=N'CHEF_Executor', 
		    @enabled=1, 
		    @notify_level_eventlog=0, 
		    @notify_level_email=2, 
		    @notify_level_netsend=2, 
		    @notify_level_page=2, 
		    @delete_level=0, 
		    @category_name=N'CHEFJobs', 
		    @owner_login_name=N'sa', @job_id = @jobId OUTPUT

    EXEC msdb.dbo.sp_add_jobserver @job_name=N'CHEF_Executor', @server_name = @@SERVERNAME
	IF @ReturnCode <> 0
        RAISERROR('Error Creating job Steps',0,17) WITH NOWAIT
	
	--TODO: This section would be changed to execute the SSIS package created from the SSIS Server and not from a folder
	DECLARE @SSISPackagePath AS VARCHAR(max)
--	SELECT @SSISPackagePath=<Parse the XML Metadata File to find the path of the SSIS created>
    DECLARE @SSISCommand AS VARCHAR(max)
	SELECT @SSISPackagePath=N'/FILE "' + @SSISPackagePath + '\CHEF.dtsx" /CONFIGFILE "'+ @SSISPackagePath +'\CHEF.dtsConfig" /MAXCONCURRENT " -1 " /CHECKPOINTING OFF /REPORTING E'
		
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
			@command=N'EXEC CHEF.UpdateJobStatus @Stage = ''Started'',@source=''Executor'';',			--This Database needs to be dynamically updated during deployment --@ProcessStep parameter added by Jyoti
			@database_name=N'$(DBName)',																								--This Database needs to be dynamically updated during deployment
			@output_file_name=N'$(OutputPath)\Log\CHEF_Executor.log',									--This path needs to be dynamically updated during deployment
			@flags=4
	IF @ReturnCode <> 0
        RAISERROR('Error Creating job Steps',0,17) WITH NOWAIT

-- If a Action is Stopped or Cancelled then also it would come to this step, which needs to be handled to not update status with Finish but Cancel or Stop
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
			@command=N'EXEC CHEF.UpdateJobStatus @Stage = ''Finished'';', 				--This Database needs to be dynamically updated during deployment --@ProcessStep parameter added by Jyoti
			@database_name=N'$(DBName)',																								--This Database needs to be dynamically updated during deployment
			@output_file_name=N'$(OutputPath)\Log\CHEF_Executor.log',									--This path needs to be dynamically updated during deployment
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
			@command=N'EXEC CHEF.UpdateJobStatus @Stage = ''Failed'';', 				--This Database needs to be dynamically updated during deployment --@ProcessStep parameter added by Jyoti
			@database_name=N'$(DBName)',																								--This Database needs to be dynamically updated during deployment
			@output_file_name=N'$(OutputPath)\Log\CHEF_Executor.log',									--This path needs to be dynamically updated during deployment
			@flags=6
	IF @ReturnCode <> 0
        RAISERROR('Error Creating job Steps',0,17) WITH NOWAIT

	EXEC @ReturnCode = msdb.dbo.sp_add_jobstep @job_id=@jobId, @step_name=N'Create CHEF SSIS Package', 
			@step_id=4, 
			@cmdexec_success_code=0, 
			@on_success_action=4, 
			@on_success_step_id=5, 
			@on_fail_action=4, 
			@on_fail_step_id=3, 
			@retry_attempts=0, 
			@retry_interval=0, 
			@os_run_priority=0,
			@subsystem=N'CmdExec', 
			@command=N'$(OutputPath)\Engine\CHEF.exe', 
			@output_file_name=N'$(OutputPath)\Log\CHEF_Executor.log', 
			@flags=0
	IF @ReturnCode <> 0
        RAISERROR('Error Creating job Steps',0,17) WITH NOWAIT

--@SSISPackagePath
        EXEC @ReturnCode = msdb.dbo.sp_add_jobstep @job_id=@jobId, @step_name=N'Execute CHEF SSIS Package', 
		    @step_id=5, 
		    @cmdexec_success_code=0, 
		    @on_success_action=4, 
		    @on_success_step_id=2, 
		    @on_fail_action=4, 
		    @on_fail_step_id=3, 
		    @retry_attempts=0, 
		    @retry_interval=0, 
		    @os_run_priority=0, 
			@subsystem=N'TSQL', 
		    --@command=@SSISPackagePath,
			@command=N'EXEC [CHEF].[ExecutePackageFromCatalog]',
		    @database_name=N'$(DBName)', 
		    @output_file_name=N'$(OutputPath)\Log\CHEF_Executor.log', 
		    @flags=34
	
	IF @ReturnCode <> 0
        RAISERROR('Error Creating job Steps',0,17) WITH NOWAIT
		    
    EXEC @ReturnCode = msdb.dbo.sp_update_job @job_name=N'CHEF_Executor', 
		    @enabled=1, 
		    @start_step_id=1, 
		    @notify_level_eventlog=0, 
		    @notify_level_email=2, 
		    @notify_level_netsend=2, 
		    @notify_level_page=2, 
		    @delete_level=0, 
		    @description=N'', 
		    @category_name=N'CHEFJobs', 
		    @owner_login_name=N'sa', 
		    @notify_email_operator_name=N'', 
		    @notify_netsend_operator_name=N'', 
		    @notify_page_operator_name=N''

	IF @ReturnCode <> 0
        RAISERROR('Error updating job Steps',0,17) WITH NOWAIT
    
    PRINT 'CHEF_Executor SQL Job Created Successfully'

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
