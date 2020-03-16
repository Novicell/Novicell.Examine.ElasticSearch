using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Examine;
using Novicell.Examine.Solr.Umbraco.Indexers;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Examine;
using Umbraco.Web.Search;

namespace Novicell.Examine.Solr.Umbraco
{
    public class SolRIndexCreator : LuceneIndexCreator, IUmbracoIndexesCreator
    {
        private readonly IPublicAccessService _publicAccessService;
        private string prefix = ConfigurationManager.AppSettings.AllKeys.Any(s => s == "examine:ElasticSearch.Prefix")
            ? ConfigurationManager.AppSettings["examine:ElasticSearch.Prefix"]
            : "";
        
        public SolRIndexCreator(IProfilingLogger profilingLogger,
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
            return new ContentSolRIndex(Constants.UmbracoIndexes.InternalIndexName,
                SolrConfig.GetConfig(Constants.UmbracoIndexes.InternalIndexName),
                ProfilingLogger,
                new UmbracoFieldDefinitionCollection(),
                "whitespace",
                UmbracoIndexConfig.GetContentValueSetValidator());
        }

        private IIndex CreateExternalIndex()
        {
            
            return new ContentSolRIndex(Constants.UmbracoIndexes.ExternalIndexName,
                SolrConfig.GetConfig(Constants.UmbracoIndexes.ExternalIndexName),
                ProfilingLogger,
                new UmbracoFieldDefinitionCollection(),
                "standard",
                UmbracoIndexConfig.GetPublishedContentValueSetValidator());
        }

        private IIndex CreateMemberIndex()
        {
            return new MemberSolRIndex(Constants.UmbracoIndexes.MembersIndexName,
                SolrConfig.GetConfig(Constants.UmbracoIndexes.ExternalIndexName),
                ProfilingLogger,
                new UmbracoFieldDefinitionCollection(),
                "standard",
                UmbracoIndexConfig.GetMemberValueSetValidator());
        }
    }
}