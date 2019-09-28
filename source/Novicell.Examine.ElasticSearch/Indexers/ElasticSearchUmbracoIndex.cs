using System;
using System.Collections.Generic;
using System.Linq;
using Elasticsearch.Net;
using Examine;
using Examine.LuceneEngine.Providers;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Nest;
using Novicell.Examine.ElasticSearch.Model;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Logging;
using Umbraco.Examine;

namespace Novicell.Examine.ElasticSearch
{
    public class ElasticSearchUmbracoIndex : ElasticSearchBaseIndex, IUmbracoIndex
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
        protected ElasticSearchUmbracoIndex(string name,
            ElasticSearchConfig connectionConfiguration,
            IProfilingLogger profilingLogger,
            FieldDefinitionCollection fieldDefinitions = null,
            string analyzer = null,
            IValueSetValidator validator = null)
            : base(name,connectionConfiguration,  fieldDefinitions, analyzer, validator)
        {
           
        }

        private readonly bool _configBased = false;

        protected IProfilingLogger ProfilingLogger { get; }

        /// <summary>
        /// When set to true Umbraco will keep the index in sync with Umbraco data automatically
        /// </summary>

        public bool PublishedValuesOnly { get; protected set; } = false;

        /// <inheritdoc />
        public IEnumerable<string> GetFields()
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
            if (e.ValueSet.Values.TryGetValue("icon", out var icon) && e.ValueSet.Values.ContainsKey(IconFieldName) == false)
            {
                e.ValueSet.Values[IconFieldName] = icon;
            }
        }
        
       
    }
}