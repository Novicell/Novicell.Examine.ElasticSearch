using Examine;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;

namespace Novicell.Examine.ElasticSearch
{
    public class MemberElasticSearchIndex : ElasticSearchUmbracoIndex
    {
        public MemberElasticSearchIndex(string name, ElasticSearchConfig connectionConfiguration,
            IProfilingLogger profilingLogger, 
            FieldDefinitionCollection fieldDefinitions = null, string analyzer = null,
            IValueSetValidator validator = null) : base(name, connectionConfiguration,
            profilingLogger, fieldDefinitions, analyzer, validator)
        {
        }
    }
}