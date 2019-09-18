using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Elasticsearch.Net;
using Examine;
using Examine.LuceneEngine.Providers;
using Lucene.Net.Search;
using Nest;
using Novicell.Examine.ElasticSearch.Model;

namespace Novicell.Examine.ElasticSearch
{
    public class ElasticSearchSearchResults : ISearchResults
    {
        private readonly int? _maxResults;
        private readonly ElasticClient _client;
        private readonly BooleanQuery _luceneQuery;
        private QueryContainer _queryContainer;
        private readonly string _indexName;

        public long TotalItemCount { get; private set; }


        public ElasticSearchSearchResults(ElasticClient client, BooleanQuery luceneQuery, string indexName,
            int? maxResults = null)
        {
            _luceneQuery = luceneQuery ?? throw new ArgumentNullException(nameof(luceneQuery));
            _maxResults = maxResults;
            _client = client;
            _indexName = indexName;
        }

        public ElasticSearchSearchResults(ElasticClient client, QueryContainer queryContainer, string indexName,
            int? maxResults = null)
        {
            _queryContainer = queryContainer ?? throw new ArgumentNullException(nameof(queryContainer));
            _maxResults = maxResults;
            _client = client;
            _indexName = indexName;
        }

        public IEnumerator<ISearchResult> GetEnumerator()
        {
            var result = DoSearch(null);
            return ConvertResult(result).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private static IEnumerable<ISearchResult> ConvertResult(ISearchResponse<Document> result)
        {
            return result.Hits.OrderByDescending(x => x.Score).Select(x =>
            {
                var id = x.Source[LuceneIndex.ItemIdFieldName].ToString();
                IDictionary<string, List<string>> results = new Dictionary<string, List<string>>();
                foreach (var d in x.Source)
                {
                    if (d.Key == null || d.Value == null) continue;
                    results[d.Key] = new List<string> {d.Value.ToString()};
                }

                var r = new SearchResult(id, Convert.ToInt64(x.Score), () => results);

                return r;
            });
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
                .Query(q => _queryContainer);

            var json = _client.RequestResponseSerializer.SerializeToString(searchDescriptor);
            searchResult = _client.Search<Document>(searchDescriptor);


            //TODO: Get sorting working
            //TODO: Get filtering/range working
            //TODO: We need to escape the resulting query

            TotalItemCount = searchResult.Total;

            return searchResult;
        }

        public IEnumerable<ISearchResult> Skip(int skip)
        {
            var result = DoSearch(skip);
            return ConvertResult(result);
        }
    }
}