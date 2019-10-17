using System.Configuration;
using Examine;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Models;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Services;
using Umbraco.Core.Strings;
using Umbraco.Examine;
using Umbraco.Web.Search;

namespace Novicell.Examine.ElasticSearch
{
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    [ComposeAfter(typeof(ExamineComposer))]
    public class ElasticSearchExamineComposer : ComponentComposer<ElasticSearchExamineComponent>, ICoreComposer
    {
        public override void Compose(Composition composition)
        {
            if (ConfigurationManager.AppSettings["examine:ElasticSearch.Debug"] == "True")
            {
                if (ElasticSearchConfig.DebugConnectionConfiguration == null)
                    ElasticSearchConfig.DebugConnectionConfiguration = new ElasticSearchConfig();
            }

            base.Compose(composition);
            composition.Register<Novicell.Examine.ElasticSearch.Populators.ContentIndexPopulator>(Lifetime.Singleton);

            composition.Register<Novicell.Examine.ElasticSearch.Populators.PublishedContentIndexPopulator>(
                Lifetime.Singleton);
            composition.Register<Novicell.Examine.ElasticSearch.Populators.MediaIndexPopulator>(Lifetime.Singleton);
            // the container can inject IEnumerable<IIndexPopulator> and get them all
            composition.Register<Novicell.Examine.ElasticSearch.Populators.MemberIndexPopulator>(Lifetime.Singleton);
            //   composition.RegisterUnique<IUmbracoIndexesCreator, UmbracoIndexesCreator>();
            composition.RegisterUnique<IUmbracoIndexesCreator, ElasticIndexCreator>();
        }
    }
}