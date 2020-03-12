using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Lucene.Net.Documents;

namespace Novicell.Examine.Solr.Model
{
    public class Document : Dictionary<string, object>
    {
        public Field GetField(string fieldName)
        {
            if (ContainsKey(fieldName))
            {
                return new Field(fieldName,Convert.ToString(this[fieldName]), Field.Store.YES, Field.Index.ANALYZED);
            }

            return null;

        }
        
        public IList<IFieldable> GetFields()
        {
            var results = new List<IFieldable>();

            foreach(var f in this)
            {
                results.Add(new Field(f.Key, Convert.ToString(f.Value), Field.Store.YES, Field.Index.ANALYZED));
            }

            return results;
        }

        public void Add(Field field)
        {            
               this[field.Name] = field.StringValue;
        }
        private Object ByteArrayToObject(byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            Object obj = binForm.Deserialize(memStream);

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