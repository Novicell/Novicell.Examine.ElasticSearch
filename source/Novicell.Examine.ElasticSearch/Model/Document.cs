using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Lucene.Net.Documents;

namespace Novicell.Examine.ElasticSearch.Model
{
    public class Document : Dictionary<string, object>
    {
        public object GetField(string FieldName)
        {
            if (this.ContainsKey(FieldName))
            {
                return this[FieldName];
            }

            return null;

        }

        public void Add(Field field)
        {
            if (this.ContainsKey(field.Name))
            {
                this[field.Name] = ByteArrayToObject(field.GetBinaryValue());
            }

        }
        private Object ByteArrayToObject(byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            Object obj = (Object) binForm.Deserialize(memStream);

            return obj;
        }
    }
}