using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Dts.Tasks.ExecuteSQLTask;
using Microsoft.SqlServer.Dts.Runtime;

namespace CHEFEngine
{
    class ExecuteSQLTaskWrapper
    {
        public string Name {get; set;}
        public bool BypassPrepare {get;set;}
        public string Connection {get;set;}
        public SqlStatementSourceType SqlStatementSourceTypeField{get;set;}
        public string SqlStatementSource {get;set;}
        public string SetExpression { get; set; }
        public ResultSetType ResultSetTypeField{get;set;}
      
        public static void AddSqlTask(Executable exe, ExecuteSQLTaskWrapper sqlTask)
        {
            TaskHost th = exe as TaskHost;
            //Configure the task
            th.Name = sqlTask.Name;
            th.Properties["BypassPrepare"].SetValue(th, sqlTask.BypassPrepare);
            th.Properties["Connection"].SetValue(th, sqlTask.Connection);
            th.Properties["SqlStatementSourceType"].SetValue(th, sqlTask.SqlStatementSourceTypeField);
            th.Properties["SqlStatementSource"].SetValue(th, sqlTask.SqlStatementSource);
            th.Properties["ResultSetType"].SetValue(th, sqlTask.ResultSetTypeField);
            th.Properties["SqlStatementSource"].SetExpression(th, sqlTask.SetExpression);
            th = null;
        
        }
    }
}
