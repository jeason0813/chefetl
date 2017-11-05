SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

RAISERROR('==========================================================================================',0,1) WITH NOWAIT
IF OBJECT_ID('CHEF.vwMetadataTypes','V') IS NOT NULL
BEGIN
	RAISERROR('Dropping View CHEF.vwMetadataTypes',0,1) WITH NOWAIT
	DROP VIEW CHEF.vwMetadataTypes
	RAISERROR('Dropped View CHEF.vwMetadataTypes',0,1) WITH NOWAIT
END
RAISERROR('Creating View CHEF.vwMetadataTypes',0,1) WITH NOWAIT
GO
/* ==========================================================================================
	File		: CHEF.vwMetadataTypes.sql
	Name		: CHEF.vwMetadataTypes
	Author		: balajim
	Date		: 21st Sep 2010
	Description	: Return the Metadata Types used in CHEF viz., --0: Process, 1: GlobalConfig, 2 : XSD
	
	Expected Resultset Sample:
	MetadataTypeID		MetadataTypeName
		0				Staging
		1				Warehouse
		2				ReportMart

	Unit Test:
		1.		SELECT * FROM CHEF.vwMetadataTypes
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
CREATE VIEW CHEF.vwMetadataTypes
AS

SELECT CAST(MetadataTypeID as tinyint) AS MetadataTypeID, CAST(MetadataTypeName as varchar(25)) AS MetadataTypeName
FROM (VALUES(0, 'Process'),(1, 'GlobalConfig'),(2, 'XSD')) AS ProcessTypes(MetadataTypeID, MetadataTypeName)

GO									  

IF OBJECT_ID('CHEF.vwMetadataTypes','V') IS NOT NULL
	RAISERROR('Created View CHEF.vwMetadataTypes',0,1) WITH NOWAIT
ELSE
	RAISERROR('Failed to Create View CHEF.vwMetadataTypes',0,1) WITH NOWAIT
RAISERROR('==========================================================================================',0,1) WITH NOWAIT
GO

