using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Elasticsearch.Net;
using Examine;
using Examine.LuceneEngine.Providers;
using Examine.Providers;
using Nest;
using Novicell.Examine.ElasticSearch.EventArgs;
using Novicell.Examine.ElasticSearch.Model;
using Umbraco.Core;
using DocumentWritingEventArgs = Novicell.Examine.ElasticSearch.EventArgs.DocumentWritingEventArgs;

namespace Novicell.Examine.ElasticSearch.Indexers
{
    public class ElasticSearchBaseIndex : BaseIndexProvider, IDisposable
    {
        public readonly ElasticSearchConfig _connectionConfiguration;
        private bool? _exists;
        private bool isReindexing = false;
        private bool _isUmbraco = false;
        public readonly Lazy<ElasticClient> _client;
        private ElasticClient _indexer;
        private static readonly object ExistsLocker = new object();
        public readonly Lazy<ElasticSearchSearcher> _searcher;
        public event EventHandler<MappingOperationEventArgs> Mapping;

        /// <summary>
        /// Occurs when [document writing].
        /// </summary>
        public event EventHandler<DocumentWritingEventArgs> DocumentWriting;

        public string indexName { get; set; }

        private string prefix = ConfigurationManager.AppSettings.AllKeys.Any(s => s == "examine:ElasticSearch.Prefix")
            ? ConfigurationManager.AppSettings["examine:ElasticSearch.Prefix"]
            : "";

        public string indexAlias { get; set; }
        private string tempindexAlias { get; set; }
        public string ElasticURL { get; set; }


        public ElasticSearchBaseIndex(string name,
            ElasticSearchConfig connectionConfiguration,
            FieldDefinitionCollection fieldDefinitions = null,
            string analyzer = null,
            IValueSetValidator validator = null, bool isUmbraco = false)
            : base(name.ToLowerInvariant(), //TODO: Need to 'clean' the name according to Azure Search rules
                fieldDefinitions ?? new FieldDefinitionCollection(), validator)
        {
            _connectionConfiguration = connectionConfiguration;
            _isUmbraco = isUmbraco;
            Analyzer = analyzer;
            ElasticURL = ConfigurationManager.AppSettings[$"examine:ElasticSearch[{name}].Url"];
            _searcher = new Lazy<ElasticSearchSearcher>(CreateSearcher);
            _client = new Lazy<ElasticClient>(CreateElasticSearchClient);
            indexAlias = prefix + Name;
            tempindexAlias = indexAlias + "temp";
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
            descriptor.Keyword(s => s.Name("Id"));
            descriptor.Keyword(s => s.Name(FormatFieldName(LuceneIndex.ItemIdFieldName)));
            descriptor.Keyword(s => s.Name(FormatFieldName(LuceneIndex.ItemTypeFieldName)));
            descriptor.Keyword(s => s.Name(FormatFieldName(LuceneIndex.CategoryFieldName)));

            foreach (FieldDefinition field in fieldDefinitionCollection)
            {
                FromExamineType(descriptor, field);
            }

            var docArgs = new MappingOperationEventArgs(descriptor);
            onMapping(docArgs);

            return descriptor;
        }

        private void FromExamineType(PropertiesDescriptor<Document> descriptor, FieldDefinition field)
        {
            switch (field.Type.ToLowerInvariant())
            {
                case "date":
                case "datetimeoffset":
                case "datetime":
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
                case "raw":
                    descriptor.Text(s => s.Name(field.Name).Analyzer("keyword"));
                    break;
                default:
                    descriptor.Text(s => s.Name(field.Name).Analyzer(FromLuceneAnalyzer(Analyzer)));
                    break;
            }
        }

        protected virtual void onMapping(MappingOperationEventArgs mappingArgs)
        {
            Mapping?.Invoke(this, mappingArgs);
        }

        protected virtual void OnDocumentWriting(DocumentWritingEventArgs docArgs)
        {
            DocumentWriting?.Invoke(this, docArgs);
        }

        private static string FromLuceneAnalyzer(string analyzer)
        {
            if (string.IsNullOrEmpty(analyzer) || !analyzer.Contains(","))
                return "simple";

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
            if (analyzer.Contains("StopAnalyzer"))
                return "stop";
            //if the above fails, return standard
            return "simple";
        }

        public void EnsureIndex(bool forceOverwrite)
        {
            if (!forceOverwrite && _exists.HasValue && _exists.Value) return;

            var indexExists = IndexExists();
            if (indexExists && !forceOverwrite) return;
            if (TempIndexExists() && !isReindexing) return;
            CreateNewIndex(indexExists);
        }

