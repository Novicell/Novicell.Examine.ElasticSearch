using System;
using Lucene.Net.Documents;
using Lucene.Net.Search;

namespace Novicell.Examine.ElasticSearch.Indexing
{
    public class DateTimeType : IndexFieldValueTypeBase, IIndexRangeValueType<DateTime>
    {
        public DateTools.Resolution Resolution { get; }

        /// <summary>
        /// Can be sorted by the normal field name
        /// </summary>
        public override string SortableFieldName => FieldName;

        public DateTimeType(string fieldName, DateTools.Resolution resolution, bool store = true)
            : base(fieldName, store)
        {
            Resolution = resolution;
        }

        protected override void AddSingleValue(Document doc, object value)
        {
            if (!TryConvert(value, out DateTime parsedVal))
                return;

            var val = DateToLong(parsedVal);

            doc.Add(new NumericField(FieldName, Store ? Field.Store.YES : Field.Store.NO, true).SetLongValue(val));
        }

        /// <summary>
        /// Returns the ticks to be indexed, then use NumericRangeQuery to query against it
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        protected long DateToLong(DateTime date)
        {
            return DateTools.Round(date, Resolution).Ticks;
        }

        public override Query GetQuery(string query, Searcher searcher)
        {
            if (!TryConvert(query, out DateTime parsedVal))
                return null;

            return GetQuery(parsedVal, parsedVal);
        }

        public Query GetQuery(DateTime? lower, DateTime? upper, bool lowerInclusive = true, bool upperInclusive = true)
        {
            return new TermRangeQuery(FieldName,
                lower != null ? lower.Value.ToString("yyyy-MM-ddTHH:mm:ss") : string.Empty,
                upper != null ? upper.Value.ToString("yyyy-MM-ddTHH:mm:ss") : string.Empty, lowerInclusive, upperInclusive);
        }
    }
}
