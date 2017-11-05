using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Dts.Tasks.ExecuteSQLTask;
using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.SqlServer.Dts.Tasks.ExecutePackageTask;

namespace CHEFEngine.Executables
{
    internal class ExecutePackageWrapper
    {
        public static void AddExecPackageTask(Executable exe, string TaskName, string connectionKey)
        {
            TaskHost th = exe as TaskHost;
                th.Name = TaskName;
            IDTSExecutePackage100 exePkgTask = th.InnerObject as IDTSExecutePackage100;
            exePkgTask.Connection = connectionKey;
          
        }
     
    }
}