        private void CreateNewIndex(bool indexExists)
        {
            lock (ExistsLocker)
            {
                _client.Value.Indices.BulkAlias(ba => ba
                    .Remove(remove => remove.Index("*").Alias(tempindexAlias)));
                indexName = prefix + Name + "_" +
                            DateTime.Now.ToString("dd_MM_yyyy_HH_mm_ss");
                var index = _client.Value.Indices.Create(indexName, c => c
                    .Mappings(ms => ms.Map<Document>(
                        m => m.AutoMap()
                            .Properties(ps => CreateFieldsMapping(ps, FieldDefinitionCollection))
                    ))
                );
                var indexesMappedToAlias = _client.Value.GetIndicesPointingToAlias(indexAlias).ToList();
                if (!indexExists || indexesMappedToAlias.Count == 0)
                {
                    var bulkAliasResponse = _client.Value.Indices.BulkAlias(ba => ba
                        .Add(add => add.Index(indexName).Alias(indexAlias))
                    );
                }
                else
                {
                    isReindexing = true;
                    _client.Value.Indices.BulkAlias(ba => ba
                        .Add(add => add.Index(indexName).Alias(tempindexAlias))
                    );
                }

                _exists = true;
            }
        }

        private ElasticSearchSearcher CreateSearcher()
        {
            return new ElasticSearchSearcher(_connectionConfiguration, Name, indexName);
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
                try
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
                            ad[FormatFieldName(i.Key)] = i.Value.Count == 1 ? i.Value[0] : i.Value;
                    }

                    var docArgs = new DocumentWritingEventArgs(d, ad);
                    OnDocumentWriting(docArgs);
                    descriptor.Index<Document>(op => op.Index(indexTarget).Document(ad).Id(d.Id));
                }
                catch (Exception e)
                {
                }
            }

            return descriptor;
        }

        protected override void PerformIndexItems(IEnumerable<ValueSet> op, Action<IndexOperationEventArgs> onComplete)
        {
            var indexesMappedToAlias = _client.Value.GetIndicesPointingToAlias(indexAlias).ToList();
            EnsureIndex(false);

            var indexTarget = isReindexing ? tempindexAlias : indexAlias;


            var indexer = GetIndexClient();
            var totalResults = 0;
            //batches can only contain 1000 records
            foreach (var rowGroup in op.InGroupsOf(1))
            {
                var batch = ToElasticSearchDocs(rowGroup, indexTarget);


                var indexResult = indexer.Bulk(e => batch);

                totalResults += indexResult.Items.Count;
            }


            if (isReindexing)
            {
                indexer.Indices.BulkAlias(ba => ba
                    .Remove(remove => remove.Index("*").Alias(indexAlias))
                    .Add(add => add.Index(indexName).Alias(indexAlias))
                );


                indexesMappedToAlias.Where(e => e != indexName).ToList()
                    .ForEach(e => _client.Value.Indices.Delete(new DeleteIndexRequest(e)));
            }


            onComplete(new IndexOperationEventArgs(this, totalResults));
        }

        protected override void PerformDeleteFromIndex(IEnumerable<string> itemIds,
            Action<IndexOperationEventArgs> onComplete)
        {
            var descriptor = new BulkDescriptor();

            foreach (var id in itemIds.Where(x => !string.IsNullOrWhiteSpace(x)))
                descriptor.Index(indexAlias).Delete<Document>(x => x
                        .Id(id))
                    .Refresh(Refresh.WaitFor);

            var response = _client.Value.Bulk(descriptor);
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
            var indexesMappedToAlias = _client.Value.GetIndicesPointingToAlias(indexAlias).ToList();
            if (indexesMappedToAlias.Count > 0)
            {
                indexName = indexesMappedToAlias.FirstOrDefault();
                return true;
            }

            return false;
        }

        public bool TempIndexExists()
        {
            var indexesMappedToAlias = _client.Value.GetIndicesPointingToAlias(indexAlias).ToList();
            if (indexesMappedToAlias.Count > 0)
            {
                indexName = indexesMappedToAlias.FirstOrDefault();
                isReindexing = true;
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            _indexer?.DisposeIfDisposable();
            if (_client.IsValueCreated)
                _client.Value.DisposeIfDisposable();
        }


        public IEnumerable<string> GetFields()
        {
            return _searcher.Value.AllFields;
        }

        #region IIndexDiagnostics

        public int DocumentCount =>
            (int) (IndexExists() ? _client.Value.Count<Document>(e => e.Index(indexAlias)).Count : 0);

        public int FieldCount => IndexExists() ? _searcher.Value.AllFields.Length : 0;

        #endregion
    }
}