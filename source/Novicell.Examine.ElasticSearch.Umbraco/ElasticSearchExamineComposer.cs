using System.Configuration;
using Nest;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Web.Search;

namespace Novicell.Examine.ElasticSearch.Umbraco
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
                    ElasticSearchConfig.DebugConnectionConfiguration = new ElasticSearchConfig( new ConnectionSettings());
            }

            base.Compose(composition);
            //   composition.RegisterUnique<IUmbracoIndexesCreator, UmbracoIndexesCreator>();
            composition.RegisterUnique<IUmbracoIndexesCreator, ElasticIndexCreator>();
        }
    }
}