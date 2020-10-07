using Examine;
using Novicell.Examine.ElasticSearch.Indexers;

namespace Novicell.Examine.ElasticSearch.Umbraco.Forms
{
    public class UmbracoFormsElasticIndex : ElasticSearchBaseIndex
    {
        public UmbracoFormsElasticIndex(string name, ElasticSearchConfig connectionConfiguration, FieldDefinitionCollection fieldDefinitions = null, string analyzer = null, IValueSetValidator validator = null, bool isUmbraco = false) : base(name, connectionConfiguration, fieldDefinitions, analyzer, validator, isUmbraco)
        {
        }
    }
}