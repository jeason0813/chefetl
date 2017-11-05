using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using wrap = Microsoft.SqlServer.Dts.Runtime.Wrapper;
using Microsoft.SqlServer.Dts.Tasks.ExecuteSQLTask;
using System.Data.SqlClient;
namespace CHEFEngine
{
    public class DFTTask
    {
        #region DFT Task
        public Executable AddDFTTask(IDTSSequence _Package, string Name, int iCount)
        {
            Executable exe = _Package.Executables.Add(SSISMoniker.PIPE_LINE);
            TaskHost taskHost = exe as TaskHost;
            taskHost.Name = Name + iCount;
            taskHost.DelayValidation = true;

            return exe;
        }
        #endregion

        #region SOURCE
        public MainPipe AddSourceTask(MainPipe dataFlow, string Name, string SourceName, SourceType Sourcetype,
                                    ConnectionManager conMgrSource,
                                    out IDTSComponentMetaData100 sourceTask, bool SQLStatementASVariable = false, string ConnectionString = "")
        {
            CManagedComponentWrapper InstanceSource;
            switch (Sourcetype)
            {
                case SourceType.SELECTSQL:
                    sourceTask = dataFlow.AddPipeLine(SSISMoniker.OLEDB_SOURCE, Name + " Source");
                    break;
                case SourceType.Table:
                    sourceTask = dataFlow.AddPipeLine(SSISMoniker.OLEDB_SOURCE, Name + " Source");
                    break;
                case SourceType.FlatFile:
                    sourceTask = dataFlow.AddPipeLine(SSISMoniker.FLAT_FILE_SOURCE, Name + " Source");
                    break;
                case SourceType.Excel:
                    sourceTask = dataFlow.AddPipeLine(SSISMoniker.EXCEL_SOURCE, Name + " Source");
                    break;
                case SourceType.TableStorage:
                    sourceTask = dataFlow.AddPipeLine(SSISMoniker.OLEDB_TABLESTORAGE_SOURCE, Name + " Source");
                    break;
                case SourceType.SPList:
                    sourceTask = dataFlow.AddPipeLine(SSISMoniker.OLEDB_SPList_SOURCE, Name + " Source");
                    break;
                default:
                    throw new Exception("Feature not implemented yet");
            }
            InstanceSource = sourceTask.InitializeTask();
            if (Sourcetype != SourceType.TableStorage && Sourcetype != SourceType.SPList)
            {
                sourceTask.SetConnection(conMgrSource);
            }
          
            switch (Sourcetype)
            {
                case SourceType.SELECTSQL:
                    InstanceSource.SetSQLSource(SourceName, SQLStatementASVariable);
                    break;
                case SourceType.Table:
                    InstanceSource.SetTableSource(SourceName);
                    break;
                case SourceType.Excel:
                    InstanceSource.SetTableSource(SourceName);
                    break;
                case SourceType.FlatFile:
                    
                    break;
                case SourceType.TableStorage:
                    InstanceSource.SetTableStorageSource(SourceName, ConnectionString);
                    break;
                case SourceType.SPList:
                    InstanceSource.SetSharePointListSource(SourceName, ConnectionString);
                    break;
                default:
                    throw new Exception("Feature not implemented yet");
            }

            InstanceSource.ConnectAndReinitializeMetaData(Name);

            return dataFlow;
        }
        #endregion

        #region Target
        public CManagedComponentWrapper AddTargetTask(MainPipe dataFlow, string Name,
                                            string TargetName, TargetType Targettype, ConnectionManager conMgrDestination,
                                            out IDTSComponentMetaData100 targetTask,string tableStorageConnectionString)
        {
            string tName = Name + " Destination";
            string classID = "";
            switch (Targettype)
            {
                case TargetType.Excel: classID = SSISMoniker.EXCEL_DESTINATION; break;
                case TargetType.FlatFile: classID = SSISMoniker.FLAT_FILE_DESTINATION; break;
                case TargetType.Table: classID = SSISMoniker.OLEDB_DESTINATION; break;
                case TargetType.TableStorage: classID = SSISMoniker.OLEDB_TABLESTORAGE_DESTINATION; break;
                case TargetType.SPList: classID = SSISMoniker.OLEDB_SP_DESTINATION; break;
            }

            targetTask = dataFlow.AddPipeLine(classID, tName);
            CManagedComponentWrapper InstanceDestination = targetTask.InitializeTask();

            if (Targettype == TargetType.TableStorage)
            {
                InstanceDestination.SetTableStorageDestination(TargetName, tableStorageConnectionString);
            }
            if (Targettype == TargetType.SPList)
            {
                InstanceDestination.SetSharePointListDestination(TargetName, tableStorageConnectionString);
            }
            else
            {
                targetTask.SetConnection(conMgrDestination);
            }

            if (Targettype == TargetType.Table)
            {
                InstanceDestination.SetTableDestination(TargetName);
                InstanceDestination.ConnectAndReinitializeMetaData(TargetName);
            }
            else if (Targettype == TargetType.FlatFile)
            {
            }

            return InstanceDestination;
        }
        #endregion

