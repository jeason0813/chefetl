SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
/* ==========================================================================================
	File			: CHEF.MetaData.sql
	Name			: CHEF.MetaData
	Author			: ramsingh
	Date			: 21-Jul-2010
	Description		: Create the Table structure for storing MetaData XML File and Releted XSD file.
						   This table would be created as part of the Deployment Script into the selected Database where CHEF resources would be maintained
	
	Unit Test:
		1.	IF OBJECT_ID('CHEF.MetaData','U') IS NOT NULL
				drop table CHEF.MetaData;
		2.   

	Verify:
		1.	exec  sp_help 'CHEF.MetaData'

		2. select * from sys.tables where name = 'MetaData' and object_schema_name(object_id) = 'CHEF'
			select * from sys.indexes where object_id = object_id('chef.MetaData','U') and name in ('IX_CHEF_MetaData_ProcessName','IX_CHEF_RequestQueue_ProcessName')

			--find all the columns in indexes 
		3.	select object_name(i.object_id) TableName, i.name IndexName,c.name ColumnName,ic.* 
			from sys.indexes i
			join sys.index_columns ic on i.index_id = ic.index_id and i.object_id = ic.object_id
			join sys.columns c on ic.column_id = c.column_id and ic.object_id=c.object_id 
			where i.object_id = object_id('chef.MetaData','U') 
			
	Note:
		1.	
		
	Change History
	-------- --------- -----------------------------------------------------------------------
	Date		Name            Description
	-------- --------- -----------------------------------------------------------------------
	17-09-2010	balajim		Renamed the file. Removed the identity column. 
							Combned the constraints into the table definition.
							Added the examples and best practices for data in the table
========================================================================================== */

DECLARE 
	  @TableName sysname = 'CHEF.MetaData'
	 ,@DBName sysname = DB_NAME()
	 ,@Dt varchar(100) = CONVERT(varchar(100),GETDATE(),109)
--drop and create the table 
IF OBJECT_ID(@TableName,'U') IS NOT NULL
BEGIN
	RAISERROR('Dropping table "%s" from Database %s; %s',0,1,@TableName,@DBName,@Dt) WITH NOWAIT;

	DROP TABLE CHEF.MetaData;

	RAISERROR('Dropped table "%s" from Database %s; %s',0,1,@TableName,@DBName,@Dt) WITH NOWAIT;
END

BEGIN
	RAISERROR('Creating table "%s" in Database %s; %s',0,1,@TableName,@DBName,@Dt) WITH NOWAIT;

CREATE TABLE [CHEF].[MetaData](
	[ProcessID] [smallint] NOT NULL,		--Best Practice to have No.s. with some gap e.g., 1010, 1020, 1030 etc, so that it allows to insert any new Process in future in between
	[ProcessName] [varchar](100) NOT NULL,	--Provide Names without space
	[MetaData] [xml] NOT NULL,				--there is an XSD to validate the metadata for each XML 
	[Type] [tinyint] NOT NULL,				--0: Process, 1: GlobalConfig, 2 : XSD
	[CreatedBy] [varchar](25) NOT NULL CONSTRAINT [DF_MetaData_CreatedBy]  DEFAULT (LEFT(SUSER_SNAME(),(25))),
	[CreatedDate] [datetime] NOT NULL CONSTRAINT [DF_MetaData_CreatedDate]  DEFAULT (GETDATE()),
	[UpdatedBy] [varchar](25) NOT NULL CONSTRAINT [DF_MetaData_UpdatedBy]  DEFAULT (LEFT(SUSER_SNAME(),(25))),
	[UpdatedDate] [datetime] NOT NULL CONSTRAINT [DF_MetaData_UpdatedDate]  DEFAULT (GETDATE()),
	CONSTRAINT [PK_MetaData] PRIMARY KEY CLUSTERED 
	(
		[ProcessID] ASC
	) ON [PRIMARY]
) ON [PRIMARY]

	RAISERROR('Created table "%s" in Database %s; %s',0,1,@TableName,@DBName,@Dt) WITH NOWAIT;
END
GO

