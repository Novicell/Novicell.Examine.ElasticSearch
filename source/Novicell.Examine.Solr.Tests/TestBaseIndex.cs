using System.Collections.Generic;
using Examine;
using Novicell.Examine.Solr.Indexers;

namespace Novicell.Examine.Solr.Tests
{
    public class TestBaseIndex : SolrBaseIndex
    {
        public TestBaseIndex(SolrConfig config,
            FieldDefinitionCollection fieldDefinitions = null,
            string analyzer = null,
            IValueSetValidator validator = null)
            : base("testIndexer", config, fieldDefinitions, analyzer, validator)
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