SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

/* ==========================================================================================
	File				: 10_CHEF.RequestQueue.sql
	Name			: CHEF.RequestQueue
	Author		: balajim
	Date			: 21-Jul-2010
	Description	: Create the Table structure for storing CHEF Requests that would be queued and executed.
						   This table would be created as part of the Deployment Script into the selected Database where CHEF resources would be maintained
	
	Unit Test:
		1.	IF OBJECT_ID('CHEF.RequestQueue','U') IS NOT NULL
				drop table CHEF.RequestQueue;
		2.   

	Verify:
		1.	exec  sp_help 'CHEF.RequestQueue'

		2. select * from sys.tables where name = 'RequestQueue' and object_schema_name(object_id) = 'CHEF'
			select * from sys.indexes where object_id = object_id('chef.RequestQueue','U') and name in ('IX_CHEF_RequestQueue_RequestStatus','IX_CHEF_RequestQueue_RequestedDate')

			--find all the columns in indexes 
		3.	select object_name(i.object_id) TableName, i.name IndexName,c.name ColumnName,ic.* 
			from sys.indexes i
			join sys.index_columns ic on i.index_id = ic.index_id and i.object_id = ic.object_id
			join sys.columns c on ic.column_id = c.column_id and ic.object_id=c.object_id 
			where i.object_id = object_id('chef.RequestQueue','U') 
			
	Note:
		1.	
		
	Change History
	-------- --------- -----------------------------------------------------------------------
	Date		Name            Description
	-------- --------- -----------------------------------------------------------------------
	12-Aug-2010 ramsingh       added new column ScheduleDate  
	21-Sep-2010	balajim			Changed the suser_sname() to original_login() 
========================================================================================== */

DECLARE 
	 @TableName sysname = 'CHEF.RequestQueue'
	,@IndexName sysname = 'IX_CHEF_RequestQueue_RequestStatus'
	,@DBName sysname = DB_NAME()
	,@Dt varchar(100) = CONVERT(varchar(100),GETDATE(),109)
	
RAISERROR('-------------------------------------------------------Table %s ---------------------------------------------------------------------',0,1,@TableName) WITH NOWAIT;

--drop the FK if any (to be added later)
IF EXISTS(SELECT * FROM sys.foreign_keys WHERE name = 'FK_CHEF_Log_CHEF_RequestQueue' and parent_object_id = object_id('CHEF.Log','U'))
BEGIN
	RAISERROR('Dropping Foreign Key FK_CHEF_Log_CHEF_RequestQueue from table "%s"; %s',0,1,@TableName,@Dt) WITH NOWAIT;
	
	ALTER TABLE CHEF.[Log] DROP CONSTRAINT FK_CHEF_Log_CHEF_RequestQueue;

	RAISERROR('Dropped Foreign Key FK_CHEF_Log_CHEF_RequestQueue from table "%s"; %s',0,1,@TableName,@Dt) WITH NOWAIT;
END
--drop and create the table 
IF OBJECT_ID(@TableName,'U') IS NOT NULL
BEGIN
	RAISERROR('Dropping table "%s" from Database %s; %s',0,1,@TableName,@DBName,@Dt) WITH NOWAIT;

	DROP TABLE CHEF.RequestQueue;

	RAISERROR('Dropped table "%s" from Database %s; %s',0,1,@TableName,@DBName,@Dt) WITH NOWAIT;
END

