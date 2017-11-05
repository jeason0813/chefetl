using System.Collections.Generic;
using Microsoft.SqlServer.Dts.Pipeline;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Pipeline.Localization;
using System;
using System.Data;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using Microsoft.SqlServer.Dts;
using System.Globalization;
using System.Reflection;
using System.CodeDom.Compiler;
using Microsoft.CSharp;

using System.Text;
using Microsoft.SqlServer.Dts.Runtime;

namespace CHEFTableStorageSSISDestination
{
    #if DENALI
    [DtsPipelineComponent
        (DisplayName = "CHEFTableStorageSSISDestination",
         Description="CHEF Table Storage SSIS Destination",
         ComponentType = ComponentType.SourceAdapter,
         UITypeName="CHEFTableStorageSSISDestination.CHEFTableStorageSSISDestination, CHEFTableStorageSSISDestination, Version=2.0.0.0, Culture=neutral, PublicKeyToken=f4c60d2d1e513b23",
         HelpKeyword="CHEFTableStorageSSISDestination;http://toolbox/chef"
         )]
    #else
      [DtsPipelineComponent
        (DisplayName = "CHEFTableStorageSSISDestination",
         Description="CHEF Table Storage SSIS Destination",
         ComponentType = ComponentType.SourceAdapter,
         UITypeName="CHEFTableStorageSSISDestination.CHEFTableStorageSSISDestination, CHEFTableStorageSSISDestination, Version=1.0.0.0, Culture=neutral, PublicKeyToken=f4c60d2d1e513b23",
         HelpKeyword="CHEFTableStorageSSISDestination;http://toolbox/chef"
         )]
    #endif


    public class CHEFTableStorageSSISDestination : PipelineComponent
	{

        #if DENALI
                const string multiLineUI = "Microsoft.DataTransformationServices.Controls.ModalMultilineStringEditor, Microsoft.DataTransformationServices.Controls, Version=11.0.00.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
        #else
            const string multiLineUI = "Microsoft.DataTransformationServices.Controls.ModalMultilineStringEditor, Microsoft.DataTransformationServices.Controls, Version=10.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
        #endif

        // Create Match Expression parameter

