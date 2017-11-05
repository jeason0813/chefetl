using System.Collections.Generic;

using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using System.Data.Services.Client;
using System.Linq;
using Microsoft.SqlServer.Dts.Pipeline;
using Microsoft.SqlServer.Dts.Pipeline.Localization;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using Microsoft.SqlServer.Dts;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;

namespace CHEFTableStorageSSISSource
{
    [DtsPipelineComponent(DisplayName = "CHEFTableStorageSSISSource", ComponentType = ComponentType.SourceAdapter)]
    public class CHEFTableStorageSSISSource : PipelineComponent
	{
		public override void ProvideComponentProperties()
		{
			// Reset the component.
			base.RemoveAllInputsOutputsAndCustomProperties();
			ComponentMetaData.RuntimeConnectionCollection.RemoveAll();

			// Add output
			IDTSOutput100 output = ComponentMetaData.OutputCollection.New();
			output.Name = "Output";

			// Properties
			var storageConnectionStringProperty = this.ComponentMetaData.CustomPropertyCollection.New();
			storageConnectionStringProperty.Name = "StorageConnectionString";
			storageConnectionStringProperty.Description = "Azure storage connection string";
			storageConnectionStringProperty.Value =string.Empty;

			var tableNameProperty = this.ComponentMetaData.CustomPropertyCollection.New();
			tableNameProperty.Name = "TableName";
			tableNameProperty.Description = "Name of the source table";
            tableNameProperty.Value = string.Empty;
		}

		public override IDTSCustomProperty100 SetComponentProperty(string propertyName, object propertyValue)
		{
			var resultingColumn = base.SetComponentProperty(propertyName, propertyValue);

			var storageConnectionString = (string)this.ComponentMetaData.CustomPropertyCollection["StorageConnectionString"].Value;
			var  strSource= (string)this.ComponentMetaData.CustomPropertyCollection["TableName"].Value;

            string tableName = string.Empty;
            string srcPartitionKey = string.Empty;
            if (strSource != string.Empty)
            {
                string[] str1 = strSource.Split(';');
                tableName = str1[0].Split('=')[1];
                srcPartitionKey = str1[1].Split('=')[1];
            }

			if (!string.IsNullOrEmpty(storageConnectionString) && !string.IsNullOrEmpty(tableName))
			{
                
				var cloudStorageAccount = CloudStorageAccount.Parse(storageConnectionString);
				var context = new GenericTableContext(cloudStorageAccount.TableEndpoint.AbsoluteUri, cloudStorageAccount.Credentials);
                
				var firstRow = context.GetFirstOrDefault(tableName);
				if (firstRow != null)
				{
					var output = this.ComponentMetaData.OutputCollection[0];
					foreach (var column in firstRow.GetProperties())
					{
						var newOutputCol = output.OutputColumnCollection.New();
						newOutputCol.Name = column.ColumnName;
						newOutputCol.SetDataTypeProperties(column.DtsType, 0, 0, 0, 0);
					}
				}
			}

          
			return resultingColumn;
		}

		private List<ColumnInfo> columnInformation;
		private GenericTableContext context;
		private struct ColumnInfo
		{
			public int BufferColumnIndex;
			public string ColumnName;
		}

		public override void PreExecute()
		{
			this.columnInformation = new List<ColumnInfo>();
			IDTSOutput100 output = ComponentMetaData.OutputCollection[0];

			var cloudStorageAccount = CloudStorageAccount.Parse((string)this.ComponentMetaData.CustomPropertyCollection["StorageConnectionString"].Value);
			context = new GenericTableContext(cloudStorageAccount.TableEndpoint.AbsoluteUri, cloudStorageAccount.Credentials);
            
			foreach (IDTSOutputColumn100 col in output.OutputColumnCollection)
			{
				ColumnInfo ci = new ColumnInfo();
				ci.BufferColumnIndex = BufferManager.FindColumnByLineageID(output.Buffer, col.LineageID);
				ci.ColumnName = col.Name;
				columnInformation.Add(ci);
			}
		}
      //  "Table=abc;PartitionKey=201001"
        class ListRowsContinuationToken
        {
            public string PartitionKey { get; set; }
            public string RowKey { get; set; }
        }

		public override void PrimeOutput(int outputs, int[] outputIDs, PipelineBuffer[] buffers)
		{
			IDTSOutput100 output = ComponentMetaData.OutputCollection[0];
			PipelineBuffer buffer = buffers[0];
             // A ListRowsContinuationToken class encapsulates the partition and row key 
            ListRowsContinuationToken continuationToken = null;
            string strSource = (string)this.ComponentMetaData.CustomPropertyCollection["TableName"].Value;
            string srcTable = string.Empty;
            string srcPartitionKey = string.Empty;
            if (strSource != string.Empty)
            {
                string[] str1 = strSource.Split(';');
                srcTable = str1[0].Split('=')[1];
                srcPartitionKey = str1[1].Split('=')[1];
            }
           
            do
            {
                var allItems = this.context.CreateQuery<GenericEntity>(srcTable).Where(item => item.PartitionKey==srcPartitionKey);
                var query = allItems as DataServiceQuery<GenericEntity>;
                if (continuationToken != null)
                {
                    query = query.AddQueryOption("NextPartitionKey", continuationToken.PartitionKey);
                    if (continuationToken.RowKey != null)
                    {
                        query = query.AddQueryOption("NextRowKey", continuationToken.RowKey);
                    }

                }
                var response = query.Execute() as QueryOperationResponse;
                if (response.Headers.ContainsKey("x-ms-continuation-NextPartitionKey"))
                {
                    continuationToken = new ListRowsContinuationToken();
                    continuationToken.PartitionKey = response.Headers["x-ms-continuation-NextPartitionKey"];
                    if (response.Headers.ContainsKey("x-ms-continuation-NextRowKey"))
                    {
                        continuationToken.RowKey = response.Headers["x-ms-continuation-NextRowKey"];
                    }
                  
                }
                else
                {
                    continuationToken = null;
                }
                foreach (var item in allItems)
                {
                    buffer.AddRow();

                    for (int x = 0; x < columnInformation.Count; x++)
                    {
                        var ci = (ColumnInfo)columnInformation[x];
                        var value = item[ci.ColumnName].Value;
                        if (value != null)
                        {
                            buffer[ci.BufferColumnIndex] = value;
                        }
                        else
                        {
                            buffer.SetNull(ci.BufferColumnIndex);
                        }
                    }
                }
                   
            }
            while (continuationToken != null);
			buffer.SetEndOfRowset();
		}
	}
}