BEGIN
	RAISERROR('Creating table "%s" in Database %s; %s',0,1,@TableName,@DBName,@Dt) WITH NOWAIT;

	CREATE TABLE CHEF.RequestQueue(
		 QueueID int identity(1,1) not null		--unique for every new Action Item Requested
		,CalendarMonth tinyint NOT NULL CONSTRAINT DF_CHEF_RequestQueue_CalendarMonth default MONTH(GETDATE())  --e.g., 1-Jan, 2-Feb        
		,CalendarYear smallint NOT NULL CONSTRAINT DF_CHEF_RequestQueue_CalendarYear default YEAR(GETDATE()) --e.g., 2009, 2008 
		,ProcessID smallint not null					--e.g.,1-OPUS Loading, 2-Excise Loading etc.; as it is in the CHEF XML metadata file, comes as per request from UI
		,StartStepID smallint not null	CONSTRAINT DF_CHEF_RequestQueue_StartStepID default 1		--as it is in the CHEF XML metdata file. By default all the steps in a process will be executed starting from 1, optionally it may be started from any other step
		,ProcessOption  XML(CHEF.RequestSteps) null		--as it is in the CHEF XML metdata file. By default all the steps in a process will be executed starting from 1, optionally it may be started from any other step
		,RequestStatus tinyint not null CONSTRAINT DF_CHEF_RequestQueue_RequestStatus default 0		-- 0:Queued, 1:Started, 2:Finished, 3:Stopped, 4:Failed
		,LineageID uniqueidentifier NOT NULL  CONSTRAINT DF_CHEF_RequestQueue_LineageID default NEWID() --e.g., 36CFA4E7-7248-49C6-8149-BE780C7ED808 
		,RequestedBy nvarchar(25) not null CONSTRAINT DF_CHEF_RequestQueue_SubmittedBy default LEFT(ORIGINAL_LOGIN(),25)
		,RequestedDate datetime not null CONSTRAINT DF_CHEF_RequestQueue_RequestedDate default GETDATE()
		,ScheduledDate datetime not null CONSTRAINT DF_CHEF_RequestQueue_ScheduledDate default GETDATE()
		,CONSTRAINT PK_CHEF_RequestQueue PRIMARY KEY (QueueID)
	)

	RAISERROR('Created table "%s" in Database %s; %s',0,1,@TableName,@DBName,@Dt) WITH NOWAIT;
END

--NB: The index drop section is required only if the script for table and index needs to be separated, otherwise these gets dropped alongwith the table
--drop and create the index on RequestStatus
IF EXISTS(SELECT * FROM sys.indexes WHERE name=@IndexName AND object_id = OBJECT_ID(@TableName))
BEGIN
	RAISERROR('Dropping index "%s" on table %s; %s',0,1,@IndexName,@TableName,@Dt) WITH NOWAIT;

	DROP INDEX 	CHEF.RequestQueue.IX_CHEF_RequestQueue_RequestStatus;

	RAISERROR('Dropped index "%s" on table %s; %s',0,1,@IndexName,@TableName,@Dt) WITH NOWAIT;
END

BEGIN
	RAISERROR('Creating index "%s" on table %s; %s',0,1,@IndexName,@TableName,@Dt) WITH NOWAIT;

	CREATE INDEX IX_CHEF_RequestQueue_RequestStatus ON CHEF.RequestQueue(RequestStatus) INCLUDE(ProcessID,StartStepID,QueueID);

	RAISERROR('Created index "%s" on table %s; %s',0,1,@IndexName,@TableName,@Dt) WITH NOWAIT;
END

--drop and create the index on RequestedDate
SET @IndexName = 'IX_CHEF_RequestQueue_RequestedDate'

IF EXISTS(SELECT * FROM sys.indexes WHERE name=@IndexName AND object_id = OBJECT_ID(@TableName))
BEGIN
	RAISERROR('Dropping index "%s" on table %s; %s',0,1,@IndexName,@TableName,@Dt) WITH NOWAIT;

	DROP INDEX 	CHEF.RequestQueue.IX_CHEF_RequestQueue_RequestStatus;

	RAISERROR('Dropped index "%s" on table %s; %s',0,1,@IndexName,@TableName,@Dt) WITH NOWAIT;
END

BEGIN
	RAISERROR('Creating index "%s" on table %s; %s',0,1,@IndexName,@TableName,@Dt) WITH NOWAIT;

	CREATE INDEX IX_CHEF_RequestQueue_RequestedDate ON CHEF.RequestQueue(RequestedDate) INCLUDE(ProcessID,StartStepID,QueueID,RequestStatus);

	RAISERROR('Created index "%s" on table %s; %s',0,1,@IndexName,@TableName,@Dt) WITH NOWAIT;
END

RAISERROR('-------------------------------------------------------Table %s ---------------------------------------------------------------------',0,1,@TableName) WITH NOWAIT;
PRINT ''	--insert a blank line
GO