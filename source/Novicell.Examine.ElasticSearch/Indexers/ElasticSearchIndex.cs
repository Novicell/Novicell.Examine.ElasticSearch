using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Elasticsearch.Net;
using Examine;
using Examine.LuceneEngine.Providers;
using Examine.Providers;
using Nest;
using Novicell.Examine.ElasticSearch.Model;
using Umbraco.Core;
using Umbraco.Core.Services;

namespace Novicell.Examine.ElasticSearch
{
    public class ElasticSearchIndex : BaseIndexProvider, IDisposable
    {
        private readonly ElasticSearchConfig _connectionConfiguration;
        private bool? _exists;
        private bool isReindexing = false;
        private readonly Lazy<ElasticClient> _client;
        private ElasticClient _indexer;
        private static readonly object ExistsLocker = new object();
        private readonly Lazy<ElasticSearchSearcher> _searcher;
        private const string SpecialFieldPrefix = "__";
        public const string IndexPathFieldName = SpecialFieldPrefix + "Path";
        public const string NodeKeyFieldName = SpecialFieldPrefix + "Key";
        public const string IconFieldName = SpecialFieldPrefix + "Icon";
        public const string PublishedFieldName = SpecialFieldPrefix + "Published";
        public string indexName { get; set; }
        public string ElasticURL { get; set; }

        /// <summary>
        /// The prefix added to a field when it is duplicated in order to store the original raw value.
        /// </summary>
        public const string RawFieldPrefix = SpecialFieldPrefix + "Raw_";

        public ElasticSearchIndex(string name,
            ElasticSearchConfig connectionConfiguration,
            IPublicAccessService publicAccessService,
            FieldDefinitionCollection fieldDefinitions = null,
            string analyzer = null,
            IValueSetValidator validator = null)
            : base(name.ToLowerInvariant(), //TODO: Need to 'clean' the name according to Azure Search rules
                fieldDefinitions ?? new FieldDefinitionCollection(), validator)
        {
            _connectionConfiguration = connectionConfiguration;
            Analyzer = analyzer;
            ElasticURL = ConfigurationManager.AppSettings[$"examine:ElasticSearch[{name}].Url"];
            _searcher = new Lazy<ElasticSearchSearcher>(CreateSearcher);
            _client = new Lazy<ElasticClient>(CreateElasticSearchClient);
        }

        private ElasticClient CreateElasticSearchClient()
        {
            var serviceClient = new ElasticClient(_connectionConfiguration.ConnectionConfiguration);
            return serviceClient;
        }

        public string Analyzer { get; }

        private PropertiesDescriptor<Document> CreateFieldsMapping(PropertiesDescriptor<Document> descriptor,
            FieldDefinitionCollection fieldDefinitionCollection)
        {
            foreach (FieldDefinition field in fieldDefinitionCollection)
            {
                FromExamineType(descriptor, field);
            }


            return descriptor;
        }

        private void FromExamineType(PropertiesDescriptor<Document> descriptor, FieldDefinition field)
        {
            switch (field.Type.ToLowerInvariant())
            {
                case "date":
                case "datetimeoffset":
                    descriptor.Date(s => s.Name(field.Name));
                    break;
                case "double":
                    descriptor.Number(s => s.Name(field.Name).Type(NumberType.Double));
                    break;
                case "float":
                    descriptor.Number(s => s.Name(field.Name).Type(NumberType.Float));
                    break;

                case "long":
                    descriptor.Number(s => s.Name(field.Name).Type(NumberType.Long));
                    break;
                case "int":
                case "number":
                    descriptor.Number(s => s.Name(field.Name).Type(NumberType.Integer));
                    break;
                default:
                    descriptor.Text(s => s.Name(field.Name).Analyzer(FromLuceneAnalyzer(Analyzer)));
                    break;
            }
        }

        private static string FromLuceneAnalyzer(string analyzer)
        {
            //not fully qualified, just return the type
            if (!analyzer.Contains(","))
                return "standard";

            //if it contains a comma, we'll assume it's an assembly typed name


            if (analyzer.Contains("StandardAnalyzer"))
                return "standard";
            if (analyzer.Contains("WhitespaceAnalyzer"))
                return "whitespace";
            if (analyzer.Contains("SimpleAnalyzer"))
                return "simple";
            if (analyzer.Contains("KeywordAnalyzer"))
                return "keyword";
            if (analyzer.Contains("StopAnalyzer"))
                return "stop";
            if (analyzer.Contains("ArabicAnalyzer"))
                return "arabic";

            if (analyzer.Contains("BrazilianAnalyzer"))
                return "brazilian";

            if (analyzer.Contains("ChineseAnalyzer"))
                return "chinese";

            if (analyzer.Contains("CJKAnalyzer"))
                return "cjk";

            if (analyzer.Contains("CzechAnalyzer"))
                return "czech";

            if (analyzer.Contains("DutchAnalyzer"))
                return "dutch";

            if (analyzer.Contains("FrenchAnalyzer"))
                return "french";

            if (analyzer.Contains("GermanAnalyzer"))
                return "german";

            if (analyzer.Contains("RussianAnalyzer"))
                return "russian";

            //if the above fails, return standard
            return "standard";
        }

