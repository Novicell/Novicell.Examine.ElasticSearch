using System;
using System.Collections.Generic;
using System.Linq;
using Examine;
using Examine.LuceneEngine.Providers;
using Novicell.Examine.Solr.Indexers;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using Umbraco.Examine;

namespace Novicell.Examine.Solr.Umbraco.Indexers
{
    public class SolRUmbracoIndex : SolrBaseIndex, IUmbracoIndex, IIndexDiagnostics
    {
        private const string SpecialFieldPrefix = "__";

        public const string IndexPathFieldName = SpecialFieldPrefix + "Path";
        public const string NodeKeyFieldName = SpecialFieldPrefix + "Key";
        public const string IconFieldName = SpecialFieldPrefix + "Icon";
        public const string PublishedFieldName = SpecialFieldPrefix + "Published";
        private readonly IProfilingLogger _logger;
        public bool EnableDefaultEventHandler { get; set; } = true;

        /// <summary>
        /// The prefix added to a field when it is duplicated in order to store the original raw value.
        /// </summary>
        public const string RawFieldPrefix = SpecialFieldPrefix + "Raw_";

        /// <summary>
        /// Create a new <see cref="UmbracoExamineIndex"/>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="fieldDefinitions"></param>
        /// <param name="luceneDirectory"></param>
        /// <param name="defaultAnalyzer"></param>
        /// <param name="profilingLogger"></param>
        /// <param name="validator"></param>
        /// <param name="indexValueTypes"></param>
        protected SolRUmbracoIndex(string name,
            SolrConfig connectionConfiguration,
            IProfilingLogger profilingLogger,
            FieldDefinitionCollection fieldDefinitions = null,
            string analyzer = null,
            IValueSetValidator validator = null)
            : base(name, connectionConfiguration, fieldDefinitions, analyzer, validator,true)
        {
            _logger = profilingLogger;
        }

        private readonly bool _configBased = false;

        protected IProfilingLogger ProfilingLogger { get; }

        /// <summary>
        /// When set to true Umbraco will keep the index in sync with Umbraco data automatically
        /// </summary>

        public bool PublishedValuesOnly { get; protected set; } = false;

        /// <inheritdoc />
        public new IEnumerable<string> GetFields()
        {
            //we know this is a LuceneSearcher
            var searcher = (LuceneSearcher) GetSearcher();
            return searcher.GetAllIndexedFields();
        }

        /// <summary>
        /// override to check if we can actually initialize.
        /// </summary>
        /// <remarks>
        /// This check is required since the base examine lib will try to rebuild on startup
        /// </remarks>
        /// <summary>
        /// Returns true if the Umbraco application is in a state that we can initialize the examine indexes
        /// </summary>
        /// <returns></returns>
        protected bool CanInitialize()
        {
            // only affects indexers that are config file based, if an index was created via code then
            // this has no effect, it is assumed the index would not be created if it could not be initialized
            return _configBased == false || Current.RuntimeState.Level == RuntimeLevel.Run;
        }

        /// <summary>
        /// overridden for logging
        /// </summary>
        /// <param name="ex"></param>
        protected override void OnIndexingError(IndexingErrorEventArgs ex)
        {
            ProfilingLogger.Error(GetType(), ex.InnerException, ex.Message);
            base.OnIndexingError(ex);
        }

        protected override void PerformDeleteFromIndex(IEnumerable<string> itemIds,
            Action<IndexOperationEventArgs> onComplete)
        {
            var descriptor = new BulkDescriptor();

            foreach (var id in itemIds.Where(x => !string.IsNullOrWhiteSpace(x)))
                descriptor.Index(indexName).Delete<Document>(x => x
                        .Id(id))
                    .Refresh(Refresh.WaitFor);

            var response = _client.Value.Bulk(descriptor);
            if (response.Errors)
            {
                foreach (var itemWithError in response.ItemsWithErrors)
                {
                    _logger.Error<ElasticSearchBaseIndex>("Failed to remove from index document {NodeID}: {Error}",
                        itemWithError.Id, itemWithError.Error);
                }
            }
        }


        protected override void OnTransformingIndexValues(IndexingItemEventArgs e)
        {
            base.OnTransformingIndexValues(e);

            //ensure special __Path field
            var path = e.ValueSet.GetValue("path");
            if (path != null)
            {
                e.ValueSet.Set(IndexPathFieldName, path);
            }

            //icon
            if (e.ValueSet.Values.TryGetValue("icon", out var icon) &&
                e.ValueSet.Values.ContainsKey(IconFieldName) == false)
            {
                e.ValueSet.Values[IconFieldName] = icon;
            }
        }


        public Attempt<string> IsHealthy()
        {
            var isHealthy = _client.Value.Ping();
            return isHealthy.ApiCall.Success ? Attempt<string>.Succeed():  Attempt.Fail(isHealthy.OriginalException.Message);
        }

        public IReadOnlyDictionary<string, object> Metadata
        {
            get
            {
                var d = new Dictionary<string, object>();
                d[nameof(DocumentCount)] = DocumentCount;
                d[nameof(Name)] = Name;
                d[nameof(indexAlias)] = indexAlias;
                d[nameof(indexName)] = indexName;
                d[nameof(ElasticURL)] = ElasticURL;
                d[nameof(Analyzer)] = Analyzer;
                d[nameof(EnableDefaultEventHandler)] = EnableDefaultEventHandler;
                d[nameof(PublishedValuesOnly)] = PublishedValuesOnly;

                if (ValueSetValidator is ValueSetValidator vsv)
                {
                    d[nameof(ContentValueSetValidator.IncludeItemTypes)] = vsv.IncludeItemTypes;
                    d[nameof(ContentValueSetValidator.ExcludeItemTypes)] = vsv.ExcludeItemTypes;
                    d[nameof(ContentValueSetValidator.IncludeFields)] = vsv.IncludeFields;
                    d[nameof(ContentValueSetValidator.ExcludeFields)] = vsv.ExcludeFields;
                }

                if (ValueSetValidator is ContentValueSetValidator cvsv)
                {
                    d[nameof(ContentValueSetValidator.PublishedValuesOnly)] = cvsv.PublishedValuesOnly;
                    d[nameof(ContentValueSetValidator.SupportProtectedContent)] = cvsv.SupportProtectedContent;
                    d[nameof(ContentValueSetValidator.ParentId)] = cvsv.ParentId;
                }

                d[nameof(FieldDefinitionCollection)] = String.Join(", ",_searcher.Value.AllFields);
                return d.Where(x => x.Value != null).ToDictionary(x => x.Key, x => x.Value);
            }
        }
    }
}