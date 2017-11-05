	Sub SedFileInPlace(path, dict)
		Dim fin, fout, s, key
		Set fin  = oFSO.OpenTextFile(path, 1)
		Set fout = oFSO.CreateTextFile(path & ".$$$", True)

		While Not fin.AtEndOfStream
			s = fin.ReadLine
			For Each key in dict.Keys
				s = Replace (s, key, dict.item(key))
			Next 'key
			fout.WriteLine(s)
		Wend

		fin.Close
		Set fin = Nothing
		fout.Close
		Set fout = Nothing
		oFSO.DeleteFile(path)
		On Error Goto 0
		oFSO.MoveFile path & ".$$$", path
	End Sub

	Function ReplaceWithDict(ByVal s, ByVal dict)
		Dim ex, key
		Set ex = New RegExp
		ex.IgnoreCase = True
		ex.Global = True
		For Each key in dict.Keys
			ex.Pattern = key
			s = ex.Replace(s, dict.item(key))
		Next 'key
		ReplaceWithDict = s
	End Function 'ReplaceWithDict(s, dict)

	Sub oSQL( strServerName, strSQLFile, ByVal dict)
		Dim oFSO, ts, tsLine, strSQL, oConn
		WScript.echo "Starting execution of script '" & strSQLFile & "'"

		Set oConn = CreateObject("ADODB.Connection")
		Set oFSO = CreateObject("Scripting.FileSystemObject") 
		Set ts = oFSO.OpenTextFile(strSQLFile ,ForReading,True)

		oConn.Open("Provider=sqloledb;server=" &  strServerName & ";integrated security=sspi")

 		strSQL = ""
		Do While NOT ts.AtEndOfStream 
			tsLine =  Trim ( Replace ( ts.ReadLine, vbTab, " " ) )
			'WScript.echo tsLine
			If UCASE(tsLine) <> "GO" then 
				If LEFT(tsLine,2) = "--" THEN
					WScript.echo ReplaceWithDict(tsLine, dict) 
				Else
					strSQL = strSQL & vbCrLf & tsLine 
				End If
			ELSE
				If strSQL<>"" Then
					strSQL = ReplaceWithDict(strSQL, dict) 
					oConn.Execute ( strSQL )
					'WScript.echo strSQL
					strSQL = ""
				ENd If
			ENd If
		Loop
		ts.Close
		Set ts=Nothing
		oConn.Close
		Set oConn = Nothing

		WScript.echo "Completed execution of script '" & strSQLFile & "'"
		WScript.echo ""

	End Sub

	Sub RegAdd( sKeyName , sValueName, sType, sData, sServerName )
		Dim oShell, oExec, sCommand

		Set oShell  = CreateObject("WScript.Shell") 
		sCommand = "REG ADD ""\\" & sServerName & "\" & sKeyName & """ /v """ & sValueName & """ /t " & sType & " /d """ & sData & """ /f"

		WScript.echo "Executing command " & sCommand

		Set oExec = oShell.Exec("cmd.exe /c 2>&1 " & sCommand)
		Do While oExec.Status = 0
			WScript.Echo oExec.StdOut.ReadAll
			WScript.Sleep 100
		Loop

	End Sub


	Function ServiceExists(pServiceName, pMachineName)
		Dim oWMIService
		Dim colRunningServices
		Dim oService
		Dim flag
		flag=0

		Set oWMIService  = GetObject("winmgmts:" _
			& "{impersonationLevel=impersonate}!\\" & pMachineName & "\root\cimv2")
		Set colRunningServices = oWMIService.ExecQuery _
			("Select * from Win32_Service")
		For Each oService in colRunningServices
			if UCase(oService.Name) = UCase(pServiceName) then
				flag=1 
			end if
		Next
		ServiceExists= flag

	End Function 

	Sub netsvc (pServiceName, pAction, pMachineName)

		Dim oWMIService
		Dim colRunningServices
		Dim oService
		Dim flag
		flag=0

		Set oWMIService  = GetObject("winmgmts:" _
			& "{impersonationLevel=impersonate}!\\" & pMachineName & "\root\cimv2")
		Set colRunningServices = oWMIService.ExecQuery _
			("Select * from Win32_Service")
		For Each oService in colRunningServices
			if UCase(oService.Name) = UCase(pServiceName) then
				flag=1 
				if ucase(pAction) = "STOP" THEN
					oService.StopService()
				ELSE
					oService.StartService()
				End If
			end if
		Next

		If flag = 1 Then
			Wscript.echo "Wating for the " & pServiceName & " Service on machine " & pMachineName & " to " & pAction
			dim cntI 
			For cntI = 1 to 15
				WScript.Sleep 1000
				WScript.Echo "."
			Next
		else
			WScript.echo "WARNING: Service name " & pServiceName & " not found on machine " & pMachineName & ". Could not perform "  & pAction & " Operation"
		End if

	End Sub

	Sub RemoteProcessExecute(pMachineName, pProcess)
		WScript.Echo "Remote execute (" & pMachineName & ") " & pProcess  
		Dim sProcess, sResult, sID
		Set sProcess = GetObject("winmgmts:{impersonationLevel=impersonate}!\\" & pMachineName & "\root\cimv2:Win32_Process")
		sResult= sProcess.Create(pProcess, null, null, sID)

		If sResult = 0 Then WScript.Echo "[0] Successful completion"
		If sResult = 2 Then WScript.Echo "[2] Access denied"
		If sResult = 3 Then WScript.Echo "[3] Insufficient privilege" 
		If sResult = 8 Then WScript.Echo "[8] Unknown failure"
		If sResult = 9 Then WScript.Echo "[9] Path not found"
		If sResult = 21 Then WScript.Echo "[21] Invalid parameter"
		dim cntI 
		For cntI = 1 to 15
			WScript.Sleep 1000
		Next
	End Sub

	Sub CreateFolders(serverName, Path)
		Dim oShell, oExec , sPath

		Set oShell  = CreateObject("WScript.Shell") 
		sPath= """\\" &  serverName & "\" & Replace(Path, ":", "$") & """" 
		

		WScript.echo "Creating Directory " & sPath
		Set oExec = oShell.Exec("cmd.exe /c 2>&1 mkdir " & sPath)
		Do While oExec.Status = 0
			WScript.Echo oExec.StdOut.ReadAll
			WScript.Sleep 100
		Loop

	End Sub



	Public Sub CleanDir ( Path,  ServerName)

		Dim oFSO, oRootFolder, oSubFolders, oSubFolder, oFile
		Dim sFolderPath

		Set oFSO     = CreateObject("Scripting.FileSystemObject")
		sFolderPath = "\\" & ServerName & "\" & Mid(Path,1,1) & "$" & "\" & Mid(Path,4,Len(Path))	
 		
		set oRootFolder=oFSO.GetFolder(sFolderPath)
		Set oSubFolders = oRootFolder.SubFolders

		if oRootFolder.Files.count > 0 Then
			Wscript.Echo "Deleting all files in " & """" & sFolderPath & """" 
			For Each oFile In oRootFolder.Files		        
       				oFile.Delete true
			Next
		End If

		For Each oSubFolder in oSubFolders 
			Wscript.Echo "Deleting Subfolder " & """" & oSubFolder.name & """" 
			oFSO.DeleteFolder sFolderPath & "\" & oSubFolder.name, true
		Next	
	End Sub



	Function GetLocalSID(pServer, pKey, pIsGroup)
		'Wscript.Echo "	Getting SID for account [" & pKey & "] on server [" & pServer & "]."

		Dim objWMIService, objItem, colItems, sSID, sFlag
		Set objWMIService = GetObject( "winmgmts:\\" & pServer & "\root\cimv2")
		If pIsGroup = 1 Then
			Set colItems = objWMIService.ExecQuery ("Select * from Win32_Group Where Name = '" & pKey & "'")
		Else
			Set colItems = objWMIService.ExecQuery ("Select * from Win32_Account Where Name = '" & pKey & "'")
		End If

		'Wscript.Echo "Local Account" & CHR(9) & "Name" & CHR(9) & "SID" & CHR(9) & "SID Type" & CHR(9) & "Status"
		sFlag = 0
		For Each objItem in colItems
			If objItem.LocalAccount="True" Then
				sFlag = 1
				sSID = objItem.SID
				Exit For
			End If
			'Wscript.Echo  objItem.LocalAccount & CHR(9) & objItem.Name & CHR(9) & objItem.SID & CHR(9) & objItem.SIDType & CHR(9) & objItem.Status
		Next
		If sFlag = 1 Then
			'Wscript.Echo "Valid SID [" & sSID & "] for [" & pServer & "\" & pKey & "]"
       			GetLocalSID = sSID
		Else
			Wscript.Echo "Invalid SID : [" & pServer & "\" & pKey & "]"
		End If
	End Function

	Public Sub SetPermissions(serverName, shareName, Users, Permission, permissionType)
		Dim userArray, iuserctr
		userArray = Split(Users , ",")
		For iuserctr = LBound(userArray) to UBound(userArray)
			SetPermission ServerName, ShareName, userArray(iuserctr), Permission, PermissionType
		Next

	End sub

	Public Sub SetPermission(serverName, shareName, userName, accessType, permissionType  )
		'***** Constants
		Const ADS_PATH_FILE 		= 1
		Const ADS_PATH_FILESHARE 	= 2
		Const ADS_SD_FORMAT_IID 	= 1
		Const ADS_SD_FORMAT_RAW		= 2
		Const ADS_ACETYPE_ACCESS_ALLOWED = 0

		'Permission ACEFlags Constants
		Const CUSTOM_SHARED_ACEFLAGS 		= 0
		Const CUSTOM_NTFS_ACEFLAGS 		= 3
		Const ADS_ACEFLAG_INHERITED_ACE	= &h10


		Dim oFSO, sdUtil, sd, oDacl, oAce, sPath
		Set oFSO = CreateObject("Scripting.FileSystemObject")
		Set sdUtil = CreateObject ("ADsSecurityUtility")
		sPath = "\\" & serverName & "\" & REPLACE(shareName, ":", "$")

		If oFSO.FileExists(sPath) Or oFSO.FolderExists(sPath) Then
			wscript.echo "Setting " & permissionType & " permission for user [" & userName & "] on [" & sPath & "]. AccessType:" & accessType
		Else
			wscript.echo "Warning: [" & sPath & "] not exists."
			Exit Sub
		End If
		Dim accessTypeCode
		accessTypeCode= GetAccessType(accessType)
		if userName = "ASPNET" then userName= GetLocalSID(serverName, userName, 0)
		if userName = "IIS_WPG" then userName = GetLocalSID(serverName, userName, 1)

		If permissionType = "NTFS" Then
			Set sd = sdUtil.GetSecurityDescriptor(sPath, ADS_PATH_FILE, ADS_SD_FORMAT_IID)
			Set oDacl = sd.DiscretionaryAcl
			Set oAce = CreateObject("AccessControlEntry")
			oAce.Trustee = userName 
			oAce.AceType = ADS_ACETYPE_ACCESS_ALLOWED
			oAce.AceFlags = CUSTOM_NTFS_ACEFLAGS
			oAce.AccessMask = accessTypeCode
			oDacl.AddAce(oAce)
			sd.DiscretionaryAcl = oDacl
			sdUtil.SetSecurityDescriptor sPath, ADS_PATH_FILE, sd, ADS_SD_FORMAT_IID
		Else
			Set sd = sdUtil.GetSecurityDescriptor(sPath, ADS_PATH_FILESHARE, ADS_SD_FORMAT_IID)
			Set oDacl = sd.DiscretionaryAcl
			Set oAce = CreateObject("AccessControlEntry")
			oAce.Trustee = userName 
			oAce.AceType = ADS_ACETYPE_ACCESS_ALLOWED
			oAce.AceFlags = CUSTOM_SHARED_ACEFLAGS
			oAce.AccessMask = accessTypeCode
			oDacl.AddAce(oAce)
			sd.DiscretionaryAcl = oDacl
			sdUtil.SetSecurityDescriptor sPath, ADS_PATH_FILESHARE, sd, ADS_SD_FORMAT_IID
		End If

		Set sdUtil = Nothing
		Set oAce= Nothing
		Set oDacl = Nothing
		Set sd = Nothing	
	End Sub 

	Public Function GetAccessType(sPermission)
		Dim sPerm
		sPerm = 0
		'Shared Permission Constants
		If sPermission = "CUSTOM_SHARE_FULLCONTROL" then
			sPerm = 2032127
		ElseIf sPermission = "CUSTOM_SHARE_CHANGE" then
			sPerm = 1245631
		Elseif sPermission = "CUSTOM_SHARE_READ" then
			sPerm = 1179817	
		End If

		'NTFS Permission Constants
		If sPermission = "CUSTOM_NTFS_FULLCONTROL" then
			sPerm = 2032127
		ElseIf sPermission = "CUSTOM_NTFS_MODIFY" then
			sPerm = 1245631
		Elseif sPermission = "CUSTOM_NTFS_READ_EXECUTE" then
			sPerm = 1180095
		ElseIf sPermission = "CUSTOM_NTFS_READ" then
			sPerm = 1179785
		Elseif sPermission = "CUSTOM_NTFS_READWRITE" then
			sPerm = 1180063	
		End If
		GetAccessType = sPerm 
	End Function 



	Public Sub ShareFolder(domainName, serverName, physicalPath, shareName, recreateShare)

		Dim ShareSrvObj, NewShareObj, adsFileShare, isExist, aLan, createShare
		createShare = "1"
		Set ShareSrvObj = GetObject("WinNT://" & domainName & "/" & serverName & "/lanmanserver")

		For Each adsFileShare In ShareSrvObj
			If lcase(adsFileShare.Name) = lcase(shareName) Then
				If recreateShare = "1" Then
					'WScript.echo "Deleting existing share"
					ShareSrvObj.Delete "fileshare", adsFileShare.Name
				Else
					WScript.echo "Shared folder already exists. Share will not be recreated."
					createShare = "0"
				End If
				Exit For
			End If
		Next


		If createShare  = "1" Then
			Set ShareSrvObj = GetObject("WinNT://" & domainName & "/" & serverName)
			Set aLAN = ShareSrvObj.GetObject("FileService", "lanmanserver")
		
			WScript.echo "(Re)Creating share " & shareName 
			Set NewShareObj = aLan.Create("fileshare", shareName)
			NewShareObj.Path = physicalPath
			NewShareObj.MaxUserCount=-1
			NewShareObj.SetInfo
			Set NewShareObj = Nothing

			ResetSharedPermission serverName, shareName
		End If
	End Sub


	Public Sub ResetSharedPermission(serverName, shareName)
		'***** Constants
		'Other ADS Constants
		Const ADS_PATH_FILE 		= 1
		Const ADS_PATH_FILESHARE 	= 2
		Const ADS_SD_FORMAT_IID 	= 1
		Const ADS_SD_FORMAT_RAW		= 2
		Const ADS_ACETYPE_ACCESS_ALLOWED = 0

		'Permission ACEFlags Constants
		Const CUSTOM_SHARED_ACEFLAGS 		= 0
		Const CUSTOM_NTFS_ACEFLAGS 		= 3
		Const ADS_ACEFLAG_INHERITED_ACE	= &h10

		'Shared Permission Constants
		Const CUSTOM_SHARE_FULLCONTROL	= 2032127
		Const CUSTOM_SHARE_CHANGE 	= 1245631
		Const CUSTOM_SHARE_READ 	= 1179817

		'NTFS Permission Constants
		Const CUSTOM_NTFS_FULLCONTROL 	= 2032127
		Const CUSTOM_NTFS_MODIFY 	= 1245631
		Const CUSTOM_NTFS_READ_EXECUTE 	= 1180095
		Const CUSTOM_NTFS_READ 		= 1179785
		Const CUSTOM_NTFS_READWRITE 	= 1180063

		Dim sdUtil, sd, oDacl, oAce, sPath
		sPath = "\\" & serverName & "\" & shareName

		'WScript.Echo "Resetting Shared Permission... " & sPath

		Set sdUtil = CreateObject ("ADsSecurityUtility")
		Set sd = sdUtil.GetSecurityDescriptor(sPath, ADS_PATH_FILE, ADS_SD_FORMAT_IID)
		Set oDacl = sd.DiscretionaryAcl 
  		sd.DiscretionaryACL = oDacl
		sdUtil.SetSecurityDescriptor sPath, ADS_PATH_FILESHARE, sd, ADS_SD_FORMAT_IID
		Set sd = sdUtil.GetSecurityDescriptor(sPath, ADS_PATH_FILESHARE, ADS_SD_FORMAT_IID)
		Set oDacl = sd.DiscretionaryAcl 
		For Each oAce In oDacl
       			oDacl.RemoveAce oAce
	  	Next

		Dim WshNetwork 
		Set WshNetwork = WScript.CreateObject("WScript.Network")
		Set oAce = CreateObject("AccessControlEntry")
		oAce.Trustee = WshNetwork.UserDomain & "\" & WshNetwork.UserName
		oAce.AceType = ADS_ACETYPE_ACCESS_ALLOWED
		oAce.AceFlags = CUSTOM_SHARED_ACEFLAGS
		oAce.AccessMask = CUSTOM_SHARE_FULLCONTROL
		oDacl.AddAce(oAce)
		sd.DiscretionaryAcl = oDacl
		sdUtil.SetSecurityDescriptor sPath, ADS_PATH_FILESHARE, sd, ADS_SD_FORMAT_IID

		Set sd = sdUtil.GetSecurityDescriptor(sPath, ADS_PATH_FILE, ADS_SD_FORMAT_IID)
		Set oDacl = sd.DiscretionaryAcl 
		Set oAce = CreateObject("AccessControlEntry")
		oAce.Trustee = WshNetwork.UserDomain & "\" & WshNetwork.UserName
		oAce.AceType = ADS_ACETYPE_ACCESS_ALLOWED
		oAce.AceFlags = CUSTOM_NTFS_ACEFLAGS
		oAce.AccessMask = CUSTOM_NTFS_FULLCONTROL
		oDacl.AddAce(oAce)
		sd.DiscretionaryAcl = oDacl
		sdUtil.SetSecurityDescriptor sPath, ADS_PATH_FILE, sd, ADS_SD_FORMAT_IID
	End Sub