        #region Column mapping
        public void MapColumns(IDTSComponentMetaData100 DestinationTask
                               , CManagedComponentWrapper InstanceDestination
                               , DTSUsageType dtsUsageType,string ErrorDetail)
        {
            #region map the columns
            IDTSInput100 input = DestinationTask.InputCollection[0];
            IDTSVirtualInput100 vInput = input.GetVirtualInput();
            IDTSInputColumn100 vCol = null;
            string columnName=string.Empty;
            try
            {
                if (dtsUsageType == DTSUsageType.UT_READONLY)
                {
                    foreach (IDTSVirtualInputColumn100 vColumn in vInput.VirtualInputColumnCollection)
                    {
                        InstanceDestination.SetUsageType(input.ID, vInput, vColumn.LineageID, dtsUsageType);
                    }

                    foreach (IDTSInputColumn100 col in input.InputColumnCollection)
                    {
                        columnName=col.Name;
                        IDTSExternalMetadataColumn100 exCol = input.ExternalMetadataColumnCollection[col.Name];
                        InstanceDestination.MapInputColumn(input.ID, col.ID, exCol.ID);
                    }
                }
                else
                {
                    foreach (IDTSVirtualInputColumn100 vColumn in vInput.VirtualInputColumnCollection)
                    {
                        vCol = InstanceDestination.SetUsageType(input.ID, vInput, vColumn.LineageID, dtsUsageType);
                        columnName=vCol.Name;
                        IDTSExternalMetadataColumn100 exCol = input.ExternalMetadataColumnCollection[vColumn.Name];
                        InstanceDestination.MapInputColumn(input.ID, vCol.ID, exCol.ID);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Table Mapping failed for source to destination for the column '" + columnName+"'. "+ErrorDetail);
            }
            #endregion
        }
        #endregion
        public void MapColumnsSPList(IDTSComponentMetaData100 DestinationTask
                               , CManagedComponentWrapper InstanceDestination
                               , DTSUsageType dtsUsageType,string ErrorDetail)
        {
            #region map the columns
            IDTSInput100 input = DestinationTask.InputCollection[0];
            IDTSVirtualInput100 vInput = input.GetVirtualInput();
            IDTSInputColumn100 vCol = null;
            string columnName = string.Empty;
            try
            {
                  foreach (IDTSExternalMetadataColumn100 exCol in input.ExternalMetadataColumnCollection)
                    {
                        columnName = exCol.Name;
                        IDTSVirtualInputColumn100 vColumn = vInput.VirtualInputColumnCollection[exCol.Name];
                        vCol = InstanceDestination.SetUsageType(input.ID, vInput, vColumn.LineageID, dtsUsageType);
                        InstanceDestination.MapInputColumn(input.ID, vCol.ID, exCol.ID);
                    }
            }
            catch (Exception ex)
            {
                throw new Exception("Table Mapping failed for source to destination for the column '" + columnName + "'. " + ErrorDetail);
            }
          
        }
        public void GenerateColumnForVirtualInput(ConnectionManager conMgrDestination,
                                                   IDTSComponentMetaData100 flatFileDestination, string ColumnDelimeter, bool UniCode)
        {
            // Add columns to the FlatFileConnectionManager
            wrap.IDTSConnectionManagerFlatFile100 fileConnection = null;
            fileConnection = conMgrDestination.InnerObject as wrap.IDTSConnectionManagerFlatFile100;
            fileConnection.Unicode = UniCode;
            DtsConvert.GetExtendedInterface(conMgrDestination);
            if (fileConnection == null) throw new Exception("Invalid file connection");
            // Get the upstream columns
            IDTSVirtualInputColumnCollection100 vColumns = flatFileDestination.InputCollection[0].GetVirtualInput().VirtualInputColumnCollection;

            for (int cols = 0; cols < vColumns.Count; cols++)
            {
                wrap.IDTSConnectionManagerFlatFileColumn100 col = fileConnection.Columns.Add();

                // If this is the last column, set the delimiter to CRLF.
                // Otherwise keep the delimiter as ",".
                if (cols == vColumns.Count - 1)
                {
                    col.ColumnDelimiter = "\r\n";
                }
                else
                {
                    if (ColumnDelimeter == "\\t" || ColumnDelimeter == "Tab {t}")
                        col.ColumnDelimiter = "\t";
                    else
                    {
                        col.ColumnDelimiter = ColumnDelimeter;
                    }   
                    col.ColumnDelimiter = "\t";
                }
                col.ColumnType = "Delimited";
                col.DataType = vColumns[cols].DataType;
                col.DataPrecision = vColumns[cols].Precision;
                col.DataScale = vColumns[cols].Scale;
                wrap.IDTSName100 name = col as wrap.IDTSName100;
                name.Name = vColumns[cols].Name;
            }
        }
        #endregion
        public void AddVirtualColumnsFromTarget(ConnectionManager conMgrSource,IDTSComponentMetaData100 sourceTask,
                                                   string TargetName, string ColumnDelimeter, bool IsColumnNamesInFirstDataRow, bool AllowFlatFileTruncate, bool UniCode, string strConn)
        {
            // Get native flat file connection 
            string serverName = string.Empty;
            string databaseName = string.Empty;
            string password = string.Empty;
            string userID = string.Empty;
            string sqlConnection = string.Empty;
            foreach (string connString in strConn.Split(';'))
            {
                if (connString.Contains("Data Source"))
                {
                    serverName = connString.Substring(connString.IndexOf("=") + 1);
                }
                else if (connString.Contains("Initial Catalog"))
                {
                    databaseName = connString.Substring(connString.IndexOf("=") + 1);
                }
                else if (connString.Contains("User ID"))
                {
                    userID = connString.Substring(connString.IndexOf("=") + 1);
                }
                else if (connString.Contains("Password"))
                {
                    password = connString.Substring(connString.IndexOf("=") + 1);
                }
            }
            if (userID != null && userID != string.Empty)
            {
                sqlConnection = string.Format("Data Source={0};Initial Catalog={1};User ID={2};Password={3};", serverName, databaseName,userID,password);
            }
            else
            {
               sqlConnection= string.Format("Data Source={0};Initial Catalog={1};Integrated Security=SSPI;", serverName, databaseName);
            }
            conMgrSource.Properties["Format"].SetValue(conMgrSource, "Delimited");
            conMgrSource.Properties["ColumnNamesInFirstDataRow"].SetValue(conMgrSource, IsColumnNamesInFirstDataRow);
            wrap.IDTSConnectionManagerFlatFile100 connectionFlatFile = conMgrSource.InnerObject as wrap.IDTSConnectionManagerFlatFile100;
            connectionFlatFile.Unicode = UniCode;
            // Connect to SQL server and examine metadata of target table, but must exclude 
            // extra Flat File FileNameColumnName (FileName) column as that is added by source
            
            SqlConnection connection = new SqlConnection(sqlConnection);
            
            SqlCommand command = new SqlCommand("SELECT name, xtype, length, scale, prec FROM sys.syscolumns " + "WHERE id = OBJECT_ID(@OBJECT_NAME) AND name <> 'FileName'", connection);
            command.Parameters.Add(new SqlParameter("@OBJECT_NAME", TargetName));
            connection.Open();
            using (SqlDataReader reader = command.ExecuteReader())
            {
                // Create Flat File columns based on SQL columns
                while (reader.Read())
                {
                    // Create Flat File column to match SQL target column
                    wrap.IDTSConnectionManagerFlatFileColumn100 flatFileColumn =
                        connectionFlatFile.Columns.Add() as wrap.IDTSConnectionManagerFlatFileColumn100;
                    SetDtsColumnProperties(flatFileColumn, reader, ColumnDelimeter, AllowFlatFileTruncate);
                    
                }
            }
            // Check we have columns
            if (connectionFlatFile.Columns.Count == 0)
            {
                throw new ArgumentException(string.Format("No flat file columns have been created, " +
                    "check that the destination table '{0}' exists.", TargetName));
            }
            //Correct the last Flat File column delimiter, needs to be NewLine not Comma
            connectionFlatFile.Columns[connectionFlatFile.Columns.Count - 1].ColumnDelimiter = Environment.NewLine;
            

        }
        private void SetDtsColumnProperties(wrap.IDTSConnectionManagerFlatFileColumn100 flatFileColumn,
           SqlDataReader reader, string ColumnDelimeter, bool AllowFlatFileTruncate)
        {
            flatFileColumn.ColumnType = "Delimited";

            
            if (ColumnDelimeter == "\\t" || ColumnDelimeter == "Tab {t}")
                flatFileColumn.ColumnDelimiter = "\t";
            else
            {
                flatFileColumn.ColumnDelimiter = ColumnDelimeter;
            }
            
          
            switch (Convert.ToInt16(reader["xtype"]))
            {
                case 104:  // DT_BOOL  bit
                    flatFileColumn.DataType = wrap.DataType.DT_BOOL;
                    break;

                case 173:   // DT_BYTES binary, varbinary, timestamp

                case 165:
                case 189:
                    flatFileColumn.DataType = wrap.DataType.DT_BYTES;
                    flatFileColumn.ColumnWidth = Convert.ToInt32(reader["length"]);
                    if(!AllowFlatFileTruncate)
                    flatFileColumn.MaximumWidth = Convert.ToInt32(reader["length"]);
                    break;

                case 60:   // DT_CY smallmoney, money
                case 122:
                    flatFileColumn.DataType = wrap.DataType.DT_CY;
                    flatFileColumn.DataPrecision = Convert.ToInt32(reader["prec"]);
                    flatFileColumn.DataScale = (int)reader["scale"];
                    break;

                case 61:   // DT_DBTIMESTAMP datetime, smalldatetime
                case 58:
                    flatFileColumn.DataType = wrap.DataType.DT_DBTIMESTAMP;
                    break;

                case 36:   // DT_GUID uniqueidentifier
                    flatFileColumn.DataType = wrap.DataType.DT_GUID;
                    break;

                case 52:    // DT_I2 smallint
                    flatFileColumn.DataType = wrap.DataType.DT_I2;
                    break;

                case 56:    // DT_I4 int
                    flatFileColumn.DataType = wrap.DataType.DT_I4;
                    break;

                case 127:    // DT_I8 bigint
                    flatFileColumn.DataType = wrap.DataType.DT_I8;
                    break;

                case 106:  // DT_NUMERIC decimal, numeric
                case 108:
                    flatFileColumn.DataType = wrap.DataType.DT_NUMERIC;
                    flatFileColumn.DataPrecision = Convert.ToInt32(reader["prec"]);
                    flatFileColumn.DataScale = (int)reader["scale"];
                    break;

                case 59:    // DT_R4 real
                    flatFileColumn.DataType = wrap.DataType.DT_R4;
                    break;

                case 62:    // DT_R8 float
                    flatFileColumn.DataType = wrap.DataType.DT_R8;
                    break;

                case 175:    // DT_STR char, varchar
                case 167:
                    flatFileColumn.DataType = wrap.DataType.DT_STR;
                    flatFileColumn.ColumnWidth = Convert.ToInt32(reader["length"]);
                    if (!AllowFlatFileTruncate)
                    flatFileColumn.MaximumWidth = Convert.ToInt32(reader["length"]);
                    break;

                case 48:    // DT_UI1 tinyint
                    flatFileColumn.DataType = wrap.DataType.DT_UI1;
                    break;

                case 239:    // DT_WSTR nchar, nvarchar, sql_variant, xml
                case 231:
                case 98:
                case 241:
                    flatFileColumn.DataType = wrap.DataType.DT_WSTR;
                    flatFileColumn.ColumnWidth = Convert.ToInt32(reader["length"]) / 2;
                    if (!AllowFlatFileTruncate)
                    flatFileColumn.MaximumWidth = Convert.ToInt32(reader["length"]) / 2;
                    break;

                case 34:    // DT_IMAGE image
                    flatFileColumn.DataType = wrap.DataType.DT_IMAGE;
                    break;

                case 99:    // DT_NTEXT ntext
                    flatFileColumn.DataType = wrap.DataType.DT_NTEXT;
                    break;

                case 35:    // DT_TEXT text
                    flatFileColumn.DataType = wrap.DataType.DT_TEXT;
                    break;

            }
            wrap.IDTSName100 columnName = flatFileColumn as wrap.IDTSName100;
            columnName.Name = reader["name"].ToString();            
        }
        #region Row Count
        public CManagedComponentWrapper AddRowCount(MainPipe dataFlow, string Name, out IDTSComponentMetaData100 rowCount, string variableName)
        {
            // Add Row Count transform
            rowCount = dataFlow.AddPipeLine(SSISMoniker.ROW_COUNT, Name + " RC");
            CManagedComponentWrapper instance = rowCount.InitializeTask();
            rowCount.Name = Name + "RC";
            // Set the variable name property
            instance.SetComponentProperty("VariableName", variableName);

            return instance;
        }
        #endregion
    }
}
