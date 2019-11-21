using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Examine;
using Examine.Providers;
using Examine.Search;
using Nest;
using Novicell.Examine.ElasticSearch.Model;
using Novicell.Examine.ElasticSearch.Queries;
using Umbraco.Core;
using IQuery = Examine.Search.IQuery;
using SortField = Lucene.Net.Search.SortField;

namespace Novicell.Examine.ElasticSearch
{
    public class ElasticSearchSearcher : BaseSearchProvider, IDisposable
    {
        private readonly ElasticSearchConfig _connectionConfiguration;
        public readonly Lazy<ElasticClient> _client;
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
        public ElasticSearchSearcher(ElasticSearchConfig connectionConfiguration, string name,string indexName) : base(name)
        {
            _connectionConfiguration = connectionConfiguration;
            _indexName = name;
            _client = new Lazy<ElasticClient>(CreateElasticSearchClient);
            indexAlias = prefix + Name;
            IndexName = indexName;
        }
        private ElasticClient CreateElasticSearchClient()
        {
            var serviceClient = new ElasticClient(_connectionConfiguration.ConnectionConfiguration);
            return serviceClient;
        }
        public bool IndexExists
        {
            get
            {
               
                
                        var indexesMappedToAlias = _client.Value.GetAlias(descriptor => descriptor.Name(indexAlias)).Indices;
                        if(indexesMappedToAlias.Count>0){
                            _exists = true;
                            return true;
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
               
                var indexesMappedToAlias = _client.Value.GetAlias(descriptor => descriptor.Name(indexAlias))
                    .Indices.Select(x => x.Key).ToList();
                IGetMappingResponse response = _client.Value.GetMapping<Document>(mapping => mapping.Index(indexAlias).AllTypes());
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
                Type=      TextQueryType.Phrase
                
            };
         
            return new ElasticSearchSearchResults(_client.Value, query, indexAlias, _sortFields, maxResults,maxResults *(page-1));
        }
        public override ISearchResults Search(string searchText, int maxResults = 500)
        {
            var query = new MultiMatchQuery
            {
                
                Query = searchText,
                Analyzer = "standard",
                Slop = 2,
                Type=      TextQueryType.Phrase
                
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
        return new ElasticSearchQuery(this, category, AllFields, defaultOperation,indexAlias);
        }

        public void Dispose()
        {
            
            if (_client.IsValueCreated)
                _client.Value.DisposeIfDisposable();
        }
    }
}