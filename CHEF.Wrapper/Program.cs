using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Dts.Runtime;              // Managed Runtime namespace           [Microsoft.SqlServer.ManagedDTS.dll]
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;     // Pipeline Primary Interop Assembly   [Microsoft.SqlServer.DTSPipelineWrap.dll]
using wrap = Microsoft.SqlServer.Dts.Runtime.Wrapper;
using Microsoft.SqlServer.Dts.Tasks.ExecuteSQLTask;
using System.Xml.Linq;
using CHEFEngine;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Data.SqlClient;
using System.Data;
using System.Collections;
using System.Configuration;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Management.IntegrationServices;
using Microsoft.SqlServer.Management.Smo;
namespace CHEFWrapper
{
    class Program
    {
        private static string processName = string.Empty;
        private static ProcessTypeNames processType = ProcessTypeNames.Staging;
        private static string strMetaDataXML = string.Empty;
        private static string strGlobalConfigXML = string.Empty;
        private static string processID = string.Empty;
        private static Int32 startStepID = 0;
        private static string executeExistingPackage = string.Empty; 

        static void Main(string[] args)
        {
            WraperCreateSSISPkg(args);
        }
       
        private static void WraperCreateSSISPkg(string[] args)
        {
            const string pkgCreationLog = "_PackageCreation";
            const string sqlLogConnectionKey = "SQLPreAndPostLogKey007";
            const string flatFileConnectionLogKey = "FileVerboseLoggingProviderConnection";
            string PreSQL = "Declare @StepName varchar(255)=left('<StepName>',255) EXEC CHEF.InsertLog @StepName," + (int)JobStatus.Started;
            string PostSQL = "Declare @StepName varchar(255)=left('<StepName>',255) Exec CHEF.InsertLog @StepName,?";
            string QueueNextSQL = "EXEC CHEF.QueueNext";
            string strPackageLocation = string.Empty;
            string strProjectLocation = string.Empty; 
            CatalogFolder catalogFolder = null; 
            bool executeExistingPackage = false;
            
            SqlConnection cn = new SqlConnection(DatabaseConnection.CHEF);
            SqlCommand cm = cn.CreateCommand();
            try
            {
                string isMetaDataFileOrDatabase = ConfigurationManager.AppSettings["MetaDataFileOrDatabase"];
                //Reading Global Config XML form Database
                cn.Open();
                cm.Parameters.Clear();
                cm.CommandType = CommandType.StoredProcedure;
                cm.Parameters.AddWithValue("@MetadataTypeID", MetaDataTypes.GlobalConfig);
                cm.CommandText = "CHEF.GetMetaData";
                SqlDataReader drGlobalConfig = cm.ExecuteReader();
                if (drGlobalConfig.HasRows)
                {
                    drGlobalConfig.Read();
                    strGlobalConfigXML = drGlobalConfig["MetaData"].ToString();
                    
                }
                drGlobalConfig.Close();
                //Reading MetaDataProccess form Database
                cm.Parameters.Clear();
                cm.CommandType = CommandType.StoredProcedure;
                cm.Parameters.AddWithValue("@MetadataTypeID", MetaDataTypes.Process);
                cm.CommandText = "CHEF.GetMetaData";
                SqlDataReader drProcess = cm.ExecuteReader();
                if (drProcess.HasRows)
                {
                    drProcess.Read();
                    strMetaDataXML = drProcess["MetaData"].ToString();
                    processName = drProcess["ProcessName"].ToString();
                    processID = drProcess["ProcessID"].ToString();
                    startStepID = Convert.ToInt32(drProcess["StartStepID"].ToString());
                    processType = (ProcessTypeNames)Enum.Parse(typeof(ProcessTypeNames), drProcess["ProcessTypeName"].ToString());
                }
                drProcess.Close();
                
                XElement root = XElement.Load(new StringReader(strMetaDataXML)); 
                XElement process = root.Element("Process"); 
                if (process.Attribute("ExecuteExistingPackage")== null)
                {
                    executeExistingPackage=false;
                }
                else
                {
                    executeExistingPackage =Convert.ToBoolean(process.Attribute("ExecuteExistingPackage").Value);    
                }
                if (executeExistingPackage)
                {
                    Microsoft.SqlServer.Management.IntegrationServices.PackageInfo packageInfo = null;
                    catalogFolder = CheckCatalogFolder("CHEFFolder");
                    packageInfo = GetPackageFromCatalog(catalogFolder, processID + "_" + processName);
                    if (packageInfo != null)
                    {
                        return;
                    }
                }
                //CHEFMetaData chefMetaData;
                CHEFGlobalConfig chefGlobalConfig;
                // Package Creating Logging
                cm.Parameters.Clear();
                cm.CommandType = CommandType.StoredProcedure;
                cm.Parameters.AddWithValue("@ProcessStep", processName + pkgCreationLog);
                cm.CommandText = "CHEF.InsertLog";
                cm.ExecuteNonQuery();
                SSISBuilder pkg;
                chefGlobalConfig = GetCHEFGlobalConfig<CHEFGlobalConfig>(strGlobalConfigXML);

                /* PUTTING THIS PORTION WITHIN COMMENTS; PACKAGE STORAGE LOCATION NO LONGER REQUIRED SINCE IT IS DEPLOYED TO CATALOG */
                
                //if (chefGlobalConfig.GlobalConfiguration.OutputPackageLocation == null)
                //{
                //    throw new Exception("Invalid Output Package Location");
                //}
                //else
                //{
                //    if (args.Length <= 0)
                //        strPackageLocation = chefGlobalConfig.GlobalConfiguration.OutputPackageLocation;
                //    else
                //        strPackageLocation = args[0].ToString();
                //    //Removing the last successfully created package
                //    if (File.Exists(strPackageLocation)) File.Delete(strPackageLocation);
                //}

                //chefMetaData = GetCHEFMetaData<CHEFMetaData>(strMetaDataXML);
                //Linq 
                pkg = new SSISBuilder(processName, processName);
                //pkg.VerboseLogging = Convert.ToBoolean(chefMetaData.Process.VerboseLogging);
                // Bad Rows error rediction 
                chefGlobalConfig.GlobalConfiguration.BadRowFolderLocation = chefGlobalConfig.GlobalConfiguration.BadRowFolderLocation + @"\" + DateTime.Now.ToString().Replace(':', '-');
                if (!System.IO.Directory.Exists(chefGlobalConfig.GlobalConfiguration.BadRowFolderLocation))
                    System.IO.Directory.CreateDirectory(chefGlobalConfig.GlobalConfiguration.BadRowFolderLocation);
                pkg.BadRowsFolderLocation = chefGlobalConfig.GlobalConfiguration.BadRowFolderLocation;
                // SQL Connection for Log
                pkg.AddConnection(new Connection() { Key = sqlLogConnectionKey, ConnectionString = cn.ConnectionString + ";Provider=SQLNCLI11.1;", ConnectionType = ConnectionTypes.OleDBConnection });
                //File Verbose Logging Provider Connection
                pkg.AddConnection(new Connection() { Key = flatFileConnectionLogKey, ConnectionString = chefGlobalConfig.GlobalConfiguration.LogLocation, ConnectionType = ConnectionTypes.FileConnection });
                //Package Start Execuation Logging
                Executable sqlTaskPre = pkg.AddExecutableSQLTask(PreSQL.Replace("<StepName>", processName + "_PackageExecution"), sqlLogConnectionKey, "Pre-" + processName + "_PackageExecution", "PackageFailSuccess");
                pkg.AddPrecedenceConstraints(sqlTaskPre);
                //Package Level Variables
                pkg.SetVariableValue("PackageFailSuccess", 2, "Int32");
                PackageCreation(strMetaDataXML, pkg, processType.ToString());

                // Process Finished Package Execution Logging
                Executable sqlTaskQueueNext = pkg.AddExecutableSQLTask(QueueNextSQL, sqlLogConnectionKey, "QueueNextETL");
                pkg.AddPrecedenceConstraints(sqlTaskQueueNext);

                // Process Finished Package Execution Logging
                Executable sqlTaskPost = pkg.AddExecutableSQLTask(PostSQL.Replace("<StepName>", processName + "_PackageExecution"), sqlLogConnectionKey, "Post-" + processName + "_PackageExecution", "PackageFailSuccess");
                pkg.AddPrecedenceConstraints(sqlTaskPost);
                
                /* PUTTING THIS PORTION WITHIN COMMENTS; PACKAGE ARCHIVE LOCATION NO LONGER REQUIRED */
                bool  DumpPackageFileOnDisk=false;
                if(ConfigurationManager.AppSettings["DumpPackageFileOnDisk"]==null)
                {
                    DumpPackageFileOnDisk=false;
                }
                else if(ConfigurationManager.AppSettings["DumpPackageFileOnDisk"].ToString()=="True")
                {
                    DumpPackageFileOnDisk=true;
                }
                if(System.IO.Directory.Exists(chefGlobalConfig.GlobalConfiguration.OutputPackageLocation))
                {
                     System.IO.Directory.Delete(chefGlobalConfig.GlobalConfiguration.OutputPackageLocation);
                }
                if(DumpPackageFileOnDisk)
                {
                    strPackageLocation = chefGlobalConfig.GlobalConfiguration.OutputPackageLocation;
                    pkg.SaveAsPackage(strPackageLocation);
                }
                strProjectLocation = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + @"\Temp"; 
                catalogFolder = CheckCatalogFolder("CHEFFolder");
                CreateProjectAndDeploy(catalogFolder, strProjectLocation, pkg.GetCurrentPackage());
                // Logging successful creation of Package
                cm.Parameters.Clear();
                cm.CommandText = "CHEF.InsertLog";
                cm.CommandType = CommandType.StoredProcedure;
                cm.Parameters.AddWithValue("@ProcessStep", processName + pkgCreationLog);
                cm.Parameters.AddWithValue("@StatusID", 2);
                cm.ExecuteNonQuery();
                cm.Parameters.Clear();
            }
            catch (Exception exx)
            {
                string errorMessage = exx.Message;
                if (errorMessage.Length > 500)
                {
                    errorMessage = errorMessage.Substring(0, 499);
                }
                cm.CommandText = "CHEF.InsertLog";
                cm.Parameters.Clear();
                cm.CommandType = CommandType.StoredProcedure;
                cm.Parameters.AddWithValue("@ProcessStep", processName + pkgCreationLog);
                cm.Parameters.AddWithValue("@Description", errorMessage);
                cm.Parameters.AddWithValue("@StatusID", JobStatus.Failed);
                cm.ExecuteNonQuery();
                //throw (exx);
            }
            finally
            {
                cn.Close();
                cm.Dispose();
            }
        }
        private static CHEFMetaDataProcessConnectionSet GetConnectionSet<CHEFMetaDataProcessConnectionSet>(string xml)
        {
            StringReader stream = null;
            XmlTextReader reader = null;
            try
            {
                XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(CHEFMetaDataProcessConnectionSet));
                stream = new StringReader(xml); // read xml data
                reader = new XmlTextReader(stream); // create reader
                // covert reader to object
                return (CHEFMetaDataProcessConnectionSet)serializer.Deserialize(reader);
            }
            catch (Exception ex)
            {
                //return default(CHEFMetaDataProcessConnectionSet);
                throw new Exception(ex.Message);
            }
            finally
            {
                if (stream != null) stream.Close();
                if (reader != null) reader.Close();
            }
        }
        private static CHEFMetaDataProcessVariablesVariable GetVariableSet<CHEFMetaDataProcessVariablesVariable>(string xml)
        {
            StringReader stream = null;
            XmlTextReader reader = null;
            try
            {
                XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(CHEFMetaDataProcessVariablesVariable));
                stream = new StringReader(xml); // read xml data
                reader = new XmlTextReader(stream); // create reader
                // covert reader to object
                return (CHEFMetaDataProcessVariablesVariable)serializer.Deserialize(reader);
            }
            catch (Exception ex)
            {
                //return default(CHEFMetaDataProcessVariablesVariable);
                throw new Exception(ex.Message);
            }
            finally
            {
                if (stream != null) stream.Close();
                if (reader != null) reader.Close();
            }
        }
        private static SetVariables GetSetVariableSet<SetVariables>(string xml)
        {
            StringReader stream = null;
            XmlTextReader reader = null;
            try
            {
                XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(SetVariables));
                stream = new StringReader(xml); // read xml data
                reader = new XmlTextReader(stream); // create reader
                // covert reader to object
                return (SetVariables)serializer.Deserialize(reader);
            }
            catch (Exception ex)
            {
                //return default(CHEFMetaDataProcessVariablesVariable);
                throw new Exception(ex.Message);
            }
            finally
            {
                if (stream != null) stream.Close();
                if (reader != null) reader.Close();
            }
          }
        private static SendMailTask GetSendEmailTaskSet<SendMailTask>(string xml)
        {
            StringReader stream = null;
            XmlTextReader reader = null;
            try
            {
                XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(SendMailTask));
                stream = new StringReader(xml); // read xml data
                reader = new XmlTextReader(stream); // create reader
                // covert reader to object
                return (SendMailTask)serializer.Deserialize(reader);
            }
            catch (Exception ex)
            {
                //return default(CHEFMetaDataProcessVariablesVariable);
                throw new Exception(ex.Message);
            }
            finally
            {
                if (stream != null) stream.Close();
                if (reader != null) reader.Close();
            }
        }
        private static CHEFMetaDataProcessStepSQLTaskSet GetSQLTaskSet<CHEFMetaDataProcessStepSQLTaskSet>(string xml)
        {
            StringReader stream = null;
            XmlTextReader reader = null;
            try
            {
                XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(CHEFMetaDataProcessStepSQLTaskSet));
                stream = new StringReader(xml); // read xml data
                reader = new XmlTextReader(stream); // create reader
                // covert reader to object
                return (CHEFMetaDataProcessStepSQLTaskSet)serializer.Deserialize(reader);
            }
            catch (Exception ex)
            {
                //return default(CHEFMetaDataProcessVariablesVariable);
                throw new Exception(ex.Message);
            }
            finally
            {
                if (stream != null) stream.Close();
                if (reader != null) reader.Close();
            }
        }
        private static CHEFMetaDataProcessStepDataFlowSet GetDataFlowSet<CHEFMetaDataProcessStepDataFlowSet>(string xml)
        {
            StringReader stream = null;
            XmlTextReader reader = null;
            try
            {
                XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(CHEFMetaDataProcessStepDataFlowSet));
                stream = new StringReader(xml); // read xml data
                reader = new XmlTextReader(stream); // create reader
                // covert reader to object
                return (CHEFMetaDataProcessStepDataFlowSet)serializer.Deserialize(reader);
            }
            catch (Exception ex)
            {
                //return default(CHEFMetaDataProcessVariablesVariable);
                throw new Exception(ex.Message);
            }
            finally
            {
                if (stream != null) stream.Close();
                if (reader != null) reader.Close();
            }
        }
        private static CHEFMetaDataProcessStepTableStorageSet GetTableStorageSet<CHEFMetaDataProcessStepTableStorageSet>(string xml)
        {
            StringReader stream = null;
            XmlTextReader reader = null;
            try
            {
                XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(CHEFMetaDataProcessStepTableStorageSet));
                stream = new StringReader(xml); // read xml data
                reader = new XmlTextReader(stream); // create reader
                // covert reader to object
                return (CHEFMetaDataProcessStepTableStorageSet)serializer.Deserialize(reader);
            }
            catch (Exception ex)
            {
                //return default(CHEFMetaDataProcessVariablesVariable);
                throw new Exception(ex.Message);
            }
            finally
            {
                if (stream != null) stream.Close();
                if (reader != null) reader.Close();
            }
        }
        private static CHEFMetaDataProcessStepPackageExecution GetPackageExecutionSet<CHEFMetaDataProcessStepPackageExecution>(string xml)
        {
            StringReader stream = null;
            XmlTextReader reader = null;
            try
            {
                XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(CHEFMetaDataProcessStepPackageExecution));
                stream = new StringReader(xml); // read xml data
                reader = new XmlTextReader(stream); // create reader
                // covert reader to object
                return (CHEFMetaDataProcessStepPackageExecution)serializer.Deserialize(reader);
            }
            catch (Exception ex)
            {
                //return default(CHEFMetaDataProcessVariablesVariable);
                throw new Exception(ex.Message);
            }
            finally
            {
                if (stream != null) stream.Close();
                if (reader != null) reader.Close();
            }
        }
        private static CHEFMetaDataProcessStep GetStepsSet<CHEFMetaDataProcessStep>(string xml)
        {
            StringReader stream = null;
            XmlTextReader reader = null;
            try
            {
                XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(typeof(CHEFMetaDataProcessStep));
                stream = new StringReader(xml); // read xml data
                reader = new XmlTextReader(stream); // create reader
                // covert reader to object
                return (CHEFMetaDataProcessStep)serializer.Deserialize(reader);
            }
            catch (Exception ex)
            {
                //return default(CHEFMetaDataProcessVariablesVariable);
                throw new Exception(ex.Message);
            }
            finally
            {
                if (stream != null) stream.Close();
                if (reader != null) reader.Close();
            }
        }
        private static void PackageCreation(string xml, SSISBuilder pkg, string processType)
        {
            XElement root = XElement.Load(new StringReader(xml));
            XElement process = root.Element("Process");
            pkg.VerboseLogging = Convert.ToBoolean(process.Attribute("VerboseLogging").Value);
            foreach (XElement node in process.Elements())
            {
                //string stepName = steps.Attribute("Name").Value;
                if (node.Name!=null && node.Name.ToString().ToUpper() == "CONNECTIONSET")
                {
                    CHEFMetaDataProcessConnectionSet con;
                    con = GetConnectionSet<CHEFMetaDataProcessConnectionSet>(node.ToString());
                    // for SQL Connection
                    if (con != null)
                    {
                        if (con.SQLConnection != null)
                            foreach (var oleCon in con.SQLConnection)
                            {
                                if (oleCon.key != string.Empty)
                                {
                                    if (oleCon.Encrypt != null && oleCon.Encrypt.ToUpper() == "FALSE")
                                    {
                                        oleCon.Encrypt = "False";
                                    }
                                    else
                                    {
                                        oleCon.Encrypt = "True";
                                    }

                                    if (oleCon.TrustedConnection != null && oleCon.TrustedConnection.ToUpper() == "FALSE")
                                    {
                                        ConnectionEncryption conEncryption=new ConnectionEncryption();
                                        string strPassword = conEncryption.DecryptString(oleCon.EncryptedPassword);
                                        string connectionString = System.Configuration.ConfigurationManager.AppSettings["Database_Standard_Connection"].Replace("[DataBase_Name]", oleCon.DatabaseName).Replace("[Server_Name]", oleCon.ServerName).Replace("[Encrypt_Connection]", oleCon.Encrypt).Replace("[User_ID]", oleCon.UserID).Replace("[User_Password]", strPassword).Replace("[Encryption]", oleCon.Encrypt);
                                        pkg.AddConnection(new Connection() { Key = oleCon.key, ConnectionString = connectionString, ConnectionType = ConnectionTypes.OleDBConnection });
                                    }
                                    else
                                    {
                                        string connectionString = System.Configuration.ConfigurationManager.AppSettings["Database_Trusted_Connection"].Replace("[DataBase_Name]", oleCon.DatabaseName).Replace("[Server_Name]", oleCon.ServerName).Replace("[Encryption]",oleCon.Encrypt);
                                        pkg.AddConnection(new Connection() { Key = oleCon.key, ConnectionString = connectionString, ConnectionType = ConnectionTypes.OleDBConnection });
                                    }
                                }
                            }
                        if (con.SharePointListConnection != null)
                            foreach (var oleCon in con.SharePointListConnection)
                            {
                                if (oleCon.key != string.Empty)
                                {

                                    if (oleCon.UseWindowsAuthentication != null && oleCon.UseWindowsAuthentication.ToUpper() == "FALSE")
                                    {
                                        ConnectionEncryption conEncryption = new ConnectionEncryption();
                                        string strPassword = conEncryption.DecryptString(oleCon.EncryptedPassword);
                                        string connectionString = "SiteURL=" + oleCon.SiteURL+";UserID="+oleCon.UserID+";Password="+strPassword;
                                        pkg.AddConnection(new Connection() { Key = oleCon.key, ConnectionString = connectionString, ConnectionType = ConnectionTypes.SharePointListConnection});
                                    }
                                    else
                                    {
                                        string connectionString = oleCon.SiteURL;
                                        pkg.AddConnection(new Connection() { Key = oleCon.key, ConnectionString = connectionString, ConnectionType = ConnectionTypes.SharePointListConnection });
                                    }
                                }
                            }
                        if (con.TableStorageConnection!= null)
                            foreach (var oleCon in con.TableStorageConnection)
                            {
                                if (oleCon.key != null && oleCon.key != string.Empty)
                                {
                                    string connectionString = string.Empty;
                                    if (oleCon.UseDevelopmentStorage != null && oleCon.UseDevelopmentStorage.ToUpper() == "TRUE")
                                    {
                                        connectionString = "UseDevelopmentStorage=true";
                                    }
                                    else
                                    {
                                        ConnectionEncryption conEncryption = new ConnectionEncryption();
                                        string strAccountKey = conEncryption.DecryptString(oleCon.EncryptedAccountKey);
                                        connectionString = "DefaultEndpointsProtocol=" + oleCon.DefaultEndpointsProtocol.Trim() + ";AccountName=" + oleCon.AccountName + ";AccountKey=" + strAccountKey;

                                    }
                                    pkg.AddConnection(new Connection() { Key = oleCon.key, ConnectionString = connectionString, ConnectionType = ConnectionTypes.TableStorageConnection });
                                }
                                else
                                {
                                    throw new Exception("Invalid TableStorage Connection");
                                }
                            }
                        // for File Connection
                        if (con.FileConnection != null)
                            foreach (var fileCon in con.FileConnection)
                            {
                                //not able to make two different file connection
                                if (fileCon.key != string.Empty)
                                    pkg.AddConnection(new Connection() { Key = fileCon.key, ConnectionString = fileCon.FileName, ConnectionType = ConnectionTypes.FileConnection });
                            }
                        // for FlatFile  Connection
                        if (con.FlatFileConnection != null)
                            foreach (var flatFileCon in con.FlatFileConnection)
                            {
                                if (flatFileCon.key != string.Empty)
                                    pkg.AddConnection(new Connection() { Key = flatFileCon.key, ConnectionString = flatFileCon.FileName, ConnectionType = ConnectionTypes.FlatFileConnection });
                            }
                        // for FlatFile  Connection
                        if (con.SMTPConnection != null)
                            foreach (var smtpCon in con.SMTPConnection)
                            {
                                string smpt = string.Empty;
                                if (smtpCon.key != string.Empty)
                                {
                                    if (smtpCon.UseWindowsAuthentication == string.Empty || smtpCon.UseWindowsAuthentication ==null)
                                    {
                                        smtpCon.UseWindowsAuthentication = "False";
                                    }
                                    if (smtpCon.EnableSsl == string.Empty || smtpCon.EnableSsl == null)
                                    {
                                        smtpCon.EnableSsl = "False";
                                    }
                                    smpt = "SmtpServer=" + smtpCon.SmtpServer + ";UseWindowsAuthentication=" + smtpCon.UseWindowsAuthentication + ";EnableSsl=" + smtpCon.EnableSsl + ";";
                                    pkg.AddConnection(new Connection() { Key = smtpCon.key, ConnectionString = smpt, ConnectionType = ConnectionTypes.SMTPConnection });
                                }
                            }
                    }
                }
                else if (node.Name!=null &&  node.Name.ToString().ToUpper() == "VARIABLES")
                {
                    foreach (var variableNode in node.Elements())
                    {
                        CHEFMetaDataProcessVariablesVariable variable;
                        variable = GetVariableSet<CHEFMetaDataProcessVariablesVariable>(variableNode.ToString());
                        if (variable != null)
                        {
                            pkg.SetVariableValue(variable.Name, variable.Value, variable.DataType);
                        }
                    }
                }
                else if (node.Name != null && node.Name.ToString().ToUpper() == "SETVARIABLES")
                {
                    SetVariables setvariable;

                    setvariable = GetSetVariableSet<SetVariables>(node.ToString());
                    if (setvariable != null)
                    {
                        pkg.SetVariableRuntime(setvariable.SetVariable, "Need To Complete", "");
                    }

                }
                else if (node.Name != null &&  node.Name.ToString().ToUpper() == "SENDMAILTASK")
                {
                    SendMailTask sendMailTask;
                    sendMailTask = GetSendEmailTaskSet<SendMailTask>(node.ToString());
                    if (sendMailTask != null)
                    {
                        pkg.AddSendMailTask(sendMailTask.Name, sendMailTask.SMTPServer, sendMailTask.From, sendMailTask.To, sendMailTask.CC, sendMailTask.BCC, sendMailTask.Subject, sendMailTask.MessageSource, sendMailTask.Attachments, sendMailTask.Priority, sendMailTask.MessageSourceType);
                    }
                }
                else if (node.Name != null &&  node.Name.ToString().ToUpper() == "STEP")
                {
                    //CHEFMetaDataProcessStep step;
                    //step = GetStepsSet<CHEFMetaDataProcessStep>(node.ToString());
                    string stepType = ProcessTypeNames.Staging.ToString();
                    Int32 stepID= 0;
                    if(node.Attribute("TypeName")!=null)
                        stepType = node.Attribute("TypeName").Value;

                    if (node.Attribute("ID") != null)
                        stepID = Convert.ToInt32(node.Attribute("ID").Value);
                    string stepName = node.Attribute("Name").Value;
                    foreach (var stepNode in node.Elements())
                    {
                        // This loop is for DataFlowTask
                        if (stepType == processType.ToString() && stepType != string.Empty && stepID >=startStepID)
                        {
                            if (stepNode.Name != null && stepNode.Name.ToString().ToUpper() == "VARIABLES")
                            {
                                foreach (var variableNode in stepNode.Elements())
                                {
                                    CHEFMetaDataProcessVariablesVariable variable;
                                    variable = GetVariableSet<CHEFMetaDataProcessVariablesVariable>(variableNode.ToString());
                                    if (variable != null)
                                    {
                                        pkg.SetVariableValue(variable.Name, variable.Value, variable.DataType);
                                    }
                                }
                            }
                            else if (stepNode.Name != null &&  stepNode.Name.ToString().ToUpper() == "SETVARIABLES")
                            {
                                SetVariables setvariable;

                                setvariable = GetSetVariableSet<SetVariables>(stepNode.ToString());
                                if (setvariable != null)
                                {
                                    pkg.SetVariableRuntime(setvariable.SetVariable, stepName, "");
                                }

                            }
                            else if (stepNode.Name != null && stepNode.Name.ToString().ToUpper() == "SENDMAILTASK")
                            {
                                SendMailTask sendMailTask;
                                sendMailTask = GetSendEmailTaskSet<SendMailTask>(stepNode.ToString());
                                if (sendMailTask != null)
                                {
                                    pkg.AddSendMailTask(sendMailTask.Name, sendMailTask.SMTPServer, sendMailTask.From, sendMailTask.To, sendMailTask.CC, sendMailTask.BCC, sendMailTask.Subject, sendMailTask.MessageSource, sendMailTask.Attachments, sendMailTask.Priority, sendMailTask.MessageSourceType);
                                }
                            }
                            else if (stepNode.Name != null &&  stepNode.Name.ToString().ToUpper() == "SQLTASKSET")
                            {
                                CHEFMetaDataProcessStepSQLTaskSet sQLTaskSet;
                                sQLTaskSet = GetSQLTaskSet<CHEFMetaDataProcessStepSQLTaskSet>(stepNode.ToString());
                                if (sQLTaskSet != null)
                                {
                                    pkg.AddSQLTaskSet(sQLTaskSet, stepName);
                                }
                            }
                            // This loop is for PackageExecution task
                            else if (stepNode.Name != null &&  stepNode.Name.ToString().ToUpper() == "PACKAGEEXECUTION")
                            {
                                CHEFMetaDataProcessStepPackageExecution packageExecution;
                                packageExecution = GetPackageExecutionSet<CHEFMetaDataProcessStepPackageExecution>(stepNode.ToString());
                                if (packageExecution != null)
                                {
                                    pkg.AddExecutablePackage(packageExecution.PackageName, packageExecution.Connection);
                                }
                                
                            }
                            else if (stepNode.Name != null &&  stepNode.Name.ToString().ToUpper() == "DATAFLOWSET")
                            {

                                CHEFMetaDataProcessStepDataFlowSet dataFlowSet;
                                dataFlowSet = GetDataFlowSet<CHEFMetaDataProcessStepDataFlowSet>(stepNode.ToString());
                                if (dataFlowSet != null)
                                {
                                    pkg.AddDataFlowSet(dataFlowSet, stepName);
                                }
                            }
                            else if (stepNode.Name != null && stepNode.Name.ToString().ToUpper() == "TABLESTORAGESET")
                            {

                                CHEFMetaDataProcessStepTableStorageSet tableStorageSet;
                                tableStorageSet = GetTableStorageSet<CHEFMetaDataProcessStepTableStorageSet>(stepNode.ToString());
                                if (tableStorageSet != null)
                                {
                                    //pkg.AddDataFlowSet(tableStorageSet, stepName);
                                }
                            }
                        }
                    }
                }
            }
            
        }
        private static CHEFMetaData GetCHEFMetaData<CHEFMetaData>(string xml)
        {
            StringReader stream = null;
            XmlTextReader reader = null;
            try
            {
                // serialise to object
                XmlSerializer serializer = new XmlSerializer(typeof(CHEFMetaData));
                stream = new StringReader(xml); // read xml data
                reader = new XmlTextReader(stream); // create reader
                // covert reader to object
                return (CHEFMetaData)serializer.Deserialize(reader);
            }
            catch
            {
                return default(CHEFMetaData);
            }
            finally
            {
                if (stream != null) stream.Close();
                if (reader != null) reader.Close();
            }
        }
        private static CHEFGlobalConfig GetCHEFGlobalConfig<CHEFGlobalConfig>(string xml)
        {
            StringReader stream = null;
            XmlTextReader reader = null;
            try
            {
                // serialise to object
                XmlSerializer serializer = new XmlSerializer(typeof(CHEFGlobalConfig));
                
                stream = new StringReader(xml); // read xml data
                reader = new XmlTextReader(stream); // create reader
                // covert reader to object
                return (CHEFGlobalConfig)serializer.Deserialize(reader);
            }
            catch
            {
                return default(CHEFGlobalConfig);
            }
            finally
            {
                if (stream != null) stream.Close();
                if (reader != null) reader.Close();
            }
        }

        //MyCode: Check if catalog exists and if folder exists; create if not.
        private static CatalogFolder CheckCatalogFolder(string folderName)
        {
            string serverName = ConfigurationManager.ConnectionStrings["CHEF"].ConnectionString.Split(';')[1];
            int index = serverName.IndexOf('=');
            serverName = serverName.Substring(index + 1);
            Server server = new Server(serverName);
            IntegrationServices integrationServices = new IntegrationServices(server);
            Catalog catalog = integrationServices.Catalogs["SSISDB"];
            CatalogFolder catalogFolder = null;
            if (catalog == null)
            {
                //Throw exception if catalog doesn't exist
                throw new Exception("Catalog not found. Please create the SSISDB catalog and try again.");
            }
            else
            {
                catalogFolder = catalog.Folders[folderName];
                if (catalogFolder == null)
                {
                    //Create catalog folder if it doesn't exist
                    catalogFolder = new CatalogFolder(catalog, folderName, "This is the folder which contains all projects generated through CHEF");
                    catalogFolder.Create();
                }
            }
            return catalogFolder;  
        }

        private static void CreateProjectAndDeploy (CatalogFolder catalogFolder, string strProjectLocation, Package package)
        {
            byte[] projectStream = null;

            if (catalogFolder.Name != "CHEFFolder")
                return;

            //Create temporary location if it doesn't exist
            if (!Directory.Exists(strProjectLocation))
                Directory.CreateDirectory(strProjectLocation);
            
            //Create Project and add the Package to it
            using (Project project = Project.CreateProject(strProjectLocation + @"\TempProject.ispac"))
            {
                project.Name = processID + "_" + processName + "_" + processType;
                project.Description = "Project Description";
                //package.Parameters.Add("logValue", TypeCode.Boolean);
                //package.Parameters["logValue"].Value = true;
                project.PackageItems.Add(package, "Package.dtsx");
                project.PackageItems[0].Package.Description = "Package Description";
                //project.Parameters.Add("logValue", TypeCode.Boolean);
                project.Save();
            }

            //Convert the Project to equivalent byte stream
            using (FileStream fileStream = new FileStream(strProjectLocation + @"\TempProject.ispac", FileMode.Open, FileAccess.Read))
            {
                byte[] stream = new byte[fileStream.Length];
                int numberOfBytesLeft = (int)fileStream.Length;
                int numberOfBytesRead = 0;
                while (numberOfBytesLeft > 0)
                {
                    int n = fileStream.Read(stream, numberOfBytesRead, numberOfBytesLeft);
                    if (n == 0) //end of file
                        break;
                    numberOfBytesRead += n;
                    numberOfBytesLeft -= n;
                }
                projectStream = stream;
            }

            //Deploy the Project
            using (Project project = Project.OpenProject(strProjectLocation + @"\TempProject.ispac"))
            {
                catalogFolder.DeployProject(processID + "_" + processName+"_"+processType, projectStream);
                catalogFolder.Alter();
            }

            //Delete temporary location
            Directory.Delete(strProjectLocation, true);

            //SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["CHEF"].ConnectionString);
            //conn.Open();
            //SqlCommand comm = conn.CreateCommand();
            //comm.CommandType = CommandType.Text;
            //comm.CommandText = "INSERT INTO [CHEF].[RequestQueue] VALUES(MONTH(SYSDATETIME()), YEAR(SYSDATETIME()), " + processID + ", " + strStartStepID + ", 1, NEWID(), SUSER_SNAME(), SYSDATETIME(), SYSDATETIME())";
            //comm.ExecuteNonQuery();
            //conn.Close();
            //comm.Dispose();
        }

        public static Microsoft.SqlServer.Management.IntegrationServices.PackageInfo GetPackageFromCatalog(CatalogFolder catalogFolder, string projectName)
        {
            Microsoft.SqlServer.Management.IntegrationServices.PackageInfo package = null;
            ProjectInfo project = catalogFolder.Projects[projectName];
            if (project == null)
            {
                return package; //returns null
            }
            package = project.Packages["Package.dtsx"];
            return package;
        }
    }


    enum TargetTypes
    {
        Excel,
        FlatFile,
        Table
    }
    enum ProcessTypeNames
    {
        Staging = 1,
        Warehouse,
        ReportMart
    }
    enum JobStatus
    {
        Queued,
        Started,
        Finished,
        Stopped,
        Failed
    }
    enum MetaDataTypes
    {
        Process,
        GlobalConfig,
        XSD
    }

}
