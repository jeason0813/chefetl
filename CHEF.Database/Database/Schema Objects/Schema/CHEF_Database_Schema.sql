SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

/* ==========================================================================================
	File				: CHEF_Database_Schema.sql
	Name			: CHEF
	Author		: balajim
	Date			: 02-Aug-2010
	Description	: Create the CHEF schema that would be used for all the CHEF related objects to maintain CHEF resources
	
	Unit Test:
		1.	if exists(SELECT * FROM sys.schemas WHERE name =  'CHEF')
				drop schema CHEF
		2. SELECT * FROM sys.schemas WHERE name =  'CHEF'  

	Verify:
		1.	SELECT * FROM sys.schemas 
		2. 
	
	Note:
		1.	
		
	Change History
	-------- --------- -----------------------------------------------------------------------
	Date		Name            Description
	-------- --------- -----------------------------------------------------------------------
	
========================================================================================== */

DECLARE 
	 @SchemaName sysname = 'CHEF'
	,@DBName sysname = DB_NAME()
	,@Dt varchar(100) = CONVERT(varchar(100),GETDATE(),109)
	
RAISERROR('-------------------------------------------------------Schema %s ---------------------------------------------------------------------',0,1,@SchemaName) WITH NOWAIT;
--create the schema if not exists
IF NOT EXISTS(SELECT * FROM sys.schemas WHERE name =  @SchemaName)
BEGIN
	RAISERROR('Creating Schema "%s" in Database %s; %s.',0,1,@SchemaName,@DBName,@Dt) WITH NOWAIT;

	EXEC('CREATE SCHEMA CHEF')
	
	RAISERROR('Created Schema "%s" in Database %s; %s ',0,1,@SchemaName,@DBName,@Dt) WITH NOWAIT;
END
ELSE
	RAISERROR('The Schema "%s" already exists in Database %s; %s',0,1,@SchemaName,@DBName,@Dt) WITH NOWAIT;

RAISERROR('-------------------------------------------------------Schema %s ---------------------------------------------------------------------',0,1,@SchemaName) WITH NOWAIT;
PRINT ''	--insert a blank line

SET @SchemaName = 'RequestSteps'
IF	NOT EXISTS(select * from sys.xml_schema_collections where name=@SchemaName)
BEGIN
-- Create an XML Schema Collection
	
	RAISERROR('Creating XSD Schema "%s" in Database %s; %s.',0,1,@SchemaName,@DBName,@Dt) WITH NOWAIT;

		CREATE XML SCHEMA COLLECTION CHEF.RequestSteps
		AS'<?xml version="1.0" encoding="utf-8"?>
			<xs:schema id="Request" xmlns="" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:msdata="urn:schemas-microsoft-com:xml-msdata">
			  <xs:element name="Request" msdata:IsDataSet="true" msdata:Locale="en-US">
				<xs:complexType>
				  <xs:choice minOccurs="0" maxOccurs="unbounded">
					<xs:element name="Process">
					  <xs:complexType>
						<xs:sequence>
						  <xs:element name="Steps" minOccurs="0" maxOccurs="unbounded">
							<xs:complexType>
							  <xs:sequence>
								<xs:element name="StepID" nillable="true" minOccurs="0" maxOccurs="unbounded">
								  <xs:complexType>
									<xs:simpleContent msdata:ColumnName="StepID_Text" msdata:Ordinal="0">
									  <xs:extension base="xs:int">
									  </xs:extension>
									</xs:simpleContent>
								  </xs:complexType>
								</xs:element>
							  </xs:sequence>
							  <xs:attribute name="UsageType" type="xs:string" />
							</xs:complexType>
						  </xs:element>
						</xs:sequence>
						<xs:attribute name="ID" type="xs:int" />
					  </xs:complexType>
					</xs:element>
				  </xs:choice>
				</xs:complexType>
			  </xs:element>
			</xs:schema>'
	RAISERROR('Created XSD Schema "%s" in Database %s; %s ',0,1,@SchemaName,@DBName,@Dt) WITH NOWAIT;
END
ELSE
BEGIN
	RAISERROR('The Schema "%s" already exists in Database %s; %s',0,1,@SchemaName,@DBName,@Dt) WITH NOWAIT;
	RAISERROR('-------------------------------------------------------Schema %s ---------------------------------------------------------------------',0,1,@SchemaName) WITH NOWAIT;
	PRINT ''	--insert a blank line

END
-- UNIT TESTING
--DECLARE @x XML(CHEF.RequestSteps)
--SELECT @x = '<Steps UsageType="EXCLUDE"> 
--<StepID>1010</StepID>
--<StepID>1020</StepID>
--</Steps>'

