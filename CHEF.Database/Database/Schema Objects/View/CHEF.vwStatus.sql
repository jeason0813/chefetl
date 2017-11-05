SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

RAISERROR('==========================================================================================',0,1) WITH NOWAIT
IF OBJECT_ID('CHEF.vwStatus','V') IS NOT NULL
BEGIN
	RAISERROR('Dropping View CHEF.vwStatus',0,1) WITH NOWAIT
	DROP VIEW CHEF.vwStatus
	RAISERROR('Dropped View CHEF.vwStatus',0,1) WITH NOWAIT
END
RAISERROR('Creating View CHEF.vwStatus',0,1) WITH NOWAIT
GO
/* ==========================================================================================
	File		: CHEF.vwStatus.sql
	Name		: CHEF.vwStatus
	Author		: balajim
	Date		: 17th Sep 2010
	Description	: Return the Process Status used in CHEF viz., Log and Queue
	
	Expected Resultset Sample:
	StatusID	StatusName
		0		Queued
		1		Started
		2		Finished
		3		Stopped
		4		Failed

	Unit Test:
		1.		SELECT * FROM CHEF.vwStatus
		2.	

	Verify:
		1.	
		2.	
	
	Note:
		1
		2.	
		
	Change History
	-------- --------- -----------------------------------------------------------------------
	Date		Name            Description
	-------- --------- -----------------------------------------------------------------------
	23-Sep-2010	balajim		Added the datatypes exclusively
========================================================================================== */
CREATE VIEW CHEF.vwStatus
AS

SELECT CAST(StatusID as tinyint) AS StatusID, CAST(StatusName as varchar(25)) AS StatusName
FROM (VALUES(0, 'Queued'),(1, 'Started'),(2, 'Finished'),(3, 'Stopped'),(4, 'Failed')) AS ProcessStatus(StatusID, StatusName)

GO									  

IF OBJECT_ID('CHEF.vwStatus','V') IS NOT NULL
	RAISERROR('Created View CHEF.vwStatus',0,1) WITH NOWAIT
ELSE
	RAISERROR('Failed to Create View CHEF.vwStatus',0,1) WITH NOWAIT
RAISERROR('==========================================================================================',0,1) WITH NOWAIT
GO

