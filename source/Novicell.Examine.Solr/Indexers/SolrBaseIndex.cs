using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using CommonServiceLocator;
using Examine;
using Examine.LuceneEngine.Providers;
using Examine.Providers;
using Novicell.Examine.Solr.Model;
using SolrNet;
using SolrNet.Impl;
using SolrNet.Mapping;
using DocumentWritingEventArgs = Novicell.Examine.ElasticSearch.EventArgs.DocumentWritingEventArgs;

namespace Novicell.Examine.Solr.Indexers
{
    public class SolrBaseIndex : BaseIndexProvider, IDisposable
    {
        public readonly SolrConfig ConnectionConfiguration;
        private bool? _exists;
        private bool isReindexing = false;
        private bool _isUmbraco = false;
        public readonly Lazy<ISolrOperations<Document>> _client;
        private static readonly object ExistsLocker = new object();
        public readonly Lazy<SolrSearcher> _searcher;
        private ISolrOperations<Document> _indexer;
        private readonly ISolrCoreAdmin _solrCoreAdmin;

        /// <summary>
        /// Occurs when [document writing].
        /// </summary>
        public event EventHandler<DocumentWritingEventArgs> DocumentWriting;

        public string indexName { get; set; }

        private string prefix = ConfigurationManager.AppSettings.AllKeys.Any(s => s == "examine:Solr.Prefix")
            ? ConfigurationManager.AppSettings["examine:Solr.Prefix"]
            : "";

        public string indexAlias { get; set; }
        public string SolRURL { get; set; }


        public SolrBaseIndex(string name,
            SolrConfig connectionConfiguration,
            FieldDefinitionCollection fieldDefinitions = null,
            string analyzer = null,
            IValueSetValidator validator = null, bool isUmbraco = false)
            : base(name.ToLowerInvariant(), //TODO: Need to 'clean' the name according to Azure Search rules
                fieldDefinitions ?? new FieldDefinitionCollection(), validator)
        {
            indexName = name;
            ConnectionConfiguration = connectionConfiguration;
            _isUmbraco = isUmbraco;
            Analyzer = analyzer;
            SolRURL = connectionConfiguration.SolrCoreIndexUrl;
            _searcher = new Lazy<SolrSearcher>(CreateSearcher);
            _client = new Lazy<ISolrOperations<Document>>(CreateSolrConnectionOperation);
            indexAlias = prefix + Name;
            var headerParser = ServiceLocator.Current.GetInstance<ISolrHeaderResponseParser>();
            var statusParser = ServiceLocator.Current.GetInstance<ISolrStatusResponseParser>();
            _solrCoreAdmin = new SolrCoreAdmin(ConnectionConfiguration.AdminConnection, headerParser, statusParser);
        }


        public string Analyzer { get; }

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
                _solrCoreAdmin.Create(indexName,indexName,"solrconfig.xml","schema.xml","data");


                isReindexing = true;

                _exists = true;
            }
        }

        private SolrSearcher CreateSearcher()
        {
            return default(SolrSearcher);
            // return new ElasticSearchSearcher(ConnectionConfiguration, Name, indexName);
        }

        private ISolrOperations<Document> GetIndexClient()
        {
            return _indexer ?? (_indexer = _client.Value);
        }

        public static string FormatFieldName(string fieldName)
        {
            return $"{fieldName.Replace(".", "_")}";
        }


        protected override void PerformIndexItems(IEnumerable<ValueSet> op, Action<IndexOperationEventArgs> onComplete)
        {
            EnsureIndex(false);

            var indexTarget = indexAlias;
            var indexer = GetIndexClient();
            var totalResults = 0;
            IEnumerable<Document> documents;

            if (isReindexing)
            {
            }

            documents = ToSolRDocuments(op);
            _client.Value.AddRange(documents);
            _client.Value.Commit();
            onComplete(new IndexOperationEventArgs(this, totalResults));
        }

        private IEnumerable<Document> ToSolRDocuments(IEnumerable<ValueSet> docs)
        {
            List<Document> documents = new List<Document>();
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
                    documents.Add(ad);
                }
                catch (Exception e)
                {
                }
            }

            return documents;
        }

        protected override void PerformDeleteFromIndex(IEnumerable<string> itemIds,
            Action<IndexOperationEventArgs> onComplete)
        {
            _client.Value.DeleteAsync(itemIds);
            _client.Value.Commit();
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
            var coreStatus = _solrCoreAdmin.Status(indexName);
            if (coreStatus.Name == null)
            {
                return false;
            }

            return true;
            return false;
        }

        public bool TempIndexExists()
        {
          

            return false;
        }

        public void Dispose()
        {
        }


        public IEnumerable<string> GetFields()
        {
            return new List<string>();
            // return _searcher.Value.AllFields;
        }

        private ISolrOperations<Document> CreateSolrConnectionOperation()
        {
            return ServiceLocator.Current.GetInstance<ISolrOperations<Document>>();
        }

        #region IIndexDiagnostics

        public int DocumentCount =>
            (int) (IndexExists() ? _solrCoreAdmin.Status(indexName).Index.TotalDocumentCount : 0);

        public int FieldCount => IndexExists() ? _searcher.Value.AllFields.Length : 0;

        #endregion
    }
}