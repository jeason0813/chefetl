SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

/* ==========================================================================================
	File				: CHEF.LogArchive.sql
	Name			: CHEF.LogArchive
	Author		: balajim
	Date			: 2nd-Aug-2010
	Description	: Create the Table structure for storing CHEF LogArchives that would be syphoned on a continuous basis from the CHEF.Log table
						   This table would be created as part of the Deployment Script into the selected Database where CHEF resources would be maintained
						   This table may be partitioned by Year by the respective application to optimize the data retrieval
	
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
		1.	IF OBJECT_ID('CHEF.LogArchive','U') IS NOT NULL
				drop table CHEF.LogArchive;
		2.   

	Verify:
		1.	exec  sp_help 'CHEF.LogArchive'

		2. select * from sys.tables where name = 'LogArchive' and object_schema_name(object_id) = 'CHEF'
			select * from sys.indexes where object_id = object_id('CHEF.LogArchive','U') 

			--find all the columns in indexes 
		3.	select object_name(i.object_id) TableName, i.name IndexName,c.name ColumnName,ic.* 
			from sys.indexes i
			join sys.index_columns ic on i.index_id = ic.index_id and i.object_id = ic.object_id
			join sys.columns c on ic.column_id = c.column_id and ic.object_id=c.object_id 
			where i.object_id = object_id('chef.LogArchive','U') 
			
	Note:
		1.	
		
	Change History
	-------- --------- -----------------------------------------------------------------------
	Date		Name            Description
	-------- --------- -----------------------------------------------------------------------
	
========================================================================================== */

DECLARE 
	 @TableName sysname = 'CHEF.LogArchive'
	,@IndexName sysname = 'IX_CHEF_LogArchive_QueueID'
	,@DBName sysname = DB_NAME()
	,@Dt varchar(100) = CONVERT(varchar(100),GETDATE(),109)
	
RAISERROR('-------------------------------------------------------Table %s ---------------------------------------------------------------------',0,1,@TableName) WITH NOWAIT;

--drop and create the table 
IF OBJECT_ID(@TableName,'U') IS NOT NULL
BEGIN
	RAISERROR('Dropping table "%s" from Database %s; %s',0,1,@TableName,@DBName,@Dt) WITH NOWAIT;

	DROP TABLE CHEF.[LogArchive];

	RAISERROR('Dropped table "%s" from Database %s; %s',0,1,@TableName,@DBName,@Dt) WITH NOWAIT;
END

BEGIN
	RAISERROR('Creating table "%s" in Database %s; %s',0,1,@TableName,@DBName,@Dt) WITH NOWAIT;

	CREATE TABLE CHEF.[LogArchive](
		 [LogID] bigint NOT NULL							--this is expected to contain the LogIDs as syphoned from Log table
		,[QueueID] int NOT NULL 
		,[ProcessStep] varchar(255) NOT NULL	--Unique for ProcessID e.g.,Load_ACCCAT1010,Load_EXRATE01Currency,Truncate_ACCCAT1010,Update_ACCCAT1010
		,[ProcessDate] datetime NOT NULL			--e.g., 2009-09-14 00:14:78 245 hrs
		,[StatusID] [tinyint] NOT NULL					--e.g., 1:Started, 2:Finished, 3:Stopped, 4:Failed 
		,[RowsAffected] bigint NOT NULL			--e.g., 100  --At the Start it is estimated rows and at the Complete/Fail it is Actual rows
		,[Description] varchar(500) NOT NULL	--e.g., Load data to dbo.AdjustmentTypeHierarchy from FeedStore Started. Estimated Rows(100).
	)

	RAISERROR('Created table "%s" in Database %s; %s',0,1,@TableName,@DBName,@Dt) WITH NOWAIT;
END

--drop and create the index on ProcessStatus
BEGIN
	RAISERROR('Creating index "%s" on table %s; %s',0,1,@IndexName,@TableName,@Dt) WITH NOWAIT;

	CREATE UNIQUE INDEX IX_CHEF_LogArchive_QueueID ON CHEF.[LogArchive](QueueID,LogID) INCLUDE(ProcessStep, StatusID, ProcessDate);

	RAISERROR('Created index "%s" on table %s; %s',0,1,@IndexName,@TableName,@Dt) WITH NOWAIT;
END

--being historical table, more likely to be queries on Date, may be dropped if the usage pattern does not pick this
--drop and create the index on RequestDate

SET @IndexName = 'IX_CHEF_LogArchive_ProcessDate'
BEGIN
	RAISERROR('Creating index "%s" on table %s; %s',0,1,@IndexName,@TableName,@Dt) WITH NOWAIT;

	CREATE UNIQUE INDEX IX_CHEF_LogArchive_ProcessDate ON CHEF.[LogArchive](ProcessDate,QueueID,LogID) INCLUDE(ProcessStep, StatusID);

	RAISERROR('Created index "%s" on table %s; %s',0,1,@IndexName,@TableName,@Dt) WITH NOWAIT;
END

RAISERROR('-------------------------------------------------------Table %s ---------------------------------------------------------------------',0,1,@TableName) WITH NOWAIT;
PRINT ''	--insert a blank line
GO