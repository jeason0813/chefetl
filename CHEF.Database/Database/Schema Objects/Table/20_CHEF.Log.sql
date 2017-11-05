SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

/* ==========================================================================================
	File				: 20_CHEF.Log.sql
	Name			: CHEF.Log
	Author		: balajim
	Date			: 2nd-Aug-2010
	Description	: Create the Table structure for storing CHEF Logs that would be generated during the Data Loading
						   This table would be created as part of the Deployment Script into the selected Database where CHEF resources would be maintained
	
	The data in this table for any Process would be stored in a inward hierarchical format e.g.,
	OPUS Load Started
		Step-1: Load Stage Started
			Step-1_1: Truncate dbo.abc Table Started
			Step-1_1: Truncate dbo.abc Table Finished
			Step-1_2: Load dbo.abc Table Started
			Step-1_2: Load dbo.abc Table Finished
		Step-1: Load Stage Finished
		Step-2: Load Warehouse Started
			Step-2_1: Delete dbo.abc Table Started
			Step-2_1: Delete dbo.abc Table Finished
			Step-2_2: Load dbo.abc Table Started
			Step-2_2: Load dbo.abc Table Finished
		Step-2: Load Warehouse Finished
	OPUS Load Finished 

	Unit Test:
		1.	IF OBJECT_ID('CHEF.Log','U') IS NOT NULL
				drop table CHEF.Log;
		2.   

	Verify:
		1.	exec  sp_help 'CHEF.Log'

		2. select * from sys.tables where name = 'Log' and object_schema_name(object_id) = 'CHEF'
			select * from sys.indexes where object_id = object_id('CHEF.Log','U') and name in ('IX_CHEF_Log_QueueID')

			--find all the columns in indexes 
		3.	select object_name(i.object_id) TableName, i.name IndexName,c.name ColumnName,ic.* 
			from sys.indexes i
			join sys.index_columns ic on i.index_id = ic.index_id and i.object_id = ic.object_id
			join sys.columns c on ic.column_id = c.column_id and ic.object_id=c.object_id 
			where i.object_id = object_id('chef.Log','U') 
			
	Note:
		1.	
		
	Change History
	-------- --------- -----------------------------------------------------------------------
	Date		Name            Description
	-------- --------- -----------------------------------------------------------------------
	
========================================================================================== */

DECLARE 
	 @TableName sysname = 'CHEF.Log'
	,@IndexName sysname = 'IX_CHEF_Log_QueueID'
	,@DBName sysname = DB_NAME()
	,@Dt varchar(100) = CONVERT(varchar(100),GETDATE(),109)
	
RAISERROR('-------------------------------------------------------Table %s ---------------------------------------------------------------------',0,1,@TableName) WITH NOWAIT;

--drop and create the table 
IF OBJECT_ID(@TableName,'U') IS NOT NULL
BEGIN
	RAISERROR('Dropping table "%s" from Database %s; %s',0,1,@TableName,@DBName,@Dt) WITH NOWAIT;

	DROP TABLE CHEF.[Log];

	RAISERROR('Dropped table "%s" from Database %s; %s',0,1,@TableName,@DBName,@Dt) WITH NOWAIT;
END

BEGIN
	RAISERROR('Creating table "%s" in Database %s; %s',0,1,@TableName,@DBName,@Dt) WITH NOWAIT;

	CREATE TABLE CHEF.[Log](
		 [LogID] int IDENTITY(1,1) NOT NULL		--this would be periodically reset using the configuration value
		,[QueueID] int NOT NULL CONSTRAINT FK_CHEF_Log_CHEF_RequestQueue FOREIGN KEY REFERENCES CHEF.RequestQueue(QueueID)         
		,[ProcessStep] varchar(255) NOT NULL	--Unique for ProcessID e.g.,Load_ACCCAT1010,Load_EXRATE01Currency,Truncate_ACCCAT1010,Update_ACCCAT1010
		,[ProcessDate] datetime NOT NULL CONSTRAINT DF_CHEF_RequestQueue_ProcessDate default GETDATE()        --e.g., 2009-09-14 00:14:78 245 hrs
		,[StatusID] tinyint NOT NULL					--e.g., 1:Started, 2:Finished, 3:Stopped, 4:Failed 
		,[RowsAffected] bigint NOT NULL CONSTRAINT DF_CHEF_RequestQueue_RowsAffected default 0			--e.g., 100  --At the Start it is estimated rows and at the Complete/Fail it is Actual rows
		,[Description] varchar(500) NOT NULL --e.g., Load data to dbo.AdjustmentTypeHierarchy from FeedStore Started. Estimated Rows(100).
	)

	RAISERROR('Created table "%s" in Database %s; %s',0,1,@TableName,@DBName,@Dt) WITH NOWAIT;
END

--drop and create the index on RequestStatus
BEGIN
	RAISERROR('Creating index "%s" on table %s; %s',0,1,@IndexName,@TableName,@Dt) WITH NOWAIT;

	CREATE UNIQUE INDEX IX_CHEF_Log_QueueID ON CHEF.[Log](QueueID,LogID) INCLUDE(ProcessStep, StatusID, ProcessDate);

	RAISERROR('Created index "%s" on table %s; %s',0,1,@IndexName,@TableName,@Dt) WITH NOWAIT;
END

RAISERROR('-------------------------------------------------------Table %s ---------------------------------------------------------------------',0,1,@TableName) WITH NOWAIT;
PRINT ''	--insert a blank line
GO