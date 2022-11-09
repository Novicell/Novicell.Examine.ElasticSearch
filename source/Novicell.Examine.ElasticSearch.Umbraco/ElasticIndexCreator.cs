using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web.Helpers;
using Examine;
using Newtonsoft.Json;
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
        private readonly ILogger _logger;
        private readonly IPublicAccessService _publicAccessService;
        private string prefix = ConfigurationManager.AppSettings.AllKeys.Any(s => s == "examine:ElasticSearch.Prefix")
            ? ConfigurationManager.AppSettings["examine:ElasticSearch.Prefix"]
            : "";
        
        public ElasticIndexCreator(IProfilingLogger profilingLogger,
            ILocalizationService languageService,
            ILogger logger,
            IPublicAccessService publicAccessService, IUmbracoIndexConfig umbracoIndexConfig)
        {
            ProfilingLogger = profilingLogger ?? throw new System.ArgumentNullException(nameof(profilingLogger));
            LanguageService = languageService ?? throw new System.ArgumentNullException(nameof(languageService));
            UmbracoIndexConfig = umbracoIndexConfig;
            _logger = logger;
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
            var config = ElasticSearchConfig.GetConfig(Constants.UmbracoIndexes.InternalIndexName);
            _logger.Info<ElasticIndexCreator>($"config {JsonConvert.SerializeObject(config)}");
            return new ContentElasticSearchIndex(Constants.UmbracoIndexes.InternalIndexName,
                config  ,
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
                UmbracoIndexConfig.GetPublishedContentValueSetValidator(), true);
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