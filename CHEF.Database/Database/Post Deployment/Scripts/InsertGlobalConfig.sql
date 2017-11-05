SET ANSI_NULLS ON
SET QUOTED_IDENTIFIER ON

/* ==========================================================================================
	File			: InsertGlobalConfig.sql
	Name			: 
	Author			: kaicho
	Date			: 2nd-Sept-2010
	Description		: Insert global config in metadata table

			
	Unit Test:
		select * from chef.Metadata
	Note:
		1.	 LogArchiveRowLimit -- it essentially helps to determine when the CHEF log archive should be done
			,LogResetIdentity	-- the default is same as that of int positive max limit
			,LogArchiveDepricationInterval	-- depricate log data older than 10 years from the Archive Log table
			,ApplicationEventViewerName		-- The Windows Application Event Viewer Name that would be used by the Operations Dashboard 
		
	Change History
	-------- --------- -----------------------------------------------------------------------
	Date		Name            Description
	-------- --------- -----------------------------------------------------------------------
	17-Sep-2010	balajim		Added the ProcessID into the insert statement; Removed the space from the name
========================================================================================== */
DELETE FROM [CHEF].[MetaData] WHERE [ProcessName]='GlobalConfig'
INSERT INTO [CHEF].[MetaData]
           ([ProcessID]
		   ,[ProcessName]
           ,[MetaData]
           ,[Type]
           )
     VALUES
           (1000
		   ,'GlobalConfig'
           ,'<CHEFGlobalConfig ApplicationName="TDW">
				<GlobalConfiguration Version="1.0" LogLocation="CHEF.PATH\Log\" InstallationBitLocation="CHEF.PATH\Engine" MaxBatchSize="1024" MaxLogTableSize="1024" NotificationAlias="ramsingh" SendNotification="True" ThresholdTimeInMinutes="10" OutputPackageLocation="CHEF.PATH\CHEFPackage.dtsx" LogArchiveRowLimit="1000000" LogResetIdentity="2000000000" LogArchiveDepricationInterval="10" ApplicationEventViewerName="CHEF" BadRowFolderLocation="CHEF.PATH"/>
			 </CHEFGlobalConfig>'
           ,1)
GO


