using Novicell.Examine.ElasticSearch.Umbraco.Forms.IndexPopulators;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Examine;
using Umbraco.Forms.Core.Components;
using Umbraco.Forms.Core.Data.RecordIndex;

namespace Novicell.Examine.ElasticSearch.Umbraco.Forms
{
    [ComposeAfter(typeof(UmbracoFormsComposer))]
    public class ElasticSearchExamineUmbracoFormsComposer : IUserComposer
    {
            
        public void Compose(Composition composition)
        {
            composition.Register<IFormsIndexCreator, UmbracoFormsElasticIndexCreator>(Lifetime.Singleton);
            composition.Register<IIndexPopulator, ElasticFormsIndexPopulator>(Lifetime.Singleton);
            composition.Register<IFormsIndexPopulator, ElasticFormsIndexPopulator>(Lifetime.Singleton);
        }
    }
}