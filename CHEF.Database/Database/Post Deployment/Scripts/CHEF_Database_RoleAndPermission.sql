SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

/* ==========================================================================================
	File				: CHEF_Database_RoleAndPermission.sql
	Name			: CHEF_Database_RoleAndPermission
	Author		: balajim
	Date			: 04-Aug-2010
	Description	: Create a Database Role for CHEF and grant Read, Write and Exec permission to this role on CHEF schema.
						  For accessing CHEF related objects, the Service Account needs to be added to this Database Role either during deployment or later. 
	
	Unit Test:
		1.	Pseudocode
			Create a SQL User, 
			Grant Impersonation permission
			Impersonate and try to access CHEF objects, It should fail
			
			Add the SQL user to CHEF_DataAccess
			Impersonate and try to access CHEF objects, It should succeed 
		2. 
			CREATE USER CHEFTest WITHOUT LOGIN
			GRANT IMPERSONATE ON USER::CHEFTest TO [fareast\balajim]
			EXECUTE AS USER = 'CHEFTest' ;

			SELECT * FROM CHEF.RequestQueue
			INSERT CHEF.RequestQueue(ProcessID) VALUES(2)
			UPDATE CHEF.RequestQueue SET StartStepID = 5 WHERE QueueID = 10
			DELETE CHEF.RequestQueue WHERE QueueID = 10
			
			EXEC  CHEF.InsertRequestQueue @ProcessID = 6
			
			The following error should be observed
			
			Msg 229, Level 14, State 5, Line 1
			The SELECT permission was denied on the object 'RequestQueue', database 'CHEF', schema 'CHEF'.
			The INSERT permission was denied on the object 'RequestQueue', database 'CHEF', schema 'CHEF'.
			The UPDATE permission was denied on the object 'RequestQueue', database 'CHEF', schema 'CHEF'.
			The DELETE permission was denied on the object 'RequestQueue', database 'CHEF', schema 'CHEF'.
			The EXECUTE permission was denied on the object 'InsertRequestQueue', database 'CHEF', schema 'CHEF'.
			
			REVERT
			
			exec sp_addrolemember 'CHEF_DataAccess', 'CHEFTest'
			--run the above statements again to check access
			--all the above operations should be successful

			--Cleanup
			exec sp_droprolemember 'CHEF_DataAccess', 'CHEFTest'
			REVOKE IMPERSONATE ON USER::CHEFTest FROM [fareast\balajim]
			DROP USER CHEFTest
			
	Verify:
		1.	SELECT * FROM sys.database_principals WHERE name = 'CHEF_DataAccess'
			SELECT * FROM sys.database_permissions dp WHERE exists (select * from sys.database_principals WHERE name = 'CHEF_DataAccess' and principal_id = dp.grantee_principal_id) 
			
		2. SELECT * FROM sys.database_role_members  
	
	Note:
		1.	No other Security permission is required to access CHEF schema objects except adding the User or Login to the CHEF_DataAccess database role
		
	Change History
	-------- --------- -----------------------------------------------------------------------
	Date		Name            Description
	-------- --------- -----------------------------------------------------------------------
	
========================================================================================== */

DECLARE 
	 @SchemaName sysname = 'CHEF'
	,@DBName sysname = DB_NAME()
	,@DBRole sysname = 'CHEF_DataAccess'
	,@Dt varchar(100) = CONVERT(varchar(100),GETDATE(),109)
	
RAISERROR('-------------------------------------------------------Database Role %s ---------------------------------------------------------------------',0,1,@SchemaName) WITH NOWAIT;
--create the Database Role if not exists
IF NOT EXISTS(SELECT * FROM sys.database_principals WHERE name = @DBRole)
BEGIN
	RAISERROR('Creating Database Role "%s" in Database %s; %s.',0,1,@DBRole,@DBName,@Dt) WITH NOWAIT;

	CREATE ROLE  CHEF_DataAccess AUTHORIZATION db_owner
	
	RAISERROR('Created Database Role "%s" in Database %s; %s ',0,1,@DBRole,@DBName,@Dt) WITH NOWAIT;
END
ELSE
	RAISERROR('The Database Role "%s" already exists in Database %s; %s',0,1,@DBRole,@DBName,@Dt) WITH NOWAIT;

--grant Read, Write and Exec permissions to CHEF schema 
RAISERROR('Granting Read, Write and Execute Permission on %s Schema to Database Role "%s" in Database %s; %s.',0,1,@SchemaName,@DBRole,@DBName,@Dt) WITH NOWAIT;

GRANT SELECT, INSERT, UPDATE, DELETE, EXECUTE ON SCHEMA::CHEF TO CHEF_DataAccess;

RAISERROR('Granted Read, Write and Execute Permission on %s Schema to Database Role "%s" in Database %s; %s.',0,1,@SchemaName,@DBRole,@DBName,@Dt) WITH NOWAIT;

RAISERROR('-------------------------------------------------------Database Role %s ---------------------------------------------------------------------',0,1,@SchemaName) WITH NOWAIT;
PRINT ''	--insert a blank line
GO