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
                return new Field(FieldName,ToByteArray(this[FieldName]), Lucene.Net.Documents.Field.Store.YES);
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
        private byte[] ToByteArray<T>(T obj)
        {
            if(obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            using(MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }
    }
}