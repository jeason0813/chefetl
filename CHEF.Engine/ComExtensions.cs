using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Dts.Runtime;              // Managed Runtime namespace           [Microsoft.SqlServer.ManagedDTS.dll]
using wrap = Microsoft.SqlServer.Dts.Runtime.Wrapper;
using Microsoft.SqlServer.Dts.Tasks.ExecuteSQLTask;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;


namespace CHEFEngine
{
    /// <summary>
    /// Provides extenstion methods to simplify SSIS API Calls
    /// </summary>
    static class SSISComExtensions
    {
        #region Main Pipe
        public static IDTSComponentMetaData100 AddPipeLine(this MainPipe dataFlow, string ClassID, string Name)
        {
            IDTSComponentMetaData100 pipeLine = dataFlow.ComponentMetaDataCollection.New();
            pipeLine.ComponentClassID = ClassID;
            pipeLine.Name = Name;
            return pipeLine;
        }
        public static void ConnectSourceTarget(this MainPipe dataFlow, IDTSComponentMetaData100 source, IDTSComponentMetaData100 target)
        {
            IDTSPath100 path = dataFlow.PathCollection.New();
            path.AttachPathAndPropagateNotifications(source.OutputCollection[0], target.InputCollection[0]);
            
            
        }
        public static void ConnectBadRowsSourceTarget(this MainPipe dataFlow, IDTSComponentMetaData100 source, IDTSComponentMetaData100 target)
        {
            IDTSPath100 path = dataFlow.PathCollection.New();
            path.AttachPathAndPropagateNotifications(source.OutputCollection[1], target.InputCollection[0]);
        }
        #endregion        
        #region IDTSComponentMetaData100
        public static void SetConnection(this IDTSComponentMetaData100 obj, ConnectionManager con)
        {
            if (obj.RuntimeConnectionCollection.Count > 0)
            {
                obj.RuntimeConnectionCollection[0].ConnectionManager = DtsConvert.GetExtendedInterface(con);
                obj.RuntimeConnectionCollection[0].ConnectionManagerID = con.ID;
            }
        }
        public static CManagedComponentWrapper InitializeTask(this IDTSComponentMetaData100 task)
        {
            // Get the design time instance of the component.
            CManagedComponentWrapper InstanceSource = task.Instantiate();

            // Initialize the component
            InstanceSource.ProvideComponentProperties();

            return InstanceSource;
        }
        #endregion

        #region CManagedComponentWrapper
        public static void SetSQLSource(this CManagedComponentWrapper InstanceSource, string SQLStatementSource,bool SQLStatementASVariable)
        {
            if (SQLStatementASVariable)
            {
                
                InstanceSource.SetComponentProperty("AccessMode", 3);
                InstanceSource.SetComponentProperty("SqlCommandVariable", SQLStatementSource);
            }
            else
            {
                InstanceSource.SetComponentProperty("AccessMode", 2);
                InstanceSource.SetComponentProperty("SqlCommand", SQLStatementSource);
            }
        }
        public static void SetTableSource(this CManagedComponentWrapper InstanceSource, string TableName)
        {
            InstanceSource.SetComponentProperty("OpenRowset", TableName);
            InstanceSource.SetComponentProperty("AccessMode", 0);
        }
        public static void SetSharePointListSource(this CManagedComponentWrapper InstanceSource, string SiteListName,string SiteURL)
        {
            InstanceSource.SetComponentProperty("SiteListName", SiteListName);
            InstanceSource.SetComponentProperty("SiteUrl",SiteURL);
            
        }
        public static void SetSharePointListDestination(this CManagedComponentWrapper InstanceSource, string SiteListName, string SiteURL)
        {
            InstanceSource.SetComponentProperty("SiteListName", SiteListName);
            InstanceSource.SetComponentProperty("SiteUrl", SiteURL);
            InstanceSource.SetComponentProperty("UseConnectionManager", 0);

        }
        public static void SetTableStorageSource(this CManagedComponentWrapper InstanceSource, string TableName,string azureTableConnection)
        {

            InstanceSource.SetComponentProperty("StorageConnectionString", azureTableConnection);
            InstanceSource.SetComponentProperty("TableName", TableName);
        }
        public static void SetTableDestination(this CManagedComponentWrapper InstanceDestination, string TableName)
        {
            InstanceDestination.SetComponentProperty("OpenRowset", TableName);
            InstanceDestination.SetComponentProperty("AccessMode", 3);
            InstanceDestination.SetComponentProperty("FastLoadOptions", "TABLOCK");
            //TODO: Can be set using config
            InstanceDestination.SetComponentProperty("FastLoadMaxInsertCommitSize", 10000);

            InstanceDestination.SetComponentProperty("FastLoadKeepNulls", false);
            InstanceDestination.SetComponentProperty("FastLoadKeepIdentity", false);
            InstanceDestination.SetComponentProperty("DefaultCodePage", 1252);
            InstanceDestination.SetComponentProperty("CommandTimeout", 0);
            InstanceDestination.SetComponentProperty("AlwaysUseDefaultCodePage", false);
            InstanceDestination.SetComponentProperty("FastLoadKeepIdentity", false);
        }
        public static void SetTableStorageDestination(this CManagedComponentWrapper InstanceDestination, string TableName, string azureTableConnection)
        {
            string accountName = string.Empty;
            string accountKey = string.Empty;
            string defaultEndpointsProtocol = "http";
            foreach( string str in azureTableConnection.Split(';'))
            {
                if(str.Contains("AccountName"))
                {
                    accountName = str.Substring(str.IndexOf("=")+1); 
                }
                else if(str.Contains("AccountKey"))
                {
                    accountKey = str.Substring(str.IndexOf("=") + 1); 
                }
                else if(str.Contains("DefaultEndpointsProtocol"))
                {
                    defaultEndpointsProtocol = str.Substring(str.IndexOf("=") + 1); 
                }

            }
            InstanceDestination.SetComponentProperty("AccountName",accountName );
            InstanceDestination.SetComponentProperty("AccountKey", accountKey);
            InstanceDestination.SetComponentProperty("DefaultEndpointsProtocol", defaultEndpointsProtocol);
            InstanceDestination.SetComponentProperty("TableName", TableName);
        }
        public static void ConnectAndReinitializeMetaData(this CManagedComponentWrapper InstanceSource,string tableName)
        {
            try
            {
                InstanceSource.AcquireConnections(null);
                InstanceSource.ReinitializeMetaData();
                InstanceSource.ReleaseConnections();
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to connect to given source/target for " + tableName);
            }
        }
        #endregion       
    }    
}
