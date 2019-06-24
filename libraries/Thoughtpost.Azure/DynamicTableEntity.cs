using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

using Thoughtpost.Azure;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Thoughtpost.Azure
{
    // Modified from http://pascallaurin42.blogspot.com/2013/03/using-azure-table-storage-with-dynamic.html
    public class DynamicTableEntity : DynamicObject, ITableEntity
    {

        public DynamicTableEntity()
        {
            this.Properties = new Dictionary<string, EntityProperty>();
        }

        public DynamicTableEntity(dynamic obj)
        {
            this.Properties = new Dictionary<string, EntityProperty>();

            Dictionary<string, object> dobj = obj as Dictionary<string, object>;

            Initialize(dobj);
        }
        public DynamicTableEntity(Dictionary<string, object> dobj)
        {
            this.Properties = new Dictionary<string, EntityProperty>();

            Initialize(dobj);
        }

        public void Initialize(Dictionary<string, object> dobj)
        {
            foreach (string key in dobj.Keys)
            {
                this[key] = dobj[key];
            }
        }

        public IDictionary<string, EntityProperty> Properties { get; private set; }

        public object this[string key]
        {
            get
            {
                if (!this.Properties.ContainsKey(key))
                    this.Properties.Add(key, this.GetEntityProperty(key, null));

                return this.Properties[key];
            }
            set
            {
                var property = this.GetEntityProperty(key, value);

                if (this.Properties.ContainsKey(key))
                    this.Properties[key] = property;
                else
                    this.Properties.Add(key, property);
            }
        }

        #region DynamicObject overrides

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = this[binder.Name];
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            this[binder.Name] = value;
            return true;
        }

        #endregion

        #region ITableEntity implementation

        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public DateTimeOffset Timestamp { get; set; }

        public string ETag { get; set; }

        public void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            this.Properties = properties;
        }

        public IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
        {
            return this.Properties;
        }

        #endregion

        private EntityProperty GetEntityProperty(string key, object val)
        {
            if (val == null) return new EntityProperty((string)null);

            Type type = val.GetType();

            if (type == typeof(byte[])) return new EntityProperty((byte[])val);
            if (type == typeof(bool)) return new EntityProperty((bool)val);
            if (type == typeof(DateTimeOffset)) return new EntityProperty((DateTimeOffset)val);
            if (type == typeof(DateTime)) return new EntityProperty((DateTime)val);
            if (type == typeof(double)) return new EntityProperty((double)val);
            if (type == typeof(Guid)) return new EntityProperty((Guid)val);
            if (type == typeof(int)) return new EntityProperty((int)val);
            if (type == typeof(long)) return new EntityProperty((long)val);
            if (type == typeof(string)) return new EntityProperty((string)val);

            return new EntityProperty((string)val);
        }

        private Type GetType(EdmType edmType)
        {
            switch (edmType)
            {
                case EdmType.Binary: return typeof(byte[]);
                case EdmType.Boolean: return typeof(bool);
                case EdmType.DateTime: return typeof(DateTime);
                case EdmType.Double: return typeof(double);
                case EdmType.Guid: return typeof(Guid);
                case EdmType.Int32: return typeof(int);
                case EdmType.Int64: return typeof(long);
                case EdmType.String: return typeof(string);
                default: return typeof(string);
            }
        }

        private object GetValue(EntityProperty property)
        {
            switch (property.PropertyType)
            {
                case EdmType.Binary: return property.BinaryValue;
                case EdmType.Boolean: return property.BooleanValue;
                case EdmType.DateTime: return property.DateTimeOffsetValue;
                case EdmType.Double: return property.DoubleValue;
                case EdmType.Guid: return property.GuidValue;
                case EdmType.Int32: return property.Int32Value;
                case EdmType.Int64: return property.Int64Value;
                case EdmType.String: return property.StringValue;
                default: return typeof(string);
            }
        }
    }
}
