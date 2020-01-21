using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Examine;
using Novicell.Examine.ElasticSearch.Indexers;
using Novicell.Examine.ElasticSearch.Umbraco.Indexers;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Examine;
using Umbraco.Web.Search;

namespace Novicell.Examine.ElasticSearch.Umbraco
{
    public class ElasticIndexCreator : LuceneIndexCreator, IUmbracoIndexesCreator
    {
        private readonly IPublicAccessService _publicAccessService;
        private string prefix = ConfigurationManager.AppSettings.AllKeys.Any(s => s == "examine:ElasticSearch.Prefix")
            ? ConfigurationManager.AppSettings["examine:ElasticSearch.Prefix"]
            : "";
        
        public ElasticIndexCreator(IProfilingLogger profilingLogger,
            ILocalizationService languageService,
            IPublicAccessService publicAccessService, IUmbracoIndexConfig umbracoIndexConfig)
        {
            ProfilingLogger = profilingLogger ?? throw new System.ArgumentNullException(nameof(profilingLogger));
            LanguageService = languageService ?? throw new System.ArgumentNullException(nameof(languageService));
            UmbracoIndexConfig = umbracoIndexConfig;
            _publicAccessService =
                publicAccessService ?? throw new System.ArgumentNullException(nameof(publicAccessService));
        }

        protected IProfilingLogger ProfilingLogger { get; }
        protected ILocalizationService LanguageService { get; }
        protected IUmbracoIndexConfig UmbracoIndexConfig { get; }

        public override IEnumerable<IIndex> Create()
        {
            return new[]
            {
                CreateInternalIndex(),
                CreateExternalIndex(),
                CreateMemberIndex()
            };
        }

        private IIndex CreateInternalIndex()
        {
            return new ContentElasticSearchIndex(Constants.UmbracoIndexes.InternalIndexName,
                ElasticSearchConfig.GetConfig(Constants.UmbracoIndexes.InternalIndexName),
                ProfilingLogger,
                new UmbracoFieldDefinitionCollection(),
                "whitespace",
                UmbracoIndexConfig.GetContentValueSetValidator());
        }

        private IIndex CreateExternalIndex()
        {
            
            return new ContentElasticSearchIndex(Constants.UmbracoIndexes.ExternalIndexName,
                ElasticSearchConfig.GetConfig(Constants.UmbracoIndexes.ExternalIndexName),
                ProfilingLogger,
                new UmbracoFieldDefinitionCollection(),
                "standard",
                UmbracoIndexConfig.GetPublishedContentValueSetValidator());
        }

        private IIndex CreateMemberIndex()
        {
            return new MemberElasticSearchIndex(Constants.UmbracoIndexes.MembersIndexName,
                ElasticSearchConfig.GetConfig(Constants.UmbracoIndexes.ExternalIndexName),
                ProfilingLogger,
                new UmbracoFieldDefinitionCollection(),
                "standard",
                UmbracoIndexConfig.GetMemberValueSetValidator());
        }
    }
}