using Lucene.Net.Search;

namespace Novicell.Examine.ElasticSearch.Indexing
{
    public interface IIndexRangeValueType<T> : IIndexRangeValueType where T : struct
    {
        Query GetQuery(T? lower, T? upper, bool lowerInclusive = true, bool upperInclusive = true);
    }

    public interface IIndexRangeValueType 
    {
    }
}