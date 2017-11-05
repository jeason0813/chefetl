SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON

RAISERROR('-------------------------------------------------------Procedure CHEF.GetProcessHistory ---------------------------------------------------------------------',0,1) WITH NOWAIT;
DECLARE @DBName sysname = DB_NAME()
RAISERROR('Dropping and Creating Stored procedure CHEF.GetProcessHistory in Database %s',0,1,@DBName) WITH NOWAIT
IF OBJECT_ID('CHEF.GetProcessHistory','P') IS NOT NULL
BEGIN
	RAISERROR('Dropping stored procedure CHEF.GetProcessHistory',0,1) WITH NOWAIT
	DROP PROCEDURE CHEF.GetProcessHistory
	RAISERROR('Dropped stored procedure CHEF.GetProcessHistory',0,1) WITH NOWAIT
END
RAISERROR('Creating stored procedure CHEF.GetProcessHistory',0,1) WITH NOWAIT
GO

/* ==========================================================================================
	File			: CHEF.GetProcessHistory.sql
	Name			: CHEF.GetProcessHistory
	Author			: balajim
	Date			: 14th-Oct-2010
	Description		: This procedure returns the Processes History and its Steps apart from the Execution Status.
					  Sample Result set should be as follows 	

ProcessID	ProcessName			StartStepID	StepTypeName	RequestStatus	StatusName		RequestedDate			PreviousRequestStatus	PreviousStatusName	PreviousRequestedDate
1010		TestDB_Stage		10100		Staging				0			Queued			2010-09-21 13:49:59.230		2					Finished			2010-09-21 11:55:20.067
1010		TestDB_Stage		10200		WareHouse			5			NotRunning		NULL						6					NA					NULL
1020		TestDB_Warehouse	20100		Staging				1			Started			2010-09-21 13:48:38.900		6					NA					NULL
1020		TestDB_Warehouse	20200		WareHouse			5			NotRunning		NULL						6					NA					NULL
1020		TestDB_Warehouse	20310		ReportMart			5			NotRunning		NULL						6					NA					NULL
	

	UNIT TEST CASE:
	1. EXEC CHEF.GetProcessHistory 
	   EXEC CHEF.GetProcessHistory @CalendarYear = 2010
	   EXEC CHEF.GetProcessHistory @CalendarYear = 2010, @CalendarMonth = 10
	   EXEC CHEF.GetProcessHistory @CalendarYear = 2010, @ProcessID = 2010
	   EXEC CHEF.GetProcessHistory @CalendarMonth = 9, @ProcessID = 2010
	   EXEC CHEF.GetProcessHistory @CalendarYear = 2010, @CalendarMonth = 10, @ProcessID = 1010
	

	Note: This is similar to CHEF.GetProcessDetails except that this brings the history and allows querying by ProcessID and without Year/Month
	      Besides fetching the FiscalMonth/FiscalYear

	Change History
	-------- --------- -----------------------------------------------------------------------
	Date		Name            Description


*/
	
CREATE PROCEDURE CHEF.GetProcessHistory
	 @CalendarYear smallint = NULL
	,@CalendarMonth tinyint = NULL
	,@ProcessID int = NULL
AS

--pull the data from the xml meta into a temp table, as this is used more than once, besides xml parsing is a costly operation
SELECT 
	 ProcessID 
	,ProcessName
	,StepID
	,StepName
	,TypeID
	,TypeName
INTO #ProcessSteps
FROM CHEF.vwProcessSteps ps
WHERE EXISTS(SELECT * 
			   FROM CHEF.RequestQueue 
			  WHERE ProcessID = ps.ProcessID 
				AND ProcessID = ISNULL(@ProcessID,ProcessID)
			)

;WITH MinProcessStep --Fetch the 1st StepID from each Process for the Respective Type
AS
(
  SELECT 
       ps.ProcessID
      ,ps.ProcessName
      ,MIN(ps.StepID) AS StartStepID
      ,ps.TypeID AS StepTypeID
      ,ps.TypeName AS StepTypeName
    FROM #ProcessSteps ps
GROUP BY 
       ps.ProcessID
      ,ps.ProcessName
      ,ps.TypeID
      ,ps.TypeName
)
--Fetch the Queue(s) status by StartStepID, StepTypeName & ProcessID for the requested year and month
    SELECT 
		 ROW_NUMBER() OVER (PARTITION BY ps.StartStepID, ps.StepTypeName, rq.CalendarYear, ps.ProcessID ORDER BY rq.QueueID DESC) AS ProcessSerialNo
		,rq.ProcessID
		,ps.ProcessName
		,ps.StepTypeName
		,rq.StartStepID
		,s.StatusName AS [Status]
		,rq.CalendarYear 
		,rq.CalendarMonth
		,rq.RequestedDate
		,s.StatusID
		,rq.QueueID
      FROM chef.RequestQueue rq 
INNER JOIN CHEF.vwStatus s
        ON rq.RequestStatus = s.StatusID
INNER JOIN MinProcessStep ps 
        ON ps.ProcessID = rq.ProcessID
       AND ps.StartStepID = rq.StartStepID       
	 WHERE 
		   rq.CalendarYear = ISNULL(@CalendarYear,rq.CalendarYear) 
	   AND rq.CalendarMonth = ISNULL(@CalendarMonth,rq.CalendarMonth)
	   AND rq.ProcessID = ISNULL(@ProcessID,rq.ProcessID)
  ORDER BY
		 ps.ProcessID
        ,ps.StepTypeName
		,rq.CalendarYear
		,ProcessSerialNo

GO
IF OBJECT_ID('CHEF.GetProcessHistory','P') IS NOT NULL
	RAISERROR('Created stored procedure CHEF.GetProcessHistory',0,1) WITH NOWAIT
ELSE
	RAISERROR('Failed to Create stored procedure CHEF.GetProcessHistory',0,1) WITH NOWAIT

RAISERROR('-------------------------------------------------------Procedure CHEF.GetProcessHistory ---------------------------------------------------------------------',0,1) WITH NOWAIT;
PRINT ''	--insert a blank line
GO
