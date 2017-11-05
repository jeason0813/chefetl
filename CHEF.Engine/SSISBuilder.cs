using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Dts.Runtime;              // Managed Runtime namespace           [Microsoft.SqlServer.ManagedDTS.dll]
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;     // Pipeline Primary Interop Assembly   [Microsoft.SqlServer.DTSPipelineWrap.dll]
using wrap = Microsoft.SqlServer.Dts.Runtime.Wrapper;
using Microsoft.SqlServer.Dts.Tasks.ExecuteSQLTask;
using System.IO;
using CHEFEngine.Executables;
using System.Globalization;
using System.Data.SqlClient;
using Microsoft.SqlServer.Dts.Tasks.SendMailTask;
namespace CHEFEngine
{
    public partial class SSISBuilder
    {
        #region Standard SQL
        string PreSQL = "Declare @StepName varchar(255)=left('<StepName>',255) EXEC CHEF.InsertLog @StepName , 1";
        string PostSQL = "Declare @StepName varchar(255)=left('<StepName>',255) Exec CHEF.InsertLog @StepName, 2, ?";
        string PostPassSuccessSQL = "Declare @StepName varchar(255)=left('<StepName>',255) Exec CHEF.InsertLog @StepName, ?, ?";
        const string LogConnectionKey = "SQLPreAndPostLogKey007";
        #endregion
        #region fields
        int iCount = 0;
        int iVarCount = 0;
        private Package _Package;
        private Sequence _sequence;
        private Executable _preTaskHost;
        private bool _verboseLogging = false;
        private string _badRowsFolderLocation = string.Empty;
        private Dictionary<string, ConnectionManager> _ConnectionDic;
        private Dictionary<string, string> _ConnectionStr;
        #endregion
        #region Package Creation and Save
        public SSISBuilder(string Name, string Description = "")
        {
            _Package = new Package();
            _Package.PackageType = DTSPackageType.DTSDesigner100;
            _Package.Name = Name;
            _Package.Description = Description.Trim().Length == 0 ? Name : Description;
            _Package.CreatorComputerName = System.Environment.MachineName;
            _Package.CreatorName = System.Environment.UserName;
            _ConnectionDic = new Dictionary<string, ConnectionManager>();
            _ConnectionStr = new Dictionary<string, string>();
        }
        public void SaveAsPackage(string PackageFileName)
        {
            if (this.VerboseLogging)
                AddVerboseLogging();
            Application SSISApp = new Application();
            SSISApp.SaveToXml(PackageFileName, _Package, null);
        }
        #endregion
        #region Variables
        public Variable SetVariableValue(string Name, object Value, string dataType)
        {
            try
            {
                TypeCode typeCode = (TypeCode)Enum.Parse(typeof(TypeCode), dataType);
                Value = Convert.ChangeType(Value, typeCode);
            }
            catch
            {
                throw new Exception("This " + dataType + " don't exist.");
            }
            return _Package.Variables.Add(Name, false, "CHEF", Value);
        }

        public Variable GetVariableValue(string QualifiedName)
        {
            if (_Package.Variables[QualifiedName] == null)
                throw new Exception("Invalid Variable Name");
            return _Package.Variables[QualifiedName];
        }
        public bool VerboseLogging
        {
            get
            {
                return _verboseLogging;
            }
            set
            {
                if (!_verboseLogging.Equals(value))
                {
                    _verboseLogging = value;

                }
            }
        }
        public string BadRowsFolderLocation
        {
            get
            {
                return _badRowsFolderLocation;
            }
            set
            {
                if (!_badRowsFolderLocation.Equals(value))
                {
                    _badRowsFolderLocation = value;

                }
            }
        }
        #endregion
        #region Connection

