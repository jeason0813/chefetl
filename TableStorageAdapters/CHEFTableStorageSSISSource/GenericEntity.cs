using System.Collections.Generic;
using Microsoft.WindowsAzure.StorageClient;

namespace CHEFTableStorageSSISSource
{
    public class GenericEntity : TableServiceEntity
    {
		private Dictionary<string, Column> properties = new Dictionary<string, Column>();

        public Column this[string key]
        {
            get
            {
				if (this.properties.ContainsKey(key))
				{
					return this.properties[key];
				}
				else
				{
					return null;
				}
            }

            set
            {
                this.properties[key] = value;
            }
        }

		public IEnumerable<Column> GetProperties()
		{
			return this.properties.Values;
		}

		public void SetProperties(IEnumerable<Column> properties)
		{
			foreach (var property in properties)
			{
				this[property.ColumnName] = property;
			}
		}
    }   

    
}
