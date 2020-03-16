using Examine;
using Umbraco.Core.Logging;
using Umbraco.Examine;

namespace Novicell.Examine.Solr.Umbraco.Indexers
{
    public class MemberSolRIndex : SolRUmbracoIndex, IUmbracoMemberIndex
    {
        public MemberSolRIndex(string name, SolrConfig connectionConfiguration,
            IProfilingLogger profilingLogger, 
            FieldDefinitionCollection fieldDefinitions = null, string analyzer = null,
            IValueSetValidator validator = null) : base(name, connectionConfiguration,
            profilingLogger, fieldDefinitions, analyzer, validator)
        {
        }
    }
}