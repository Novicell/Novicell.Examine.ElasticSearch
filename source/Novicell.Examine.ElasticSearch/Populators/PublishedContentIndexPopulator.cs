using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Examine;

namespace Novicell.Examine.ElasticSearch.Populators
{
    public class PublishedContentIndexPopulator : ContentIndexPopulator
    {
        public PublishedContentIndexPopulator(IContentService contentService, ISqlContext sqlContext, IPublishedContentValueSetBuilder contentValueSetBuilder) :
            base(true, null, contentService, sqlContext, contentValueSetBuilder)
        {   
        }
    
    }
}