﻿using Lucene.Net.Documents;
using Lucene.Net.Search;

namespace Novicell.Examine.ElasticSearch.Indexing
{
    public class DoubleType : IndexFieldValueTypeBase, IIndexRangeValueType<double>
    {
        public DoubleType(string fieldName, bool store= true)
            : base(fieldName, store)
        {
        }

        /// <summary>
        /// Can be sorted by the normal field name
        /// </summary>
        public override string SortableFieldName => FieldName;

        protected override void AddSingleValue(Document doc, object value)
        {
            if (!TryConvert(value, out double parsedVal))
                return;

            doc.Add(new NumericField(FieldName, Store ? Field.Store.YES : Field.Store.NO, true).SetDoubleValue(parsedVal));
        }

        public override Query GetQuery(string query, Searcher searcher)
        {
            return !TryConvert(query, out double parsedVal) ? null : GetQuery(parsedVal, parsedVal);
        }

        public Query GetQuery(double? lower, double? upper, bool lowerInclusive = true, bool upperInclusive = true)
        {
            return NumericRangeQuery.NewDoubleRange(FieldName,
                lower ?? double.MinValue,
                upper ?? double.MaxValue, lowerInclusive, upperInclusive);
        }
    }
}
