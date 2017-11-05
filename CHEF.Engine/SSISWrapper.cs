using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using Microsoft.SqlServer.Dts.Runtime;
using wrap = Microsoft.SqlServer.Dts.Runtime.Wrapper;

namespace CHEFEngine
{
    public class Connection
    {
        public string Key { get; set; }

        /// <summary>
        /// At this moment it supports only CSV and OLE DB
        /// </summary>
        public ConnectionTypes ConnectionType { get; set; }

        /// <summary>
        /// Can be a Connection String or a File Path or File Name.  
        /// It must be full path at this moment
        /// </summary>
        public string ConnectionString { get; set; }
    }

    public enum ConnectionTypes
    {
        OleDBConnection,
        FlatFileConnection,
        FileConnection,
        ExcelConnection,
        SMTPConnection,
        TableStorageConnection,
        SharePointListConnection
    }
    
    public enum SourceType
    {
        Folder, Excel, FlatFile, Table, SELECTSQL,TableStorage,SPList
    }

    public enum TargetType
    {
        Excel, FlatFile, Table, TableStorage,SPList
    }
        
    public class SSISMoniker
    {
        public const String SQL_TASK = "STOCK:SQLTask";
        public const String EXECUTE_PACKAGE = "STOCK:ExecutePackageTask";
        public const String PIPE_LINE = "STOCK:PipelineTask";

        public const String OLEDB_SOURCE = "DTSAdapter.OleDbSource";
        public const String OLEDB_DESTINATION = "DTSAdapter.OLEDBDestination";

        public const String FLAT_FILE_DESTINATION = "DTSAdapter.FlatFileDestination";
        public const String FLAT_FILE_SOURCE = "DTSAdapter.FlatFileSource";

        public const String EXCEL_SOURCE = "DTSAdapter.ExcelSource";
        public const String EXCEL_DESTINATION = "DTSAdapter.ExcelDestination";

        public const String ROW_COUNT = "DTSTransform.RowCount";
        public const String SENDMAIL_TASK = "STOCK:SendMailTask";

        public const String LOGPROVIDER_TEXTFILE = "DTS.LogProviderTextFile";

        
        public const String OLEDB_TABLESTORAGE_SOURCE = "CHEFTableStorageSSISSource.CHEFTableStorageSSISSource, CHEFTableStorageSSISSource, Version=2.0.0.0, Culture=neutral, PublicKeyToken=abe491cc7f8a4fba";
        public const String OLEDB_TABLESTORAGE_DESTINATION = "CHEFTableStorageSSISDestination.CHEFTableStorageSSISDestination, CHEFTableStorageSSISDestination, Version=2.0.0.0, Culture=neutral, PublicKeyToken=f4c60d2d1e513b23";

        public const String OLEDB_SPList_SOURCE = "Microsoft.Samples.SqlServer.SSIS.SharePointListAdapters.SharePointListSource, SharePointListAdapters, Version=1.2012.0.0, Culture=neutral, PublicKeyToken=f4b3011e1ece9d47";
        public const String OLEDB_SP_DESTINATION = "Microsoft.Samples.SqlServer.SSIS.SharePointListAdapters.SharePointListDestination, SharePointListAdapters, Version=1.2012.0.0, Culture=neutral, PublicKeyToken=f4b3011e1ece9d47";
        public const String OLEDB_SP_Connection = "Microsoft.Samples.SqlServer.SSIS.SharePointListConnectionManager.CredentialConnectionManagerUI, SharePointListConnectionManager, Version=1.2012.0.0, Culture=neutral,PublicKeyToken=f4b3011e1ece9d47";
    }

}
