SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

	
		 
RAISERROR('-------------------------------------------------------Procedure CHEF.ExecutePackageFromCatalog ---------------------------------------------------------------------',0,1) WITH NOWAIT;
DECLARE @DBName sysname = DB_NAME()
RAISERROR('Dropping and Creating Stored procedure CHEF.ExecutePackageFromCatalog in Database %s',0,1,@DBName) WITH NOWAIT
IF OBJECT_ID('CHEF.ExecutePackageFromCatalog','P') IS NOT NULL
BEGIN
	RAISERROR('Dropping stored procedure CHEF.ExecutePackageFromCatalog',0,1) WITH NOWAIT
	DROP PROCEDURE CHEF.ExecutePackageFromCatalog
	RAISERROR('Dropped stored procedure CHEF.ExecutePackageFromCatalog',0,1) WITH NOWAIT
END
RAISERROR('Creating stored procedure CHEF.ExecutePackageFromCatalog',0,1) WITH NOWAIT
GO

/* ==========================================================================================
	File		: CHEF.ExecutePackageFromCatalog.sql
	Name		: CHEF.ExecutePackageFromCatalog
	Author		: ramsingh
	Date		: 29th-oct-2012
	Description	: Execute the CHEF SSIS Package recently created and deployed in the SSISDB Catalog.
	
	Unit Test:
		1.	EXEC CHEF.ExecutePackageFromCatalog

	Verify:
		1.	SELECT * from CHEF.RequestQueue
		2.  SELECT * from CHEF.Log

	-------- --------- -----------------------------------------------------------------------
	Date		Name            Description
	-------- --------- -----------------------------------------------------------------------
	
========================================================================================== */

CREATE PROCEDURE [CHEF].[ExecutePackageFromCatalog]
AS

Declare @RC int
Declare @folderName nvarchar(100)
Declare @projectName nvarchar(100)
Declare @packageName nvarchar(100)
Declare @referenceID bigint
Declare @use32BitRuntime bit
Declare @execution_id bigint
Declare @processID nvarchar(50)
Declare @processName nvarchar(50)
DECLARE @processTypeName varchar(50)
SELECT TOP 1 @processID= md.ProcessID ,@processName=md.ProcessName,@processTypeName=t.ProcessTypeName FROM CHEF.MetaData md   INNER JOIN CHEF.RequestQueue rq	  ON md.ProcessID = rq.ProcessID  INNER JOIN CHEF.vwProcessSteps ps   ON rq.ProcessID = ps.ProcessID   AND rq.StartStepID = ps.StepID  INNER JOIN CHEF.vwProcessTypes t  ON ps.TypeID = t.ProcessTypeID     INNER JOIN CHEF.vwMetadataTypes mt          ON mt.MetadataTypeID = md.[Type]     WHERE rq.RequestStatus = 1	  AND md.[Type] = 0    ORDER BY rq.QueueID 
Set @folderName = 'CHEFFolder'
Set @projectName = @processID+'_'+@processName +'_'+@processTypeName
Set @packageName = 'Package.dtsx'
Set @use32BitRuntime = 0
EXEC @RC = [SSISDB].[catalog].[create_execution]
		@package_name=@packageName, 
        @execution_id=@execution_id OUTPUT, 
        @folder_name=@folderName, 
        @project_name=@projectName, 
        @use32bitruntime=False;
PRINT 'CREATE_EXCTURE'+ CAST(@RC AS VARCHAR(10))
EXEC @RC = [SSISDB].[catalog].[set_execution_parameter_value]
	 @execution_id,
	 @object_type= 50,
	@PARAMETER_NAME=N'SYNCHRONIZED',
	@PARAMETER_VALUE=1; -- TRUE

EXEC  @RC = [SSISDB].[catalog].[set_execution_parameter_value] 
        @execution_id,  
        @object_type=50, 
        @parameter_name=N'LOGGING_LEVEL', 
        @parameter_value= 2; -- Basic
PRINT '[set_execution_parameter_value]'+ CAST(@RC AS VARCHAR(10))

PRINT '[set_execution_parameter_value]'+ CAST(@RC AS VARCHAR(10))
EXEC @RC = [SSISDB].[catalog].[start_execution]
	@execution_id
	PRINT '[start_execution]'+ CAST(@RC AS VARCHAR(10))

	if exists(select 1 from [SSISDB].[catalog].[executions] where execution_id=@execution_id and [status]=4) 
    BEGIN
			RAISERROR('Package Execution Failed',16,1);
	end
GO


