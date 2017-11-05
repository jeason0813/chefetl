Echo @DWServer = %1
Echo @DBName = %2
Echo @OutputPath = %3

Echo 'Passing Parameters'

sqlcmd -E -S %1 -v DBName=%2 -v OutputPath=%3 -i "..\..\Database\Schema Objects\Jobs\CHEF_ControllerJob.sql"

sqlcmd -E -S %1 -v DBName=%2 -v OutputPath=%3 -i "..\..\Database\Schema Objects\Jobs\CHEF_ExecutorJob.sql"




