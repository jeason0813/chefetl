SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

RAISERROR('==========================================================================================',0,1) WITH NOWAIT
IF OBJECT_ID('CHEF.vwProcessTypes','V') IS NOT NULL
BEGIN
	RAISERROR('Dropping View CHEF.vwProcessTypes',0,1) WITH NOWAIT
	DROP VIEW CHEF.vwProcessTypes
	RAISERROR('Dropped View CHEF.vwProcessTypes',0,1) WITH NOWAIT
END
RAISERROR('Creating View CHEF.vwProcessTypes',0,1) WITH NOWAIT
GO
/* ==========================================================================================
	File		: CHEF.vwProcessTypes.sql
	Name		: CHEF.vwProcessTypes
	Author		: balajim
	Date		: 21st Sep 2010
	Description	: Return the Process Types used in CHEF viz., Staging, Warehouse, ReportMart
	
	Expected Resultset Sample:
	ProcessTypeID		ProcessTypeName
		1				Staging
		2				Warehouse
		3				ReportMart

	Unit Test:
		1.		SELECT * FROM CHEF.vwProcessTypes
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
CREATE VIEW CHEF.vwProcessTypes
AS

SELECT CAST(ProcessTypeID as tinyint) AS ProcessTypeID, CAST(ProcessTypeName as varchar(25)) AS ProcessTypeName 
FROM (VALUES(1, 'Staging'),(2, 'Warehouse'),(3, 'ReportMart')) AS ProcessTypes(ProcessTypeID, ProcessTypeName)

GO									  

IF OBJECT_ID('CHEF.vwProcessTypes','V') IS NOT NULL
	RAISERROR('Created View CHEF.vwProcessTypes',0,1) WITH NOWAIT
ELSE
	RAISERROR('Failed to Create View CHEF.vwProcessTypes',0,1) WITH NOWAIT
RAISERROR('==========================================================================================',0,1) WITH NOWAIT
GO

