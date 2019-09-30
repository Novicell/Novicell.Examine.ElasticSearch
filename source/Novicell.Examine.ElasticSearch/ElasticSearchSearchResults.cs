using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Elasticsearch.Net;
using Examine;
using Examine.LuceneEngine.Providers;
using Lucene.Net.Search;
using Nest;
using Novicell.Examine.ElasticSearch.Helpers;
using Novicell.Examine.ElasticSearch.Model;
using SortField = Lucene.Net.Search.SortField;

namespace Novicell.Examine.ElasticSearch
{
    public class ElasticSearchSearchResults : ISearchResults
    {
        private readonly int? _maxResults;
        private readonly ElasticClient _client;
        private readonly BooleanQuery _luceneQuery;
        private QueryContainer _queryContainer;
        private SortDescriptor<Document> _sortDescriptor;
        private readonly string _indexName;
        private IEnumerable<ISearchResult> results;
        public long TotalItemCount { get; private set; }


        public ElasticSearchSearchResults(ElasticClient client, BooleanQuery luceneQuery, string indexName, List<SortField> sortFields,
            int? maxResults = null)
        {
            _luceneQuery = luceneQuery ?? throw new ArgumentNullException(nameof(luceneQuery));
            _maxResults = maxResults;
            _client = client;
            _indexName = indexName;
            _sortDescriptor = GetSortDescriptor(sortFields);

            results = DoSearch(null).ConvertResult();
        }

        public ElasticSearchSearchResults(ElasticClient client, QueryContainer queryContainer, string indexName, List<SortField> sortFields, int? maxResults = null)
        {
            _queryContainer = queryContainer ?? throw new ArgumentNullException(nameof(queryContainer));
            _maxResults = maxResults;
            _client = client;
            _indexName = indexName;
            _sortDescriptor = GetSortDescriptor(sortFields);

            results = DoSearch(null).ConvertResult();
        }

        public IEnumerator<ISearchResult> GetEnumerator()
        {
       
            return results.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

       

        private ISearchResponse<Document> DoSearch(int? skip)
        {
            ISearchResponse<Document> searchResult;
            if (_luceneQuery != null)
            {
                _queryContainer = new QueryContainer(new QueryStringQuery()
                {
                    Query = _luceneQuery.ToString()
                });
            }

           
            SearchDescriptor<Document> searchDescriptor = new SearchDescriptor<Document>();
           
            searchDescriptor.Index(_indexName)
                .Skip(skip)
                .Size(_maxResults)
                .Query(q => _queryContainer)
                .Sort(s => _sortDescriptor);

            var json = _client.RequestResponseSerializer.SerializeToString(searchDescriptor);
            searchResult = _client.Search<Document>(searchDescriptor);

            //TODO: Get filtering/range working
            //TODO: We need to escape the resulting query

            TotalItemCount = searchResult.Total;

            return searchResult;
        }

        public IEnumerable<ISearchResult> Skip(int skip)
        {
            results = DoSearch(skip).ConvertResult();
            return results;
        }

        public SortDescriptor<Document> GetSortDescriptor(List<SortField> fields)
        {
            SortDescriptor<Document> sortDescriptor = new SortDescriptor<Document>();

            foreach (var field in fields)
            {
                sortDescriptor.Field(field.Field, field.Reverse ? SortOrder.Descending : SortOrder.Ascending);
            }

            return sortDescriptor;
        }
    }
}