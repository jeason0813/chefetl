Echo @OutputLogPath=%1
Echo @CHEFServer = %2
Echo @ServerDomain = %3
Echo @ShareName = %4

call cscript mkdir_Permission.wsf "%1" "%2" "%3" "%4"
