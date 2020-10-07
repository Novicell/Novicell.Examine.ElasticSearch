using System.Collections.Generic;
using System.Linq;
using Examine;
using Umbraco.Forms.Core.Data.RecordIndex;

namespace Novicell.Examine.ElasticSearch.Umbraco.Forms
{
    public class UmbracoFormsElasticIndexCreator : IFormsIndexCreator
    {
        public IEnumerable<IIndex> Create() => (IEnumerable<IIndex>) new IIndex[1]
        {
            this.CreateElasticRecordIndex()
        };

        private IIndex CreateElasticRecordIndex() => (IIndex) new UmbracoFormsElasticIndex("UmbracoFormsRecordsIndex",
            ElasticSearchConfig.GetConfig("FormsConfig"),
            new FieldDefinitionCollection(Enumerable.Empty<FieldDefinition>()), "simple");

    }
}