        private void EnsureIndex(bool forceOverwrite)
        {
            if (!forceOverwrite && _exists.HasValue && _exists.Value) return;

            var indexExists = IndexExists();
            if (indexExists && !forceOverwrite) return;


            CreateNewIndex(indexExists);
        }

        private void CreateNewIndex(bool indexExists)
        {
            lock (ExistsLocker)
            {
                indexName = Name + "_" +
                            DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");
                var index = _client.Value.CreateIndex(indexName, c => c
                    .Mappings(ms => ms.Map<Document>(
                        m => m.AutoMap()
                            .Properties(ps => CreateFieldsMapping(ps, FieldDefinitionCollection))
                    )).Aliases(a => a.Alias(Name))
                );
                isReindexing = true;
                _exists = true;
            }
        }

        private ElasticSearchSearcher CreateSearcher()
        {
            return new ElasticSearchSearcher(_connectionConfiguration, Name);
        }

        private ElasticClient GetIndexClient()
        {
            return _indexer ?? (_indexer = _client.Value);
        }

        public static string FormatFieldName(string fieldName)
        {
            return $"{fieldName.Replace(".", "_")}";
        }

        private BulkDescriptor ToElasticSearchDocs(IEnumerable<ValueSet> docs, string indexTarget)
        {
            var descriptor = new BulkDescriptor();


            foreach (var d in docs)
            {
                //this is just a dictionary
                var ad = new Document
                {
                    ["Id"] = d.Id,
                    [FormatFieldName(LuceneIndex.ItemIdFieldName)] = d.Id,
                    [FormatFieldName(LuceneIndex.ItemTypeFieldName)] = d.ItemType,
                    [FormatFieldName(LuceneIndex.CategoryFieldName)] = d.Category
                };
                foreach (var i in d.Values)
                {
                    if (i.Value.Count > 0)
                        ad[FormatFieldName(i.Key)] = i.Value;
                }

                descriptor.Index<Document>(op => op.Index(indexTarget).Document(ad).Id(d.Id));
            }

            return descriptor;
        }

        protected override void PerformIndexItems(IEnumerable<ValueSet> op, Action<IndexOperationEventArgs> onComplete)
        {
            var indexTarget = isReindexing ? indexName : Name;
            if (isReindexing)
            {
                var index = _client.Value.CreateIndex(indexName
                    , c => c
                        .Mappings(ms => ms.Map<Document>(
                            m => m.AutoMap()
                                .Properties(ps => CreateFieldsMapping(ps, FieldDefinitionCollection))
                        ))
                );
            }

            var indexer = GetIndexClient();
            var totalResults = 0;
            //batches can only contain 1000 records
            foreach (var rowGroup in op.InGroupsOf(1))
            {
                var batch = ToElasticSearchDocs(rowGroup, indexTarget);


                var indexResult = indexer.Bulk(e => batch);
                //TODO: Do we need to check for errors in any of the results?
                totalResults += indexResult.Items.Count;

                if (indexResult.Errors)
                {
                    foreach (var itemWithError in indexResult.ItemsWithErrors)
                    {
                        Console.WriteLine("Failed to index document {0}: {1}", itemWithError.Id, itemWithError.Error);
                    }
                }
            }

            var bulkAliasResponse = indexer.Alias(ba => ba
                .Add(add => add.Alias(Name).Index(indexName))
                .Remove(remove => remove.Alias(Name).Index("*")));
            onComplete(new IndexOperationEventArgs(this, totalResults));
        }

        protected override void PerformDeleteFromIndex(IEnumerable<string> itemIds,
            Action<IndexOperationEventArgs> onComplete)
        {
            var descriptor = new BulkDescriptor();

            foreach (var id in itemIds.Where(x => !string.IsNullOrWhiteSpace(x)))
                descriptor.Delete<Document>(x => x
                        .Id(id))
                    .Refresh(Refresh.WaitFor);

            var response = _client.Value.Bulk(descriptor);
            if (response.Errors)
            {
                foreach (var itemWithError in response.ItemsWithErrors)
                {
                    Console.WriteLine("Failed to index document {0}: {1}", itemWithError.Id, itemWithError.Error);
                }
            }
        }

        public override ISearcher GetSearcher()
        {
            return _searcher.Value;
        }

        public override void CreateIndex()
        {
            EnsureIndex(true);
        }

        public override bool IndexExists()
        {
            return _client.Value.IndexExists(Name).Exists;
        }

        public void Dispose()
        {
            _indexer?.DisposeIfDisposable();
            if (_client.IsValueCreated)
                _client.Value.DisposeIfDisposable();
        }

        public long DocumentCount => _client.Value.Count<Document>(e => e.Index(Name)).Count;
        public int FieldCount => _searcher.Value.AllFields.Length;
    }
}