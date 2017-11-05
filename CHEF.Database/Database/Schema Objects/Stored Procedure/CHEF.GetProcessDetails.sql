IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[CHEF].[GetProcessDetails]') AND type in (N'P', N'PC'))
DROP PROCEDURE [CHEF].[GetProcessDetails]
GO


SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


/* ==========================================================================================
	File			: CHEF.GetProcessDetails.sql
	Name			: CHEF.GetProcessDetails
	Author			: kaicho
	Date			: 26th-Aug-2010
	Description		: Create the proc to get status from RequestQueue
					  This procedure returns the Processes and its Steps apart from the Execution Status.
					  Sample Result set should be as follows 	

ProcessID	ProcessName			StartStepID	StepTypeName	RequestStatus	StatusName		RequestedDate			PreviousRequestStatus	PreviousStatusName	PreviousRequestedDate
1010		TestDB_Stage		10100		Staging				0			Queued			2010-09-21 13:49:59.230		2					Finished			2010-09-21 11:55:20.067
1010		TestDB_Stage		10200		WareHouse			5			NotRunning		NULL						6					NA					NULL
1020		TestDB_Warehouse	20100		Staging				1			Started			2010-09-21 13:48:38.900		6					NA					NULL
1020		TestDB_Warehouse	20200		WareHouse			5			NotRunning		NULL						6					NA					NULL
1020		TestDB_Warehouse	20310		ReportMart			5			NotRunning		NULL						6					NA					NULL
	
	UNIT TEST CASE:
	1. EXEC CHEF.GetProcessDetails	@year = 2010, @month = 10	--success case
	
	2. EXEC CHEF.GetProcessDetails	@year = 2099, @month = 9 --non existstant case
	
	Change History
	-------- --------- -----------------------------------------------------------------------
	Date		Name            Description
	21-Sep-2010	balajim			Changed the procedure to use the chef.vwProcessSteps and Return the 1st StartStepID of the respective Type instead of TypeID for the Step
								Updated descriptions. Corrected the unit test case sample.
								Made the input Parameters mandatory
								Added steps to return the Previous Run Status for the respective ProcessID
	23-Sep-2010	balajim			Corrected the datatype for status and updated the filename & proc name in description
	14-Oct-2010 balajim			Changed the query to fetch the Previous Status in the order StartStepID, ProcessType, Process as Types/Steps within a 
								Process may be run multiple times. Optimized the query.
	20-Oct-2010	balajim			Fixed the issue of not returning data for year/month not there in RequestQueue
	18-01-2011	Ramsingh		workbench UI don't show the Q or Finish process for Jan
*/
	
CREATE PROCEDURE CHEF.GetProcessDetails
	@Year smallint,		--CalendarYear
	@Month tinyint		--CalendarMonth
AS

--Need to remove the below code modified the ui code 
-------------------
if(@Month=0)
	set @Month=1
--------------------
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
,ProcessQueue	--Fetch the Queue(s) status by StartStepID, StepTypeName & ProcessID for the requested year and month
AS
(
    SELECT 
		 ROW_NUMBER() OVER (PARTITION BY ps.StartStepID, ps.StepTypeName, ps.ProcessID ORDER BY rq.QueueID DESC) AS RowNo
		,rq.ProcessID
		,rq.QueueID
		,rq.RequestStatus
		,rq.RequestedDate
		,s.StatusName
		,rq.StartStepID
      FROM chef.RequestQueue rq 
INNER JOIN CHEF.vwStatus s
        ON rq.RequestStatus = s.StatusID
INNER JOIN MinProcessStep ps 
        ON ps.ProcessID = rq.ProcessID
       AND ps.StartStepID = rq.StartStepID       
     WHERE rq.CalendarYear = @Year
       AND rq.CalendarMonth = @Month
)
	  SELECT 
		 ps.ProcessID
		,ps.ProcessName
		,mps.StartStepID 
		,ps.TypeName AS StepTypeName
		,ISNULL(cs.RequestStatus, 5) AS RequestStatus	--ID:5 is used for reference by the UI only
		,ISNULL(cs.StatusName, 'NotRunning') AS StatusName
		,cs.RequestedDate
		,ISNULL(pcs.RequestStatus, 6) AS PreviousRequestStatus	--ID:6 is used for reference by the UI only
		,ISNULL(pcs.StatusName, 'NA') AS PreviousStatusName
		,pcs.RequestedDate AS PreviousRequestedDate
	  FROM #ProcessSteps ps
INNER JOIN MinProcessStep mps
        ON ps.ProcessID = mps.ProcessID
       AND ps.StepID = mps.StartStepID
       AND ps.TypeID = mps.StepTypeID
 LEFT JOIN ProcessQueue cs
        ON ps.ProcessID = cs.ProcessID
	   AND cs.StartStepID = ps.StepID
       AND cs.RowNo = 1		--fetch the Current QueueID for the ProcessID
 LEFT JOIN ProcessQueue pcs
        ON ps.ProcessID = pcs.ProcessID
	   AND pcs.StartStepID = ps.StepID
       AND pcs.RowNo = 2	--fetch the Previous QueueID for the ProcessID
GO