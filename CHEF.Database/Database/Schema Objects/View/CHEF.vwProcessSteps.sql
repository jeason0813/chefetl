SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON

RAISERROR('==========================================================================================',0,1) WITH NOWAIT
IF OBJECT_ID('CHEF.vwProcessSteps','V') IS NOT NULL
BEGIN
	RAISERROR('Dropping View CHEF.vwProcessSteps',0,1) WITH NOWAIT
	DROP VIEW CHEF.vwProcessSteps
	RAISERROR('Dropped View CHEF.vwProcessSteps',0,1) WITH NOWAIT
END
RAISERROR('Creating View CHEF.vwProcessSteps',0,1) WITH NOWAIT
GO
/* ==========================================================================================
	File		: CHEF.vwProcessSteps.sql
	Name		: CHEF.vwProcessSteps
	Author		: balajim
	Date		: 17th Sep 2010
	Description	: Return the Process & its Steps from the CHEF.Metadata table for the Process XML file
	
	Expected Resultset Sample: e.g.,
	ProcessID	ProcessName	StepID	StepName						TypeID	TypeName
	1010	TestDB_Stage	10100	Load_PartitionedTblWithoutPK	1		Staging
	1010	TestDB_Stage	10200	WLoad_PartitionedTblWithoutPK	2		WareHouse

	Unit Test:
		1.		SELECT * FROM CHEF.vwProcessSteps
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
	23-Sep-2010	balajim		Corrected the datatype
	12-May-2011 balajim		Added Null handle
========================================================================================== */
CREATE VIEW CHEF.vwProcessSteps
AS

SELECT 
   ProcessID
  ,ProcessName 
  ,c.value('@ID','smallint') AS StepID
  ,c.value('@Name','varchar(255)') AS StepName
  ,ISNULL(c.value('@TypeID','tinyint'),1) AS TypeID 
  ,ISNULL(c.value('@TypeName','varchar(255)'),'Staging') AS TypeName
FROM CHEF.MetaData
CROSS APPLY MetaData.nodes('/CHEFMetaData/Process/Step') T(c)
WHERE [Type] = 0 --Process Metadata

GO									  

IF OBJECT_ID('CHEF.vwProcessSteps','V') IS NOT NULL
	RAISERROR('Created View CHEF.vwProcessSteps',0,1) WITH NOWAIT
ELSE
	RAISERROR('Failed to Create View CHEF.vwProcessSteps',0,1) WITH NOWAIT
RAISERROR('==========================================================================================',0,1) WITH NOWAIT
GO