        public void AddConnection(Connection connection)
        {
            ConnectionManager SSISConnection;
            if (connection.ConnectionType == ConnectionTypes.OleDBConnection)
            {
                // Add the OLEDB connection manager.
                ConnectionManager oleDB = _Package.Connections.Add("OLEDB");

                // Set stock properties.
                oleDB.Name = connection.Key;
                oleDB.ConnectionString = connection.ConnectionString;
                SSISConnection = oleDB;

            }
            else if (connection.ConnectionType == ConnectionTypes.FileConnection)
            {
                ConnectionManager File = _Package.Connections.Add("FILE");
                File.ConnectionString = connection.ConnectionString;
                File.Name = connection.Key;

                SSISConnection = File;
            }
            else if (connection.ConnectionType == ConnectionTypes.FlatFileConnection)
            {
                // Add the Destination connection manager.
                ConnectionManager csvFile = _Package.Connections.Add("FLATFILE");

                // Set the stock properties.
                csvFile.ConnectionString = connection.ConnectionString;
                csvFile.Name = connection.Key;

                csvFile.Properties["Format"].SetValue(csvFile, "Delimited");
                csvFile.Properties["DataRowsToSkip"].SetValue(csvFile, 0);
                csvFile.Properties["ColumnNamesInFirstDataRow"].SetValue(csvFile, true);
                csvFile.Properties["RowDelimiter"].SetValue(csvFile, "\r\n");
                csvFile.Properties["TextQualifier"].SetValue(csvFile, "\"");

                SSISConnection = csvFile;

            }
            else if (connection.ConnectionType == ConnectionTypes.ExcelConnection)
            {
                ConnectionManager connMgr = _Package.Connections.Add("Excel");
                connMgr.Name = connection.Key;

                string XlConnectionString = String.Format(CultureInfo.InvariantCulture, "Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties=\"Excel 12.0;HDR=YES\";", connection.ConnectionString);
                connMgr.ConnectionString = XlConnectionString;

                wrap.IDTSConnectionManagerExcel100 excelConMgr = connMgr.InnerObject as wrap.IDTSConnectionManagerExcel100;
                excelConMgr.ExcelFilePath = connection.ConnectionString;
                //TODO: Can be exposed to config
                excelConMgr.FirstRowHasColumnName = true;

                SSISConnection = connMgr;
            }
            else if (connection.ConnectionType == ConnectionTypes.SMTPConnection)
            {
                ConnectionManager smtp = _Package.Connections.Add("SMTP");
                smtp.ConnectionString = connection.ConnectionString;
                smtp.Name = connection.Key;
                SSISConnection = smtp;
            }
            else if (connection.ConnectionType == ConnectionTypes.SharePointListConnection)
            {
                //ConnectionManager sharePointList = _Package.Connections.Add("SPCRED");
                //sharePointList.ConnectionString = connection.ConnectionString;
                //sharePointList.Name = connection.Key;
                //SSISConnection = sharePointList;
                SSISConnection = null;
            }
            else if (connection.ConnectionType == ConnectionTypes.TableStorageConnection)
            {
                //to do
                SSISConnection = null;
            }
            else
            {
                throw new Exception("Invalid Connection Type");
            }
            _ConnectionDic.Add(connection.Key, SSISConnection);
            _ConnectionStr.Add(connection.Key, connection.ConnectionString);
        }

        private ConnectionManager GetConnection(string Key)
        {
            ConnectionManager conMgr = _ConnectionDic[Key];
            if (conMgr == null) throw new Exception("Invalid source key " + Key);
            return conMgr;
        }
        private string GetConnStr(string Key)
        {
            string conStr = _ConnectionStr[Key];
            if (conStr == null) throw new Exception("Invalid source key " + Key);
            return conStr;
        }
        #endregion

        public void AddPrecedenceConstraints(Executable sqlTaskExe, IDTSSequence container = null)
        {
            if (container == null)
            {
                container = _Package;
            }
            if (_preTaskHost != null)
            {
                container.PrecedenceConstraints.Add(_preTaskHost, sqlTaskExe);

            }
            _preTaskHost = sqlTaskExe;
        }
        #region Tasks

