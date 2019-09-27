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

namespace Novicell.Examine.ElasticSearch.Helpers
{
    public static class ElasticHelpers
    {
        public static IEnumerable<ISearchResult> ConvertResult(this ISearchResponse<Document> result)
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
    }
}