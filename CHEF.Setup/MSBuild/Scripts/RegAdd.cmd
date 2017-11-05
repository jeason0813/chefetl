Echo @SolutionRoot = %1
Echo @TDW_Web_Web = %2
Echo @MARTServer = %3
Echo @ODSServer = %4
Echo @WebServer = %5

Echo 'Registry - Add Mart Conn String'
call cscript %1\Install\remoteExec.wsf "%2\bin\LoadConnectionUtil.exe" "/site:server=%3;database=TDW_Site;Integrated Security=SSPI" "%5"

Echo 'Registry - Add ODS Conn String'
call cscript %1\Install\remoteExec.wsf "%2\bin\LoadConnectionUtil.exe" "/lpad:server=%4;database=ods_excise;Integrated Security=SSPI" "%5"