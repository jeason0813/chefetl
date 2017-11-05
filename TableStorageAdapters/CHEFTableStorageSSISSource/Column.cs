using System;
using Microsoft.SqlServer.Dts.Pipeline;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;
using Microsoft.SqlServer.Dts;

namespace CHEFTableStorageSSISSource
{
	public class Column
	{
		public Column(string columnName, string typeName, string valueAsString)
		{
			this.ColumnName = columnName;
			this.ClrType = Column.GetType(typeName);
			this.DtsType = Column.GetSsisType(typeName);
			this.Value = Column.GetValue(this.DtsType, valueAsString);
		}

		public string ColumnName { get; private set; }
		public Type ClrType { get; private set; }
		public DataType DtsType { get; private set; }
		public object Value { get; private set; }

		private static Type GetType(string type)
		{
			switch (type)
			{
				case "String": return typeof(string);
				case "Int32": return typeof(int);
				case "Edm.Int64": return typeof(long);
				case "Edm.Double": return typeof(double);
				case "Edm.Boolean": return typeof(bool);
				case "Edm.DateTime": return typeof(DateTime);
				case "Edm.Binary": return typeof(byte[]);
				case "Edm.Guid": return typeof(Guid);
                default: return typeof(string);
               // default: throw new NotSupportedException(string.Format("Unsupported data 000 type {0}", type));
			}
		}

		private static DataType GetSsisType(string type)
		{
			switch (type)
			{
				case "Edm.String": return DataType.DT_NTEXT;
				case "Edm.Binary": return DataType.DT_IMAGE;
				case "Edm.Int32": return DataType.DT_I4;
				case "Edm.Int64": return DataType.DT_I8;
				case "Edm.Boolean": return DataType.DT_BOOL;
				case "Edm.DateTime": return DataType.DT_DATE;
				case "Edm.Guid": return DataType.DT_GUID;
				case "Edm.Double": return DataType.DT_R8;
                default: return DataType.DT_NTEXT;
				//default: throw new NotSupportedException(string.Format("Unsupported data111 type {0}", type));
			}
		}

		private static object GetValue(DataType dtsType, string valueAsString)
		{
			switch (dtsType)
			{
				case DataType.DT_NTEXT: return valueAsString;
				case DataType.DT_IMAGE: return Convert.FromBase64String(valueAsString);
				case DataType.DT_BOOL: return bool.Parse(valueAsString);
				case DataType.DT_DATE: return DateTime.Parse(valueAsString);
				case DataType.DT_GUID: return new Guid(valueAsString);
				case DataType.DT_I2: return Int32.Parse(valueAsString);
				case DataType.DT_I4: return Int64.Parse(valueAsString);
				case DataType.DT_R8: return double.Parse(valueAsString);
				default: throw new NotSupportedException(string.Format("Unsupported data 222type {0}", dtsType));
			}
		}
	}
}
