SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

RAISERROR('==========================================================================================',0,1) WITH NOWAIT
IF OBJECT_ID('CHEF.DataLoadStatus','IF') IS NOT NULL
BEGIN
	RAISERROR('Dropping Tabular User Defined Function CHEF.DataLoadStatus',0,1) WITH NOWAIT
	DROP FUNCTION CHEF.DataLoadStatus
	RAISERROR('Dropped Tabular User Defined Function CHEF.DataLoadStatus',0,1) WITH NOWAIT
END
RAISERROR('Creating Tabular User Defined Function CHEF.DataLoadStatus',0,1) WITH NOWAIT
GO
/* ==========================================================================================
	File			: CHEF.DataLoadStatus.sql
	Name		: CHEF.DataLoadStatus
	Author	: balajim
	Date		: 3rd Aug 2010
	Description	: Return the data from the CHEF.Log table in a user friendly manner
	
	Expected Resultset Sample:
	CalendarYear CalendarMonth ETLName	ETLTask	ProcessStartDate	ProcessEndDate	ProcessStatus	SourceRows	TargetRows	Description UserName
	2009	9	Stage1_Loading	Load_ACCCAT1010	9/14/2009 0:23	9/14/2009 0:23	Completed	100	100	Load data to dbo.ACCCAT1010 from FeedStore Completed. Actual Rows affected(100).	fareast\balajim
	2009	9	Stage1_Loading	Load_EXRATE01Currency	9/14/2009 0:23	9/14/2009 0:23	Failed	100	90	Load data to dbo.EXRATE01Currency from FeedStore Failed. Actual Rows affected(90). ErrorDetails: ErrorNo-50001, Actual Row Count is different from Source Row Count.	fareast\balajim
	2009	9	Stage1_Loading	Load_CAWN1010	9/14/2009 0:23	NULL	Started	100	NULL	Load data to dbo.CAWN1010 from FeedStore Started.	fareast\balajim
	2009	9	Stage1_Loading	Load_EXRATE01Currency	9/14/2009 0:35	9/14/2009 0:38	Completed	100	100	Load data to dbo.EXRATE01Currency from FeedStore Completed. Actual Rows affected(100).	fareast\balajim

	Unit Test:
		1.		SELECT * FROM CHEF.DataLoadStatus(NULL,NULL,NULL) 
				SELECT * FROM CHEF.DataLoadStatus(2009,1,NULL) 
				SELECT * FROM CHEF.DataLoadStatus(2010,NULL,NULL) 

		2.	Few Advanced Queries
			SELECT DATEDIFF(ss,ProcessStartDate,ProcessEndDate) AS Duration,* 
			FROM CHEF.DataLoadStatus(2010,8,1) 
			ORDER BY Duration DESC

			SELECT DATEDIFF(ss,ProcessStartDate,ProcessEndDate) AS Duration,
			(CASE WHEN SourceRows<>TargetRows THEN 'Failed' ELSE 'Success' END) AS DataLoadStatus,* 
			FROM CHEF.DataLoadStatus(2010,8,1) 
			ORDER BY Duration DESC

	Verify:
		1.	For data insert
			SELECT * FROM CHEF.Log
			SELECT * FROM CHEF.RequestQueue
			
		2.	
	
	Note:
		1.	Few additional columns of derived information is commented out which may be turned on later for more detailed quick analysis
		2.	
		
	Change History
	-------- --------- -----------------------------------------------------------------------
	Date			Name            Description
	--------		---------	-----------------------------------------------------------------------
	28th Sep 2010	balajim		Added additional columns from RequestQueue viz., QueueID, ProcessID and StartStepID into the resultset
								Renamed the Columns StartProcessID & EndProcessID to StartLogID and EndLogID respectively, as these would confuse with RequestQueue.ProcessID
========================================================================================== */
CREATE FUNCTION CHEF.DataLoadStatus(@CalendarYear smallint = NULL,@CalendarMonth tinyint = NULL,@QueueID int = NULL)
RETURNS TABLE
AS
RETURN
(
	WITH LastStatus
	AS
	(
		SELECT 
			   rq.CalendarYear
			  ,rq.CalendarMonth
			  ,rq.LineageID
			  ,tl.ProcessStep AS ETLTask
			  ,rq.QueueID
			  ,rq.ProcessID
			  ,rq.StartStepID
			  ,MAX(tl.StatusID) AS StatusID
			  ,MIN(tl.LogID) AS StartLogID
			  ,MAX(tl.LogID) AS EndLogID
		  FROM CHEF.[Log] tl
		  JOIN CHEF.RequestQueue rq
			ON tl.QueueID = rq.QueueID 
		 WHERE rq.CalendarYear = (CASE WHEN @CalendarYear IS NULL THEN rq.CalendarYear ELSE @CalendarYear END)
		   AND rq.CalendarMonth = (CASE WHEN @CalendarMonth IS NULL THEN rq.CalendarMonth ELSE @CalendarMonth END)
		   AND tl.QueueID = (CASE WHEN @QueueID IS NULL THEN tl.QueueID ELSE @QueueID END)
		 GROUP BY 
			 rq.CalendarYear
			,rq.CalendarMonth
			,rq.LineageID
			,tl.ProcessStep
			,rq.QueueID
			,rq.ProcessID
			,rq.StartStepID
	)
	SELECT
		 ls.CalendarYear
		,ls.CalendarMonth
		,ls.LineageID
		,ls.ProcessID
		,ls.QueueID
		,ls.StartStepID
		,m.ProcessName
		,ls.ETLTask
		,tl1.ProcessDate AS ProcessStartDate
		,(CASE WHEN ls.StartLogID = ls.EndLogID THEN NULL ELSE tl2.ProcessDate END) AS ProcessEndDate
		,(CASE tl2.StatusID 
			WHEN 1 THEN 'Started' 
			WHEN 2 THEN 'Finished'
			WHEN 3 THEN 'Stopped'
			ELSE 'Failed'
		 END) AS ProcessStatus
		,tl1.RowsAffected AS SourceRows
		,tl2.RowsAffected AS TargetRows
		,tl2.[Description]
		,(SELECT MAX(RequestedBy) FROM CHEF.RequestQueue WHERE QueueID = tl2.QueueID) AS UserName
		,CONVERT(varchar(25), tl1.ProcessDate, 106) AS ProcessDate
		,CONVERT(varchar(25), tl1.ProcessDate, 108) AS ProcessStartTime
		,(CASE WHEN ls.StartLogID = ls.EndLogID THEN NULL ELSE CONVERT(varchar(25), tl2.ProcessDate,108) END) AS ProcessEndTime	--if the IDs are same means the process has not finished
		,tl2.Description AS LastDescription
		,tl1.Description AS StartDescription
		,ls.StartLogID AS StartLogID  --can be used for order by 
	  FROM LastStatus ls
INNER JOIN CHEF.[Log] tl1
	    ON ls.StartLogID = tl1.LogID
INNER JOIN CHEF.[Log] tl2
	    ON ls.EndLogID = tl2.LogID
INNER JOIN CHEF.Metadata m
		ON ls.ProcessID = m.ProcessID
);
GO									  


IF OBJECT_ID('CHEF.DataLoadStatus','IF') IS NOT NULL
	RAISERROR('Created Tabular User Defined Function CHEF.DataLoadStatus',0,1) WITH NOWAIT
ELSE
	RAISERROR('Failed to Create Tabular User Defined Function CHEF.DataLoadStatus',0,1) WITH NOWAIT
RAISERROR('==========================================================================================',0,1) WITH NOWAIT
GO