        public void AddSendMailTask(string Name, string smtpKey, string FromLine, string ToLine, string CCLine, string BCCLine, string Subject, string MessageSource, SendMailTaskAttachments Attachments, string Priority, string MessageSourceType, IDTSSequence container = null)
        {
            if (container == null)
            {
                container = _Package;
            }
            MailPriority mailPriority;
            SendMailMessageSourceType sendMailMessageSourceType;
            if (Priority != string.Empty)
            {
                mailPriority = (MailPriority)Enum.Parse(typeof(MailPriority), Priority);
            }
            else
            {
                mailPriority = MailPriority.Normal;
            }

            if (MessageSourceType != string.Empty)
            {
                sendMailMessageSourceType = (SendMailMessageSourceType)Enum.Parse(typeof(SendMailMessageSourceType), MessageSourceType);
            }
            else
            {
                sendMailMessageSourceType = SendMailMessageSourceType.DirectInput;
            }
            Executable exe = container.Executables.Add(SSISMoniker.SENDMAIL_TASK);
            TaskHost emailTH = exe as TaskHost;
            emailTH.Name = Name;
            var sqlTaskPre = emailTH.InnerObject as IDTSSendMailTask;
            ConnectionManager conSMTP = _ConnectionDic[smtpKey];
            sqlTaskPre.SmtpConnection = conSMTP.Name;
            sqlTaskPre.FromLine = FromLine;
            sqlTaskPre.ToLine = ToLine;
            sqlTaskPre.Subject = Subject;
            sqlTaskPre.MessageSource = MessageSource;
            sqlTaskPre.CCLine = CCLine;
            sqlTaskPre.FileAttachments = Attachments.FileName;
            sqlTaskPre.Priority = mailPriority;
            sqlTaskPre.MessageSourceType = sendMailMessageSourceType;
            AddPrecedenceConstraints(exe);
        }
        public Executable AddExecutableSQLTask(String SQLStatement, string conKey, string taskName, string variableName = "", IDTSSequence container = null)
        {
            if (container == null)
            {
                container = _Package;
            }
            ConnectionManager conMgrSource = _ConnectionDic[conKey];
            ExecuteSQLTaskWrapper sqlTask = new ExecuteSQLTaskWrapper();
            //Configure the task
            sqlTask.Name = taskName;
            sqlTask.BypassPrepare = true;
            sqlTask.Connection = conMgrSource.Name.ToString();
            sqlTask.SqlStatementSourceTypeField = SqlStatementSourceType.DirectInput;

            if (SQLStatement.Contains(@"@[CHEF::"))
            {
                sqlTask.SqlStatementSourceTypeField = SqlStatementSourceType.Variable;

                Variable taskFailSuccess = SetVariableValue("varSQLStatement" + iVarCount.ToString(), "", "String");
                taskFailSuccess.EvaluateAsExpression = true;
                taskFailSuccess.Expression = "\"" + SQLStatement.Replace(@"\", @"\\").Replace("\\\\\"", "\\\"") + "\"";
                sqlTask.SqlStatementSource = taskFailSuccess.QualifiedName;
                iVarCount++;
            }
            else
            {
                sqlTask.SqlStatementSource = SQLStatement;
            }
            sqlTask.ResultSetTypeField = ResultSetType.ResultSetType_None;
            Executable exe = container.Executables.Add(SSISMoniker.SQL_TASK);
            ExecuteSQLTaskWrapper.AddSqlTask(exe, sqlTask);
            if (variableName != string.Empty)
            {
                TaskHost sqlTaskHostPost = exe as TaskHost;
                var sqlTaskPre = sqlTaskHostPost.InnerObject as IDTSExecuteSQL;
                IDTSParameterBinding bindingPostSuccessFailure = sqlTaskPre.ParameterBindings.Add();
                bindingPostSuccessFailure.ParameterDirection = ParameterDirections.Input;
                bindingPostSuccessFailure.DtsVariableName = variableName;
                bindingPostSuccessFailure.ParameterName = 0;
                bindingPostSuccessFailure.DataType = 3;
            }
            //#####Error Task#####
            AddSQLErrorTask(exe, sqlTask.Name);
            return exe;
        }
        public void AddSQLTaskSet(CHEFMetaDataProcessStepSQLTaskSet sqlTaskSet, string stepName)
        {
            string taskName = string.Empty;
            if (stepName == string.Empty)
            {
                throw new Exception("Step Name is missing: Please modify the metadata and try again");
            }
            if (sqlTaskSet != null)
            {

                if (sqlTaskSet.Name == string.Empty)
                {
                    throw new Exception("SQLTask Set Name is missing under Step " + stepName + " : Please modify the metadata and try again");
                }
                else if (sqlTaskSet.TargetConnection != string.Empty)
                {
                    //Handling Paraller Task
                    bool isParallel = true;
                    if (sqlTaskSet.RunParallel.ToUpper() != "TRUE")
                    {
                        isParallel = false;
                    }
                    if (sqlTaskSet.SQLTask != null)
                    {
                        //Setting up Variables Values @Runtime
                        if (sqlTaskSet.SetVariables != null)
                        {
                            SetVariableRuntime(sqlTaskSet.SetVariables, stepName, sqlTaskSet.Name);
                        }
                        if (sqlTaskSet.SendMailTask != null)
                        {
                            foreach (var sendMail in sqlTaskSet.SendMailTask)
                            {
                                AddSendMailTask(sendMail.Name, sendMail.SMTPServer, sendMail.From, sendMail.To, sendMail.CC, sendMail.BCC, sendMail.Subject, sendMail.MessageSource, sendMail.Attachments, sendMail.Priority, sendMail.MessageSourceType);
                            }
                        }
                        IDTSSequence container = null;
                        if (isParallel)
                        {
                            _sequence = (Sequence)_Package.Executables.Add("STOCK:SEQUENCE");
                            _sequence.Name = sqlTaskSet.Name;
                            if (_preTaskHost != null)
                            {
                                _Package.PrecedenceConstraints.Add(_preTaskHost, _sequence);

                            }
                            container = _sequence;
                        }
                        else
                        {
                            container = _Package;
                        }
                        foreach (var sqlTask in sqlTaskSet.SQLTask)
                        {
                            if (sqlTask.Name == string.Empty)
                            {
                                throw new Exception("SQLTask name is missing under SQLTask Set " + sqlTaskSet.Name + " : Please modify the metadata and try again");
                            }
                            else
                            {
                                if (isParallel)
                                {
                                    _preTaskHost = null;
                                }
                                taskName = stepName + "_" + sqlTaskSet.Name + "_" + sqlTask.Name;
                                // Pre log 
                                Executable sqlTaskPreExe = AddExecutableSQLTask(PreSQL.Replace("<StepName>", taskName) + ",0", LogConnectionKey, "Pre-" + taskName, string.Empty, container);
                                AddPrecedenceConstraints(sqlTaskPreExe, container);
                                // SQL Task
                                Executable sqlTaskExe = AddExecutableSQLTask(sqlTask.SQLStatement, sqlTaskSet.TargetConnection, taskName, string.Empty, container);
                                AddPrecedenceConstraints(sqlTaskExe, container);
                                // Post Log
                                Executable sqlTaskPostExe = AddExecutableSQLTask(PostSQL.Replace("<StepName>", taskName).Replace('?', '0') + "", LogConnectionKey, "Post-" + taskName, string.Empty, container);
                                AddPrecedenceConstraints(sqlTaskPostExe, container);
                            }
                        }
                    }
                    if (isParallel)
                        _preTaskHost = _sequence;
                }
                else
                {
                    // loging if target connection is not provided
                }

            }
        }
        private void AddSQLErrorTask(Executable exe, string TaskName)
        {
            TaskHost th = exe as TaskHost;
            DtsEventHandler thOnError = (DtsEventHandler)th.EventHandlers.Add("OnError");
            Executable runSQL = thOnError.Executables.Add(SSISMoniker.SQL_TASK);
            CreateEventHandlerSqlTask(runSQL, th.Name + " EventHandler", TaskName, GetConnection(LogConnectionKey));
        }
        private void CreateEventHandlerSqlTask(Executable runSQL, string p, string TaskName, ConnectionManager conLogMgrSource)
        {
            TaskHost th = runSQL as TaskHost;
            th.Name = p;
            th.Properties["BypassPrepare"].SetValue(th, true);
            th.Properties["Connection"].SetValue(th, conLogMgrSource.Name.ToString());
            th.Properties["SqlStatementSourceType"].SetValue(th, SqlStatementSourceType.DirectInput);
            //Build Sql Statement
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("\"if not exists(select 1 from Chef.Log where StatusID = 4 and QueueID = (SELECT MIN(QueueID) FROM CHEF.RequestQueue WHERE RequestStatus = 1) and ProcessStep='" + TaskName + "')");
            stringBuilder.Append("exec CHEF.InsertLog @ProcessStep = '");
            stringBuilder.Append(TaskName);//Step Name
            stringBuilder.Append("', @StatusID = 4,@RowsAffected = 0,@Description =");
            stringBuilder.Append('\'');
            stringBuilder.Append('"');
            stringBuilder.Append("+REPLACE(REPLACE( SUBSTRING(  (DT_WSTR, 500) @[System::ErrorDescription], FINDSTRING(  (DT_WSTR, 500) @[System::ErrorDescription],");
            stringBuilder.Append('"');
            stringBuilder.Append(" Description:");
            stringBuilder.Append('"');
            stringBuilder.Append(", 1)+1 ,500 ) ,");
            stringBuilder.Append('"');
            stringBuilder.Append('\\');
            stringBuilder.Append('"');
            stringBuilder.Append('"');
            stringBuilder.Append(',');
            stringBuilder.Append('"');
            stringBuilder.Append('"');
            stringBuilder.Append("),");
            stringBuilder.Append('"');
            stringBuilder.Append("'");
            stringBuilder.Append('"');
            stringBuilder.Append(",");
            stringBuilder.Append('"');
            stringBuilder.Append("''");
            stringBuilder.Append('"');
            stringBuilder.Append(")+");
            stringBuilder.Append('"');
            stringBuilder.Append('\'');
            stringBuilder.Append('"');
            th.Properties["SqlStatementSource"].SetExpression(th, stringBuilder.ToString());
            th.Properties["ResultSetType"].SetValue(th, ResultSetType.ResultSetType_None);
        }
        public void SetVariableRuntime(SetVariablesSetVariable[] sqlTaskSetVariables, string stepName, string setName)
        {
            int iVarCount = 0;

            foreach (var sqlSetVariable in sqlTaskSetVariables)
            {
                Executable sqlSetVariableExe = AddExecutableSQLTask(sqlSetVariable.SQLStatement, sqlSetVariable.TargetConnection, "Set Variable-" + stepName + "_" + setName + iVarCount.ToString(), string.Empty);
                TaskHost srcth = sqlSetVariableExe as TaskHost;
              //  srcth.Properties["BypassPrepare"].SetValue(srcth, true);
               // srcth.Properties["Connection"].SetValue(srcth, sqlSetVariable.TargetConnection);
               // srcth.Properties["SqlStatementSourceType"].SetValue(srcth, SqlStatementSourceType.DirectInput);
                //srcth.Properties["SqlStatementSource"].SetValue(srcth, sqlSetVariable.SQLStatement);
                srcth.Properties["ResultSetType"].SetValue(srcth, ResultSetType.ResultSetType_SingleRow);
                var sqlTaskSRC = srcth.InnerObject as IDTSExecuteSQL;
                foreach (var resultSet in sqlSetVariable.ResultSet)
                {
                    IDTSResultBinding resultbinding = sqlTaskSRC.ResultSetBindings.Add();
                    resultbinding.ResultName = resultSet.Order;
                    resultbinding.DtsVariableName = GetVariableValue(resultSet.VariableName).QualifiedName;
                }
                AddPrecedenceConstraints(sqlSetVariableExe);
                iVarCount++;
            }
        }
        public void AddExecutablePackage(string Name, string connectionKey)
        {
            Executable exe = _Package.Executables.Add(SSISMoniker.EXECUTE_PACKAGE);
            ExecutePackageWrapper.AddExecPackageTask(exe, Name, connectionKey);
            if (_preTaskHost != null)
            {
                _Package.PrecedenceConstraints.Add(_preTaskHost, exe);
            }
            _preTaskHost = exe;
        }
        #endregion


