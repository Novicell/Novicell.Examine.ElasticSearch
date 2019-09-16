using System;
using System.Collections.Generic;
using System.Linq;
using Examine;
using Examine.Providers;
using Examine.Search;
using Nest;
using Novicell.Examine.ElasticSearch.Model;
using Novicell.Examine.ElasticSearch.Queries;
using Umbraco.Core;
using IQuery = Examine.Search.IQuery;

namespace Novicell.Examine.ElasticSearch
{
    public class ElasticSearchSearcher : BaseSearchProvider, IDisposable
    {
        private readonly ElasticSearchConfig _connectionConfiguration;
        public readonly Lazy<ElasticClient> _client;
        private string[] _allFields;
        private IProperties _fieldsMapping;
        private bool? _exists;
        private string _indexName;
        private static readonly string[] EmptyFields = new string[0];
        public ElasticSearchSearcher(ElasticSearchConfig connectionConfiguration, string name) : base(name)
        {
            _connectionConfiguration = connectionConfiguration;
            _indexName = name;
            _client = new Lazy<ElasticClient>(CreateElasticSearchClient);
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
                if (_exists == null || !_exists.Value)
                {
                    _exists = _client.Value.IndexExists(_indexName).Exists;
                }
                return _exists.Value;
            }
        }
        public string[] AllFields
        {
            get
            {

                if (_allFields != null) return _allFields;
                if (!IndexExists) return EmptyFields;
           
                IEnumerable<PropertyName> keys = AllProperties.Keys;

                _allFields = keys.Select(x => x.ToString()).ToArray();
                return _allFields;
            }
        }

        public IProperties AllProperties
        {
            get
            {

                if (_fieldsMapping != null) return _fieldsMapping;
                if (!IndexExists) return null;
                var indexesMappedToAlias = _client.Value.GetAlias(descriptor => descriptor.Name(Name))
                    .Indices.Select(x => x.Key).ToList();
                var response = _client.Value.GetMapping<Document>(mapping => mapping.Index(Name).AllTypes());
                _fieldsMapping = response.GetMappingFor(indexesMappedToAlias[0]).Properties;
             
                return _fieldsMapping;
            } 
        }
        public override ISearchResults Search(string searchText, int maxResults = 500)
        {
            var query = new CommonTermsQuery()
            {

                Analyzer = "standard",
                Boost = 1.1,
                CutoffFrequency = 0.001,
                HighFrequencyOperator = Operator.And,
                LowFrequencyOperator = Operator.Or,
                MinimumShouldMatch = 1,
                Name = "named_query",
                Query = "nelly the elephant not as a"
            };
            return new ElasticSearchSearchResults(_client.Value, query, _indexName,maxResults);
        }
        public ISearchResults Search(QueryContainer queryContainer, int maxResults = 500)
        {
            return new ElasticSearchSearchResults(_client.Value, queryContainer, _indexName,maxResults);
        }
        public override IQuery CreateQuery(string category = null,
            BooleanOperation defaultOperation = BooleanOperation.And)
        {  
        return new ElasticSearchQuery(this, category, AllFields, defaultOperation,_indexName);
        }

        public void Dispose()
        {
            
            if (_client.IsValueCreated)
                _client.Value.DisposeIfDisposable();
        }
    }
}