        private DataTable m_table = null; 
        private DataColumn[] m_tableCols = null;
        private int[] m_bufferIdxs = null;
        public override void ProvideComponentProperties()
        {
            ComponentMetaData.RuntimeConnectionCollection.RemoveAll();
            RemoveAllInputsOutputsAndCustomProperties();
            
            //Account Name
            var storageAccountNameStringProperty = this.ComponentMetaData.CustomPropertyCollection.New();
            storageAccountNameStringProperty.Name = "AccountName";
            storageAccountNameStringProperty.Description = "Azure storage account name";
            storageAccountNameStringProperty.Value = string.Empty;

            //Account Key
            var storageAccountKeyStringProperty = this.ComponentMetaData.CustomPropertyCollection.New();
            storageAccountKeyStringProperty.Name = "AccountKey";
            storageAccountKeyStringProperty.Description = "Azure storage account key";
            storageAccountKeyStringProperty.Value = string.Empty;

            //Default Endpoints Protocol
            var storageDefaultEndpointsProtocolStringProperty = this.ComponentMetaData.CustomPropertyCollection.New();
            storageDefaultEndpointsProtocolStringProperty.Name = "DefaultEndpointsProtocol";
            storageDefaultEndpointsProtocolStringProperty.Description = "Default Endpoints Protocol";
            storageDefaultEndpointsProtocolStringProperty.Value = "http";

            //Table Name
            var tableNameProperty = this.ComponentMetaData.CustomPropertyCollection.New();
            tableNameProperty.Name = "TableName";
            tableNameProperty.Description = "Name of the source azure storage table ";
            tableNameProperty.Value =string.Empty;

            //Inputs
            IDTSInput100 input = ComponentMetaData.InputCollection.New();
            input.Name = "Component Input";
            input.Description = "This is what we see from the upstream component";
            input.HasSideEffects = true; 
        }
        public override void ReinitializeMetaData()
        {
            IDTSInput100 _profinput = ComponentMetaData.InputCollection["Component Input"];
            _profinput.InputColumnCollection.RemoveAll();
            IDTSVirtualInput100 vInput = _profinput.GetVirtualInput();
            foreach (IDTSVirtualInputColumn100 vCol in vInput.VirtualInputColumnCollection)
            {
                this.SetUsageType(_profinput.ID, vInput, vCol.LineageID, DTSUsageType.UT_READONLY);
            }
        }
        public override void OnInputPathAttached(int inputID)
        {
            IDTSInput100 input = ComponentMetaData.InputCollection.GetObjectByID(inputID);
            IDTSVirtualInput100 vInput = input.GetVirtualInput();
            foreach (IDTSVirtualInputColumn100 vCol in vInput.VirtualInputColumnCollection)
            {
                this.SetUsageType(inputID, vInput, vCol.LineageID, DTSUsageType.UT_READONLY);
            }
        }
        public override IDTSCustomProperty100 SetComponentProperty(string propertyName, object propertyValue)
        {
            var resultingColumn = base.SetComponentProperty(propertyName, propertyValue);
            return resultingColumn;
        }
        public override void PreExecute()
        {
            createDataTable();   
        }
        public override void ProcessInput(int inputID, PipelineBuffer buffer)
        {
            try
            {
                string strAccountName = (string)this.ComponentMetaData.CustomPropertyCollection["AccountName"].Value;
                string strAccountKey = (string)this.ComponentMetaData.CustomPropertyCollection["AccountKey"].Value;
                string strDefaultEndpointsProtocol = (string)this.ComponentMetaData.CustomPropertyCollection["DefaultEndpointsProtocol"].Value;
                var tableName = (string)this.ComponentMetaData.CustomPropertyCollection["TableName"].Value;
                string strEndProxy = string.Empty;
                strEndProxy = string.Format("{0}://{1}.table.core.windows.net", strDefaultEndpointsProtocol, strAccountName);
                int cCols = m_table.Columns.Count;
                while (buffer.NextRow())
                {
                    DataRow newRow = m_table.NewRow();
                    for (int iCol = 0; iCol < cCols; iCol++)
                    {
                        newRow[m_tableCols[iCol]] = GetBufferDataAtCol(buffer, iCol);
                    }
                    m_table.Rows.Add(newRow);
                }
                if (m_table != null && m_table.Rows.Count > 0)
                {
                    GenerateXMLRowByRow(m_table, strAccountName, strAccountKey, strEndProxy, tableName);
                }
                m_table.Dispose();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
      
        private object GetBufferDataAtCol(PipelineBuffer buffer, int iCol)
        {
            object colData;
            int idxCol = m_bufferIdxs[iCol];
            if (buffer.IsNull(idxCol))
            {
                colData = DBNull.Value;
            }
            else
            {
                colData = buffer[idxCol];
                // specially treat BLOB columns
                BlobColumn blob = colData as BlobColumn;
                if (blob != null)
                {
                    DataType dataType = blob.ColumnInfo.DataType;
                    if (dataType == DataType.DT_TEXT
                        || dataType == DataType.DT_NTEXT)
                        colData = buffer.GetString(idxCol);
                    else if (dataType == DataType.DT_IMAGE)
                        colData = blob.GetBlobData(0, (int)blob.Length);
                }

            }
            return colData;
        }
        public override void PostExecute()
        {

            base.PostExecute();
            m_table.Dispose();
            m_bufferIdxs = null;
            m_tableCols = null;

        }
        private void createDataTable()
        {
            
            m_table = new DataTable();
            m_table.Locale = CultureInfo.InvariantCulture;
            IDTSInput100 iDTSInput = ComponentMetaData.InputCollection[0];
            IDTSExternalMetadataColumnCollection100 iDTSExtCols =
                iDTSInput.ExternalMetadataColumnCollection;
            IDTSInputColumnCollection100 iDTSInpCols =
                iDTSInput.InputColumnCollection;
            int cInpCols = iDTSInpCols.Count;
            m_tableCols = new DataColumn[cInpCols];
            IDTSExternalMetadataColumn100[] mappedExtCols =
                new IDTSExternalMetadataColumn100[cInpCols];
            m_bufferIdxs = new int[cInpCols];
            for (int iCol = 0; iCol < cInpCols; iCol++)
            {
                IDTSInputColumn100 iDTSInpCol = iDTSInpCols[iCol];
                int metaID = iDTSInpCol.MappedColumnID;
                DataType dataType = iDTSInpCol.DataType;
                Type type;
                bool isLong = false;
                dataType = ConvertBufferDataTypeToFitManaged(dataType,
                        ref isLong);
                type = BufferTypeToDataRecordType(dataType);
                m_tableCols[iCol] = new DataColumn(iDTSInpCol.Name, type);
                int lineageID = iDTSInpCol.LineageID;
                try
                {
                    m_bufferIdxs[iCol] = BufferManager.FindColumnByLineageID(
                        iDTSInput.Buffer, lineageID);
                }
                catch (Exception)
                {
                    bool bCancel;
                    //ErrorSupport.FireErrorWithArgs(HResults.DTS_E_ADODESTNOLINEAGEID,
                    //    out bCancel, lineageID, iDTSInpCol.Name);
                    //throw new PipelineComponentHResultException(HResults.
                    //    DTS_E_ADODESTNOLINEAGEID);
                }

            }

            m_table.Columns.AddRange(m_tableCols);
        }
        public override IDTSInputColumn100 SetUsageType(int inputID, IDTSVirtualInput100 virtualInput, int lineageID, DTSUsageType usageType)
        {
            IDTSInputColumn100 inp = base.SetUsageType(inputID, virtualInput, lineageID, usageType);
            if (inp != null)
            {
                if (inp.UsageType == DTSUsageType.UT_READWRITE)
                {
                    throw new Exception("You cannot set a column to read write for this destination");
                }
            }
            return inp;
        }
        public static void GenerateXMLRowByRow(DataTable EavTableObject, string AccountName, string AccountKey, string EndPoint, string TableName)
        {
            StringBuilder OneRowXML = new StringBuilder();
            string RowXML = String.Empty;
            int ColumnCount = EavTableObject.Columns.Count;
            int RowCount = EavTableObject.Rows.Count;
            for (int i = 0; i < RowCount; i++)
            {
                DataRow row = EavTableObject.NewRow();

                row = EavTableObject.Rows[i];

                OneRowXML.Remove(0, OneRowXML.Length);
                RowXML.Remove(0, RowXML.Length);

                for (int j = 0; j < ColumnCount; j++)
                {
                    if (j == 0 || j == 1)
                        OneRowXML.Append(string.Format(EntityPropertyTemplateRowKeyPartitionKey, EavTableObject.Columns[j].ColumnName.ToString(), XMLSafeString(row[j].ToString())));
                    else
                    {
                        if (!string.IsNullOrEmpty(row[j].ToString()))
                            OneRowXML.Append(string.Format(EntityPropertyTemplate, EavTableObject.Columns[j].ColumnName.ToString(), EavTableObject.Columns[j].DataType.Name, FormatPropertyValue(EavTableObject.Columns[j].DataType.Name, row[j].ToString())));
                        else
                            OneRowXML.Append(string.Format(EntityPropertyTemplateNull, EavTableObject.Columns[j].ColumnName.ToString(), EavTableObject.Columns[j].DataType.Name));
                    }
                }
                RowXML = String.Format(EntityXmlTemplate, DateTime.UtcNow, OneRowXML.ToString());
                try
                {
                    TableStorage.UploadData(RowXML, DateTime.UtcNow, AccountName, AccountKey, EndPoint, TableName);
                    
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("error: (409) Conflict")) throw ex;
                  
                }
            }
        }

        private static string FormatPropertyValue(string PropertyType, string PropertyValue)
        {
            string ReturnPropertyValue = PropertyValue;
            try
            {
                switch (PropertyType)
                {
                    case "Boolean":
                        ReturnPropertyValue = Convert.ToBoolean(PropertyValue).ToString();
                        break;
                    case "DateTime":
                        ReturnPropertyValue = Convert.ToDateTime(PropertyValue).ToString("yyyy-MM-ddTHH:mm:ss");
                        break;
                    case "Int32":
                        ReturnPropertyValue = Convert.ToInt32(PropertyValue).ToString();
                        break;
                    case "Int64":
                        ReturnPropertyValue = Convert.ToInt64(PropertyValue).ToString();
                        break;
                    case "Double":
                        ReturnPropertyValue = Convert.ToDouble(PropertyValue).ToString();
                        break;
                    case "String":
                        ReturnPropertyValue = XMLSafeString(PropertyValue);
                        break;
                    default:
                        break;
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return ReturnPropertyValue;
        }

        private static string XMLSafeString(string inputString)
        {
            inputString = inputString.Replace("&", "&amp;");
            inputString = inputString.Replace("<", "&lt;");
            inputString = inputString.Replace(">", "&gt;");
            inputString = inputString.Replace("\"", "&quot;");
            inputString = inputString.Replace("'", "&apos;");
            return inputString;
        }

        private static string EntityXmlTemplate = @"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
        <entry 
                xmlns:d=""http://schemas.microsoft.com/ado/2007/08/dataservices"" 
                xmlns:m=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"" 
                xmlns=""http://www.w3.org/2005/Atom"">
            <title />
            <updated>{0:yyyy-MM-ddTHH:mm:ss.fffffffZ}</updated>
            <author>
                <name />
            </author>
            <id />
            <content type=""application/xml"">
              <m:properties>
                 {1}
              </m:properties>
            </content>
        </entry>";

        private static string EntityPropertyTemplate =
            @"<d:{0} m:type=""Edm.{1}"">{2}</d:{0}>";

        private static string EntityPropertyTemplateRowKeyPartitionKey =
            @"<d:{0}>{1}</d:{0}>";

        private static string EntityPropertyTemplateNull =
            @"<d:{0} m:type=""Edm.{1}"" m:null=""true""/>";



    }

}
