SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

RAISERROR('-------------------------------------------------------Procedure CHEF.GetActiveQProcess ---------------------------------------------------------------------',0,1) WITH NOWAIT;
DECLARE @DBName sysname = DB_NAME()
RAISERROR('Dropping and Creating Stored procedure CHEF.GetActiveQProcess in Database %s',0,1,@DBName) WITH NOWAIT
IF OBJECT_ID('CHEF.GetActiveQProcess','P') IS NOT NULL
BEGIN
	RAISERROR('Dropping stored procedure CHEF.GetActiveQProcess',0,1) WITH NOWAIT
	DROP PROCEDURE CHEF.GetActiveQProcess
	RAISERROR('Dropped stored procedure CHEF.GetActiveQProcess',0,1) WITH NOWAIT
END
RAISERROR('Creating stored procedure CHEF.GetActiveQProcess',0,1) WITH NOWAIT
GO
/* ==========================================================================================
	File		: CHEF.GetActiveQProcess.sql
	Name		: CHEF.GetQActiveProcess
	Author		: RAMSINGH
	Date		: 18nd-Aug-2010
	Description	: Create the proc to SELECT THE ACTIVE PROCESS IN chef.RequestQ table
			
	Note:
		1.	No try catch as it is expected to throw error and crash which would be handled by the calling method
		
	Change History
	-------- --------- -----------------------------------------------------------------------
	Date		Name            Description
	-------- --------- -----------------------------------------------------------------------
	
========================================================================================== */
	
CREATE PROCEDURE [CHEF].[GetActiveQProcess]
	As
BEGIN
 SELECT 
	QueueID,
	CalendarMonth,
	CalendarYear,
	ProcessID, 
	StartStepID, 
	RequestStatus, 
	LineageID,
	RequestedBy,
	RequestedDate,
	ScheduledDate FROM  CHEF.RequestQueue WHERE RequestStatus=1
END
