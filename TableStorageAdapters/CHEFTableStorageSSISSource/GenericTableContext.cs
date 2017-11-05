using System;
using System.Data.Services.Client;
using System.Linq;
using System.Xml.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace CHEFTableStorageSSISSource
{
	public class GenericTableContext : TableServiceContext
	{
		public GenericTableContext(string baseAddress, StorageCredentials credentials)
			: base(baseAddress, credentials)
		{
			this.IgnoreMissingProperties = true;
			this.ReadingEntity += new EventHandler<ReadingWritingEntityEventArgs>(GenericTableContext_ReadingEntity);
		}

		public GenericEntity GetFirstOrDefault(string tableName)
		{
			return this.CreateQuery<GenericEntity>(tableName).FirstOrDefault();
		}

		private static readonly XNamespace AtomNamespace = "http://www.w3.org/2005/Atom";
		private static readonly XNamespace AstoriaDataNamespace = "http://schemas.microsoft.com/ado/2007/08/dataservices";
		private static readonly XNamespace AstoriaMetadataNamespace = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";

		private void GenericTableContext_ReadingEntity(object sender, ReadingWritingEntityEventArgs e)
		{
			var entity = e.Entity as GenericEntity;
			if (entity != null)
			{
				e.Data
					.Element(AtomNamespace + "content")
					.Element(AstoriaMetadataNamespace + "properties")
					.Elements()
					.Select(p =>
						new
						{
							Name = p.Name.LocalName,
							IsNull = string.Equals("true", p.Attribute(AstoriaMetadataNamespace + "null") == null ? null : p.Attribute(AstoriaMetadataNamespace + "null").Value, StringComparison.OrdinalIgnoreCase),
							TypeName = p.Attribute(AstoriaMetadataNamespace + "type") == null ? null : p.Attribute(AstoriaMetadataNamespace + "type").Value,
							p.Value
						})
					.Select(dp => new Column(dp.Name, dp.TypeName, dp.Value.ToString()))
					.ToList()
					.ForEach(column => entity[column.ColumnName] = column);
			}
		}
	}
}
