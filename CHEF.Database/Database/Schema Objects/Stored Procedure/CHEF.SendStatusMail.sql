
SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON
SET ANSI_PADDING ON

RAISERROR('-------------------------------------------------------Procedure CHEF.SendStatusMail ---------------------------------------------------------------------',0,1) WITH NOWAIT;
DECLARE @DBName sysname = DB_NAME()
RAISERROR('Dropping and Creating Stored procedure CHEF.SendStatusMail in Database %s',0,1,@DBName) WITH NOWAIT
IF OBJECT_ID('CHEF.SendStatusMail','P') IS NOT NULL
BEGIN
	RAISERROR('Dropping stored procedure CHEF.SendStatusMail',0,1) WITH NOWAIT
	DROP PROCEDURE CHEF.SendStatusMail
	RAISERROR('Dropped stored procedure CHEF.SendStatusMail',0,1) WITH NOWAIT
END
RAISERROR('Creating stored procedure CHEF.SendStatusMail',0,1) WITH NOWAIT
GO


CREATE PROCEDURE [CHEF].[SendStatusMail]
	@Stage varchar(20) --e.g., Started,Finished,Stopped,Failed 
	,@ProcessName varchar(255)
AS
DECLARE  @emailProfileName varchar(100)	
		,@Body varchar(1000)=NULL
		,@Subject varchar(1000) = NULL
		,@Recipients varchar(500)	
		,@IsSendNotification bit =1
BEGIN TRY		

		-- Get config values from globalconfig
		
		SELECT @Recipients = Metadata.value('(/CHEFGlobalConfig/GlobalConfiguration/@NotificationAlias)[1]','varchar(max)')
		FROM CHEF.MetaData
		WHERE ProcessID = 1000

		SELECT @IsSendNotification = Metadata.value('(/CHEFGlobalConfig/GlobalConfiguration/@SendNotification)[1]','varchar(max)')
		FROM CHEF.MetaData
		WHERE ProcessID = 1000

		SELECT @emailProfileName = Metadata.value('(/CHEFGlobalConfig/GlobalConfiguration/@DBMailProfileName)[1]','varchar(max)')
		FROM CHEF.MetaData
		WHERE ProcessID = 1000

		IF @IsSendNotification=1
			BEGIN
			IF @Stage='Finished'
		
				BEGIN
					SET @Subject = 'The '''+@ProcessName+'''Process Finished Successfully'
					SET @Body = 'The <b>'+@ProcessName+'</b> Process Finished Successfully. Please go to CHEF.Log table to view further details.'
				END
			ELSE IF @Stage='Failed'
				BEGIN
					SET @Subject = 'The '''+@ProcessName+''' Process Failed'
					SET @Body = 'The <b>'+@ProcessName+'</b> Process <font color="red"><b>Failed</b></font>. Please go to CHEF.Log table to view further details.'
				END

			ELSE IF @Stage='Stopped'
			BEGIN
				SET @Subject = 'The'''+@ProcessName+''' Process Stopped'
				SET @Body = 'The <b>'+@ProcessName+'</b> Process Stopped. Please go to CHEF.Log table to view further details.'
			END
		EXEC msdb.dbo.sp_send_dbmail 	
		@profile_name = @emailProfileName,     
		@recipients=@Recipients,        
		@body = @Body,	
		@body_format = 'HTML',        
		@subject = @Subject
	END
END TRY
BEGIN CATCH
	RAISERROR('Error Sending mail using DB Mail Please check DB Mail configuration',0,1) WITH NOWAIT
END CATCH