        public void AddDataFlowSet(CHEFMetaDataProcessStepDataFlowSet dataFlowSet, string stepName)
        {
            string ErrorDetail = string.Empty;
            string Name = string.Empty;
            if (stepName == string.Empty)
            {
                throw new Exception("Step Name is missing: Please modify the metadata and try again");
            }
            if (dataFlowSet != null)
            {
                if (dataFlowSet.Name == string.Empty || dataFlowSet.Name == null)
                {
                    throw new Exception("Data Flow Set Name is missing under Step " + stepName + " : Please modify the metadata and try again");
                }
                else if (dataFlowSet.TargetConnection != string.Empty)
                {
                    //Handling Paraller Task
                    bool isParallel = true;
                    if (dataFlowSet.RunParallel.ToUpper() != "TRUE")
                    {
                        isParallel = false;
                    }
                    if (dataFlowSet.DataFlow != null)
                    {
                        //Setting up Variables Values @Runtime
                        if (dataFlowSet.SetVariables != null)
                        {
                            SetVariableRuntime(dataFlowSet.SetVariables, stepName, dataFlowSet.Name);
                        }
                        if (dataFlowSet.SendMailTask != null)
                        {
                            foreach (var sendMail in dataFlowSet.SendMailTask)
                            {
                                AddSendMailTask(sendMail.Name, sendMail.SMTPServer, sendMail.From, sendMail.To, sendMail.CC, sendMail.BCC, sendMail.Subject, sendMail.MessageSource, sendMail.Attachments, sendMail.Priority, sendMail.MessageSourceType);
                            }
                        }
                        IDTSSequence container = null;
                        if (isParallel)
                        {
                            _sequence = (Sequence)_Package.Executables.Add("STOCK:SEQUENCE");
                            _sequence.Name = dataFlowSet.Name;
                            if (_preTaskHost != null)
                            {
                                _Package.PrecedenceConstraints.Add(_preTaskHost, _sequence);

                            }
                            container = _sequence;
                        }
                        else
                        {
                            container = _Package;
                        }
                        foreach (var dataFlow in dataFlowSet.DataFlow)
                        {
                            if (dataFlow.Name == string.Empty)
                            {
                                throw new Exception("Data Flow Task name is missing under DataFlow Set " + dataFlowSet.Name + " : Please modify the metadata and try again");
                            }
                            else
                            {

                                if (isParallel)
                                {
                                    _preTaskHost = null;
                                }
                                Name = stepName + "_" + dataFlowSet.Name + "_" + dataFlow.Name;
                                ErrorDetail = "Please varify the metadata for DataFlow :" + dataFlow.Name +"' under Step:'" + stepName + "' and  DataFlowSet:'" + dataFlowSet.Name + "'";
                                SourceType sourceType = (SourceType)Enum.Parse(typeof(SourceType), dataFlowSet.SourceType);
                                TargetType targetType = (TargetType)Enum.Parse(typeof(TargetType), dataFlowSet.TargetType);
                                Executable sqlTaskPostExe = null;
                                Executable sqlTaskPreExe = null;
                                sqlTaskPreExe = AddExecutableSQLTask(PreSQL.Replace("<StepName>", Name) + ",?", LogConnectionKey, "Pre-" + Name, string.Empty, container);

                                //Source Counting binding
                                string sourcevariableName = "SRC" + iCount.ToString();
                                Variable src = SetVariableValue(sourcevariableName, 0, "Int32");

                                string taskFailSuccessVariable = "TaskFailSuccess" + iCount.ToString();
                                Variable varTaskFailSuccess = SetVariableValue(taskFailSuccessVariable, 2, "Int32");
                                TaskHost sqlTaskHostPre = sqlTaskPreExe as TaskHost;
                                var sqlTaskPre = sqlTaskHostPre.InnerObject as IDTSExecuteSQL;
                                IDTSParameterBinding bindingPre = sqlTaskPre.ParameterBindings.Add();
                                bindingPre.ParameterDirection = ParameterDirections.Input;
                                bindingPre.DtsVariableName = src.QualifiedName;
                                bindingPre.ParameterName = 0;
                                bindingPre.DataType = 3;
                                if (SourceType.SPList != sourceType && SourceType.FlatFile != sourceType && SourceType.TableStorage != sourceType && TargetType.TableStorage != targetType && GetConnStr(dataFlowSet.SourceConnection).Contains("database.windows.net") == false)
                                {
                                    string sqlSRCStatement = "declare @rowCount INT = 0 select @rowCount = rows from sys.partitions with (nolock) where object_id = object_id('" + dataFlow.TargetName + "') select @rowCount";
                                    Executable sqlExecSRC = AddExecutableSQLTask(sqlSRCStatement, dataFlowSet.SourceConnection, "SRC " + Name, string.Empty, container);
                                    TaskHost sqlTaskHostSRC = sqlExecSRC as TaskHost;
                                    var sqlTaskSRC = sqlTaskHostSRC.InnerObject as IDTSExecuteSQL;
                                    TaskHost srcth = sqlExecSRC as TaskHost;
                                    srcth.Properties["ResultSetType"].SetValue(srcth, ResultSetType.ResultSetType_SingleRow);
                                    IDTSResultBinding resultbinding = sqlTaskSRC.ResultSetBindings.Add();
                                    resultbinding.ResultName = 0;
                                    resultbinding.DtsVariableName = src.QualifiedName;
                                    AddPrecedenceConstraints(sqlExecSRC, container);
                                }
                                else if (SourceType.FlatFile == sourceType)
                                {
                                    string strSQLPreQuery = "DECLARE @TaskHasBadRows INT DECLARE @RC INT SET @TaskHasBadRows=? SET @RC=? IF @TaskHasBadRows >=4 BEGIN RAISERROR ('File has bad records. Modifiy the bad rows and re-run the process', 16, 1) END ELSE BEGIN Exec CHEF.InsertLog '" + Name + "', @TaskHasBadRows, @RC END";

                                    varTaskFailSuccess.Expression = "@[CHEF::BadRC" + (iCount).ToString() + "] >0 ?4:2";
                                    varTaskFailSuccess.EvaluateAsExpression = true;
                                    Variable packageFailSuccess = GetVariableValue("CHEF::PackageFailSuccess");
                                    if (packageFailSuccess.Expression == string.Empty || packageFailSuccess.Expression == null)
                                    {
                                        packageFailSuccess.Expression = "@[CHEF::TaskFailSuccess" + (iCount).ToString() + "] ==4 ?4:2";
                                        packageFailSuccess.EvaluateAsExpression = true;
                                    }
                                    else
                                    {
                                        packageFailSuccess.Expression = "@[CHEF::TaskFailSuccess" + (iCount).ToString() + "] ==4 || " + packageFailSuccess.Expression;
                                    }
                                }
                                AddPrecedenceConstraints(sqlTaskPreExe, container);
                                //Truncate or Delete Cases
                                if (TargetType.TableStorage != targetType && TargetType.SPList != targetType)
                                    AddTruncateOrDeleteSQLTask(container, Name, dataFlow.Name, dataFlow.TargetName, dataFlowSet.TargetConnection, dataFlowSet.TruncateOrDeleteBeforeInsert, dataFlowSet.DeleteFilterClause);

                                Executable dftTaskExe = AddDataFlowTask(container, Name, dataFlowSet, dataFlow, ErrorDetail);
                                AddPrecedenceConstraints(dftTaskExe, container);
                                // Post Log
                                sqlTaskPostExe = AddExecutableSQLTask(PostPassSuccessSQL.Replace("<StepName>", Name), LogConnectionKey, "Post-" + Name, string.Empty, container);
                                TaskHost sqlTaskHostPost = sqlTaskPostExe as TaskHost;
                                var sqlTaskPost = sqlTaskHostPost.InnerObject as IDTSExecuteSQL;

                                IDTSParameterBinding bindingPostBadRC = sqlTaskPost.ParameterBindings.Add();
                                bindingPostBadRC.ParameterDirection = ParameterDirections.Input;
                                bindingPostBadRC.DtsVariableName = varTaskFailSuccess.QualifiedName;
                                bindingPostBadRC.ParameterName = 0;
                                bindingPostBadRC.DataType = 3;


                                IDTSParameterBinding bindingPost = sqlTaskPost.ParameterBindings.Add();
                                bindingPost.ParameterDirection = ParameterDirections.Input;
                                bindingPost.DtsVariableName = "CHEF::RC" + (iCount - 1);
                                bindingPost.ParameterName = 1;
                                bindingPost.DataType = 3;

                                AddPrecedenceConstraints(sqlTaskPostExe, container);
                            }
                        }
                    }
                    if (isParallel)
                        _preTaskHost = _sequence;
                }
                else
                {
                    // loging if target connection is not provided
                }

            }
        }
        private Executable AddDataFlowTask(IDTSSequence container, string Name, CHEFMetaDataProcessStepDataFlowSet dataFlowSet, CHEFMetaDataProcessStepDataFlowSetDataFlow dataFlow, string ErrorDetail)
        {

            IDTSComponentMetaData100 sourceTask = null;
            IDTSComponentMetaData100 targetTask = null;
            IDTSComponentMetaData100 rowCountTask = null;
            IDTSComponentMetaData100 badRowCountTask = null;
            IDTSComponentMetaData100 badRowTask = null;
            bool unicode = false;

            string variableName = "RC" + iCount.ToString();
            SetVariableValue(variableName, 0, "Int32");

            string variableBadRowCount = "BadRC" + iCount.ToString();
            Variable varBadRC = SetVariableValue(variableBadRowCount, 0, "Int32");
            SourceType sourceType = (SourceType)Enum.Parse(typeof(SourceType), dataFlowSet.SourceType);
            TargetType targetType = (TargetType)Enum.Parse(typeof(TargetType), dataFlowSet.TargetType);

            iCount++;
            DFTTask dft_task = new DFTTask();
            Executable dftTask = dft_task.AddDFTTask(container, Name, iCount);
            ConnectionManager conMgrSource = null;
            string conStr = string.Empty;
            if (sourceType == SourceType.TableStorage || sourceType == SourceType.SPList)
                conStr = GetConnStr(dataFlowSet.SourceConnection);
            else
            {
                conMgrSource = GetConnection(dataFlowSet.SourceConnection);
            }

            ConnectionManager conMgrDestination = null;
            string conStrTarget = string.Empty;
            if (targetType == TargetType.TableStorage || targetType == TargetType.SPList)
                conStrTarget = GetConnStr(dataFlowSet.TargetConnection);
            else
            {
                conMgrDestination = GetConnection(dataFlowSet.TargetConnection);
            }

            ConnectionManager conMgrBadRows = null;
            string sourceName = dataFlow.SourceName;
            string targetName = dataFlow.TargetName;
            string taskName = dataFlow.Name;
            if (taskName == null || taskName == string.Empty)
            {
                taskName = Name;
            }
            if (sourceType == SourceType.FlatFile)
            {
                if (dataFlow.SourceName.Contains("@[CHEF::"))
                {
                    Variable varTaskFlowNameRuntime = SetVariableValue("varTaskFlowNameRuntime" + iVarCount.ToString(), "", "String");
                    varTaskFlowNameRuntime.EvaluateAsExpression = true;
                    varTaskFlowNameRuntime.Expression = "\"" + dataFlow.SourceName + "\"";
                    iVarCount++;
                    sourceName = varTaskFlowNameRuntime.Value.ToString();
                }
                string flatFileConnectionKey = taskName.Replace('.', '_');
                string badRowsConnectionKey = flatFileConnectionKey + "_BadRows";
                AddConnection(new Connection() { Key = flatFileConnectionKey, ConnectionString = conMgrSource.ConnectionString + @"\" + sourceName, ConnectionType = ConnectionTypes.FlatFileConnection });
                conMgrSource = GetConnection(flatFileConnectionKey);
                AddConnection(new Connection() { Key = badRowsConnectionKey, ConnectionString = this.BadRowsFolderLocation + @"\" + badRowsConnectionKey + ".txt", ConnectionType = ConnectionTypes.FlatFileConnection });
                conMgrBadRows = GetConnection(badRowsConnectionKey);

                conMgrBadRows.Properties["ColumnNamesInFirstDataRow"].SetValue(conMgrBadRows, false);
                //Map target columns

                if (dataFlowSet.UniCode == null || dataFlowSet.UniCode == string.Empty)
                {
                    unicode = false;
                }
                else
                {
                    unicode = Convert.ToBoolean(dataFlowSet.UniCode);
                }
                //    conMgrBadRows.Properties["Unicode"].SetValue(conMgrBadRows, unicode);
                if (Convert.ToBoolean(dataFlowSet.PickColumnsFromTarget))
                {
                    string strConn = GetConnStr(dataFlowSet.TargetConnection);

                    dft_task.AddVirtualColumnsFromTarget(conMgrSource, sourceTask, dataFlow.TargetName, dataFlowSet.ColumnDelimeter, Convert.ToBoolean(dataFlowSet.IsColumnNamesInFirstDataRow), Convert.ToBoolean(dataFlowSet.AllowFlatFileTruncate), unicode, strConn);
                }
                else
                {
                    throw new Exception("Engine don't support Pick Columns From Target as 'False'");
                }

            }
            TaskHost taskHost = dftTask as TaskHost;
            MainPipe dataFlowTask = taskHost.InnerObject as MainPipe;
            if (sourceType == SourceType.SELECTSQL)
            {
                if (sourceName.Contains("@[CHEF::"))
                {
                    string str = sourceName;
                    Variable taskFailSuccess = SetVariableValue("varTaskFlowSQLStatement" + iVarCount.ToString(), "", "String");
                    taskFailSuccess.EvaluateAsExpression = true;
                    iVarCount++;
                    taskFailSuccess.Expression = "\"" + str + "\"";
                    dft_task.AddSourceTask(dataFlowTask, Name, taskFailSuccess.QualifiedName, sourceType, conMgrSource, out sourceTask, true, conStr);
                }
                else
                {
                    dft_task.AddSourceTask(dataFlowTask, Name, dataFlow.SourceName, sourceType, conMgrSource, out sourceTask, false, conStr);
                }
            }
            else
            {
                dft_task.AddSourceTask(dataFlowTask, Name, dataFlow.SourceName, sourceType, conMgrSource, out sourceTask, false, conStr);
            }
            dft_task.AddRowCount(dataFlowTask, Name, out rowCountTask, variableName);
            dataFlowTask.ConnectSourceTarget(sourceTask, rowCountTask);
            CManagedComponentWrapper InstanceDestination = dft_task.AddTargetTask(dataFlowTask, Name, targetName, targetType, conMgrDestination, out targetTask, conStrTarget);
            dataFlowTask.ConnectSourceTarget(rowCountTask, targetTask);
            //  dataFlowTask.ConnectSourceTarget(sourceTask, targetTask);


            if (targetType == TargetType.FlatFile)
            {
                //Add column based on source
                dft_task.GenerateColumnForVirtualInput(conMgrDestination, targetTask, dataFlowSet.ColumnDelimeter, unicode);
                //Refresh to generate meta data
                InstanceDestination.ConnectAndReinitializeMetaData(sourceName);
                //Map target columns
                dft_task.MapColumns(targetTask, InstanceDestination, DTSUsageType.UT_READONLY, ErrorDetail);
            }
            else if (targetType == TargetType.Table && sourceType == SourceType.FlatFile)
            {
                //Map target columns
                dft_task.MapColumns(targetTask, InstanceDestination, DTSUsageType.UT_READONLY, ErrorDetail);
                dft_task.AddRowCount(dataFlowTask, Name + "BadRowCount", out badRowCountTask, variableBadRowCount);
                dataFlowTask.ConnectBadRowsSourceTarget(sourceTask, badRowCountTask);
                BadRowsRedirect(sourceTask, badRowCountTask);
                CManagedComponentWrapper InstanceBadRowDestination = dft_task.AddTargetTask(dataFlowTask, Name + "BadRows", sourceName + "BadRows", TargetType.FlatFile, conMgrBadRows, out badRowTask, string.Empty);
                BadRowsRedirect(badRowCountTask, badRowTask);
                dataFlowTask.ConnectSourceTarget(badRowCountTask, badRowTask);
                //Add column based on source
                dft_task.GenerateColumnForVirtualInput(conMgrBadRows, badRowTask, "\\t", false);
                //Refresh to generate meta data

                InstanceBadRowDestination.ConnectAndReinitializeMetaData(sourceName);
                //Map target columns
                dft_task.MapColumns(badRowTask, InstanceBadRowDestination, DTSUsageType.UT_READONLY, ErrorDetail);
            }
            else if (targetType != TargetType.TableStorage)
            {
                if (targetType == TargetType.Table && sourceType == SourceType.SPList && Convert.ToBoolean(dataFlowSet.PickColumnsFromTarget))
                {
                    dft_task.MapColumnsSPList(targetTask, InstanceDestination, DTSUsageType.UT_READONLY, ErrorDetail);
                }
                else
                {
                    dft_task.MapColumns(targetTask, InstanceDestination, DTSUsageType.UT_READONLY, ErrorDetail);
                }
            }
            //Function to add Event handler SQLTask 
            AddSQLErrorTask(dftTask, Name);
            return dftTask;
        }
        private void AddTruncateOrDeleteSQLTask(IDTSSequence container, string name, string taskName, string targetName, string targetConnectionName, string truncateOrDeleteBeforeInsert, string deleteFiltration = "")
        {
            Executable truncateOrDeleteExe = null;
            if (truncateOrDeleteBeforeInsert.ToUpper() == "DELETE" || truncateOrDeleteBeforeInsert.ToUpper() == "TRUNCATE")
            {
                string strSQLQuery = string.Empty;
                string sqlTaskName = string.Empty;
                if (taskName == null || taskName == string.Empty)
                {
                    taskName = name;
                }
                name = name + "_" + truncateOrDeleteBeforeInsert;

                if (truncateOrDeleteBeforeInsert.ToUpper() == "DELETE")
                {
                    sqlTaskName = "DELETE" + taskName;
                    if (deleteFiltration.Trim() != string.Empty)
                    {
                        strSQLQuery = "Delete from  " + " " + targetName + " WHERE " + deleteFiltration;
                    }
                    else
                    {
                        strSQLQuery = "Delete from  " + " " + targetName;
                    }
                }
                else
                {
                    sqlTaskName = "TRUNCATE_" + taskName;
                    strSQLQuery = "TRUNCATE table " + " " + targetName;
                }
                //pre Log
                Executable sqlTaskPreExe = AddExecutableSQLTask(PreSQL.Replace("<StepName>", name) + ",0", LogConnectionKey, "Pre-" + sqlTaskName, string.Empty, container);
                AddPrecedenceConstraints(sqlTaskPreExe, container);

                //Truncate or Delete
                truncateOrDeleteExe = AddExecutableSQLTask(strSQLQuery, targetConnectionName, sqlTaskName, string.Empty, container);
                AddPrecedenceConstraints(truncateOrDeleteExe, container);
                //Post Log
                Executable sqlTaskPostExe = AddExecutableSQLTask(PostSQL.Replace("<StepName>", name).Replace('?', '0') + "", LogConnectionKey, "Post-" + sqlTaskName, string.Empty, container);
                AddPrecedenceConstraints(sqlTaskPostExe, container);

            }
            else if (truncateOrDeleteBeforeInsert.ToUpper() != "NONE")
            {
                throw new Exception("TRUNCATE or delete before insertion parameter is incorrect : Possible values-DELETE, TRUNCATE or NONE.");

            }

        }
        private void BadRowsRedirect(IDTSComponentMetaData100 sourceTask, IDTSComponentMetaData100 badRowsTask)
        {
            IDTSOutput100 srcOutput = sourceTask.OutputCollection[0];
            IDTSOutputColumnCollection100 srcOutputCols = srcOutput.OutputColumnCollection;
            sourceTask.UsesDispositions = true;
            foreach (IDTSOutputColumn100 outputCol in srcOutputCols)
            {
                outputCol.ErrorRowDisposition = DTSRowDisposition.RD_RedirectRow;
                outputCol.TruncationRowDisposition = DTSRowDisposition.RD_RedirectRow;
            }
        }

        #region AddLogging
        /// <summary>
        /// Enable package level logging.
        /// </summary>
        private void AddVerboseLogging()
        {
            try
            {
                this._Package.LoggingMode = DTSLoggingMode.Enabled;

                // Add a file connection manager for the text log provider.
                ConnectionManager conMgrLog = GetConnection("FileVerboseLoggingProviderConnection"); ;
                if (File.Exists(conMgrLog.ConnectionString + @"\" + _Package.Name + ".log"))
                    File.Delete(conMgrLog.ConnectionString + @"\" + _Package.Name + ".log");
                conMgrLog.ConnectionString = conMgrLog.ConnectionString + @"\" + _Package.Name + ".log";

                // Add a LogProvider.
                LogProvider provider = this._Package.LogProviders.Add(SSISMoniker.LOGPROVIDER_TEXTFILE);
                provider.Name = _Package.Name + "Log";
                provider.ConfigString = conMgrLog.Name;
                this._Package.LoggingOptions.SelectedLogProviders.Add(provider);

            }
            catch (System.NullReferenceException nre)
            {
                System.Diagnostics.Debug.WriteLine(nre.StackTrace);
            }
        }
        #endregion

        public Package GetCurrentPackage()
        {
            if (this.VerboseLogging)
                AddVerboseLogging();
            return this._Package;
        }

    }
}