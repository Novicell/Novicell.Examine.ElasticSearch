using Examine;
using Novicell.Examine.ElasticSearch.Indexers;

namespace Novicell.Examine.ElasticSearch.Umbraco.PDF
{
    public class PdfElasticIndex : ElasticSearchBaseIndex
    {
        public PdfElasticIndex(string name, ElasticSearchConfig connectionConfiguration, FieldDefinitionCollection fieldDefinitions = null, string analyzer = null, IValueSetValidator validator = null, bool isUmbraco = false) : base(name,  fieldDefinitions, analyzer, validator, isUmbraco)
        {
        }
    }
}