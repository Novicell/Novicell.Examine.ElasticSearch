using System.Collections.Generic;
using System.IO;
using Examine;
using Examine.LuceneEngine;
using Novicell.Examine.ElasticSearch.Indexers;
using Umbraco.Core.Logging;

namespace Novicell.Examine.ElasticSearch.Tests
{
    public class TestBaseIndex : ElasticSearchBaseIndex
    {
        public TestBaseIndex(ElasticSearchConfig connectionConfiguration,
            FieldDefinitionCollection fieldDefinitions = null,
            string analyzer = null,
            IValueSetValidator validator = null)
            : base("testIndexer", connectionConfiguration, fieldDefinitions, analyzer, validator)
        {
        }


        public IEnumerable<ValueSet> AllData()
        {
            var data = new List<ValueSet>();
            for (int i = 0; i < 100; i++)
            {
                data.Add(ValueSet.FromObject(i.ToString(), "category" + (i % 2), new { item1 = "value" + i, item2 = "value" + i }));
            }
            return data;
        }
    }
}