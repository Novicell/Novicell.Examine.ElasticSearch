using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using CommonServiceLocator;
using Examine;
using Examine.Providers;
using Examine.Search;
using Novicell.Examine.Solr.Model;
using SolrNet;
using IQuery = Examine.Search.IQuery;
using SortField = Lucene.Net.Search.SortField;

namespace Novicell.Examine.Solr
{
    public class SolrSearcher : BaseSearchProvider, IDisposable
    {
        private readonly SolrConfig _connectionConfiguration;
        public readonly Lazy<ISolrOperations<Document>> _client;
        internal readonly List<SortField> _sortFields = new List<SortField>();
        private string[] _allFields;
        private IProperties _fieldsMapping;
        private bool? _exists;
        private string _indexName;
        private string IndexName;

        public string indexAlias { get; set; }

        private string prefix = ConfigurationManager.AppSettings.AllKeys.Any(s => s == "examine:ElasticSearch.Prefix")
            ? ConfigurationManager.AppSettings["examine:ElasticSearch.Prefix"]
            : "";

        private static readonly string[] EmptyFields = new string[0];

        public SolrSearcher(SolrConfig connectionConfiguration, string name, string indexName) :
            base(name)
        {
            _connectionConfiguration = connectionConfiguration;
            _indexName = name;
            _client = new Lazy<ISolrOperations<Document>>(CreateSolrConnectionOperation);
            indexAlias = prefix + Name;
            IndexName = indexName;
        }

 

        public bool IndexExists
        {
            get
            {
                var aliasExists = _client.Value.Indices.Exists(indexAlias).Exists;
                if (aliasExists)
                {
                    var indexesMappedToAlias = _client.Value.GetIndicesPointingToAlias(indexAlias).ToList();
                    if (indexesMappedToAlias.Count > 0)
                    {
                        _exists = true;
                        return true;
                    }
                }

                _exists = false;
                return false;
            }
        }


        public string[] AllFields
        {
            get
            {
                if (!IndexExists) return EmptyFields;

                IEnumerable<PropertyName> keys = AllProperties.Keys;

                _allFields = keys.Select(x => x.Name).ToArray();
                return _allFields;
            }
        }

        public IProperties AllProperties
        {
            get
            {
                if (!IndexExists) return null;
                if (_fieldsMapping != null) return _fieldsMapping;

                var indexesMappedToAlias = _client.Value.GetIndicesPointingToAlias(indexAlias).ToList();
                GetMappingResponse response =
                    _client.Value.Indices.GetMapping(new GetMappingRequest {IncludeTypeName = false});
                _fieldsMapping = response.GetMappingFor(indexesMappedToAlias[0]).Properties;
                return _fieldsMapping;
            }
        }

        public ISearchResults Search(string searchText, int maxResults = 500, int page = 1)
        {
            var query = new MultiMatchQuery
            {
                Query = searchText,
                Analyzer = "standard",
                Slop = 2,
                Type = TextQueryType.Phrase
            };

            return new ElasticSearchSearchResults(_client.Value, query, indexAlias, _sortFields, maxResults,
                maxResults * (page - 1));
        }

        public override ISearchResults Search(string searchText, int maxResults = 500)
        {
            var query = new MultiMatchQuery
            {
                Query = searchText,
                Analyzer = "standard",
                Slop = 2,
                Type = TextQueryType.Phrase
            };
            return new ElasticSearchSearchResults(_client.Value, query, indexAlias, _sortFields, maxResults);
        }

        public ISearchResults Search(QueryContainer queryContainer, int maxResults = 500)
        {
            return new ElasticSearchSearchResults(_client.Value, queryContainer, indexAlias, _sortFields, maxResults);
        }

        public ISearchResults Search(ISearchRequest searchRequest)
        {
            return new ElasticSearchSearchResults(_client.Value, searchRequest, indexAlias, _sortFields);
        }

        public ISearchResults Search(Func<SearchDescriptor<Document>, ISearchRequest> searchSelector)
        {
            return new ElasticSearchSearchResults(_client.Value, searchSelector, indexAlias, _sortFields);
        }

        public override IQuery CreateQuery(string category = null,
            BooleanOperation defaultOperation = BooleanOperation.And)
        {
            return new ElasticSearchQuery(this, category, AllFields, defaultOperation, indexAlias);
        }
        private ISolrOperations<Document> CreateSolrConnectionOperation()
        {
            
            return ServiceLocator.Current.GetInstance<ISolrOperations<Document>>();
        }
        public void Dispose()
        {
        
        }
    }
}