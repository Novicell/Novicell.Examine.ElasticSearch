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
            //composition.Register(typeof(ElasticIndexCreator));
           
            composition.Register<Novicell.Examine.ElasticSearch.Populators.ContentIndexPopulator>(Lifetime.Singleton);

            composition.Register<Novicell.Examine.ElasticSearch.Populators.PublishedContentIndexPopulator>(
                Lifetime.Singleton);

            composition.Register<Novicell.Examine.ElasticSearch.Populators.MediaIndexPopulator>(Lifetime.Singleton);
            // the container can inject IEnumerable<IIndexPopulator> and get them all
            composition.Register<Novicell.Examine.ElasticSearch.Populators.MemberIndexPopulator>(Lifetime.Singleton);

            composition.Register<IndexRebuilder>(Lifetime.Singleton);

            //   composition.RegisterUnique<IUmbracoIndexesCreator, UmbracoIndexesCreator>();
            composition.RegisterUnique<IUmbracoIndexesCreator,ElasticIndexCreator>();

            composition.RegisterUnique<IPublishedContentValueSetBuilder>(factory =>
                new ContentValueSetBuilder(
                    factory.GetInstance<PropertyEditorCollection>(),
                    factory.GetInstance<UrlSegmentProviderCollection>(),
                    factory.GetInstance<IUserService>(),
                    true));
            composition.RegisterUnique<IContentValueSetBuilder>(factory =>
                new ContentValueSetBuilder(
                    factory.GetInstance<PropertyEditorCollection>(),
                    factory.GetInstance<UrlSegmentProviderCollection>(),
                    factory.GetInstance<IUserService>(),
                    false));
            composition.RegisterUnique<IValueSetBuilder<IMedia>, MediaValueSetBuilder>();
            composition.RegisterUnique<IValueSetBuilder<IMember>, MemberValueSetBuilder>();


            //We want to manage Examine's AppDomain shutdown sequence ourselves so first we'll disable Examine's default behavior
            //and then we'll use MainDom to control Examine's shutdown - this MUST be done in Compose ie before ExamineManager
            //is instantiated, as the value is used during instantiation
            ExamineManager.DisableDefaultHostingEnvironmentRegistration();
        }
    }
}