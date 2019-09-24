using System;
using System.Collections.Generic;
using System.Linq;
using Examine;
using Examine.LuceneEngine.Indexing;
using Examine.LuceneEngine.Search;
using Examine.Search;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Nest;
using Novicell.Examine.ElasticSearch.Model;
using Umbraco.Core;
using SortField = Lucene.Net.Search.SortField;
using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;
using Version = Lucene.Net.Util.Version;

namespace Novicell.Examine.ElasticSearch.Queries
{
    public class ElasticSearchQuery : LuceneSearchQueryBase, IQueryExecutor
    {
        public readonly ElasticSearchSearcher _searcher;
        public string _indexName;
        internal readonly List<SortField> SortFields = new List<SortField>();
        internal Analyzer DefaultAnalyzer { get; } = new StandardAnalyzer(Version.LUCENE_29);
        internal static readonly LuceneSearchOptions EmptyOptions = new LuceneSearchOptions();

        public ElasticSearchQuery(ElasticSearchSearcher searcher, string category, string[] fields, BooleanOperation op,
            string indexName) : base(category, new StandardAnalyzer(Version.LUCENE_29), fields,
            EmptyOptions, op)
        {
            _searcher = searcher;
            _indexName = indexName;
        }

        public ElasticSearchQuery(ElasticSearchQuery previous, BooleanOperation op)
            : base(previous.Category, previous.DefaultAnalyzer, previous._searcher.AllFields, EmptyOptions, op)
        {
            _searcher = previous._searcher;
            _indexName = previous._indexName;
        }

        public ISearchResults Execute(int maxResults = 500)
        {
            return new ElasticSearchSearchResults(_searcher._client.Value, Query, _indexName, maxResults);
        }

        protected override LuceneBooleanOperationBase CreateOp()
        {
            return new ElasticSearchBooleanOperation(this);
        }

        public override IBooleanOperation Field<T>(string fieldName, T fieldValue)
            => RangeQueryInternal<T>(new[] {fieldName}, fieldValue, fieldValue);

        public override IBooleanOperation ManagedQuery(string query, string[] fields = null)
        {
            //TODO: Instead of AllFields here we should have a reference to the FieldDefinitionCollection
            foreach (var field in fields ?? AllFields)
            {
                var fullTextQuery = FullTextType.GenerateQuery(field, query, DefaultAnalyzer);
                Query.Add(fullTextQuery, Occurrence);
            }

            return new ElasticSearchBooleanOperation(this);
        }


        public override IBooleanOperation RangeQuery<T>(string[] fields, T? min, T? max, bool minInclusive = true,
            bool maxInclusive = true)
            => RangeQueryInternal(fields, min, max, minInclusive, maxInclusive);

        protected override INestedBooleanOperation FieldNested<T>(string fieldName, T fieldValue)
            => RangeQueryInternal<T>(new[] {fieldName}, fieldValue, fieldValue);

        protected override INestedBooleanOperation ManagedQueryNested(string query, string[] fields = null)
        {
            //TODO: Instead of AllFields here we should have a reference to the FieldDefinitionCollection
            foreach (var field in fields ?? AllFields)
            {
                var fullTextQuery = FullTextType.GenerateQuery(field, query, DefaultAnalyzer);
                Query.Add(fullTextQuery, Occurrence);
            }

            return new ElasticSearchBooleanOperation(this);
        }

        protected override INestedBooleanOperation RangeQueryNested<T>(string[] fields, T? min, T? max,
            bool minInclusive = true,
            bool maxInclusive = true)
            => RangeQueryInternal(fields, min, max, minInclusive, maxInclusive);

        private IIndexFieldValueType FromElasticType(IProperty property)
        {
            switch (property.Type.ToLowerInvariant())
            {
                case "date":
                    return new DateTimeType(property.Name.ToString(), DateTools.Resolution.MILLISECOND);
                case "double":
                    return new DoubleType(property.Name.ToString());

                case "float":
                    return new SingleType(property.Name.ToString());

                case "long":
                    return new Int64Type(property.Name.ToString());
                case "integer":
                    return new Int32Type(property.Name.ToString());
                default:
                    return new FullTextType(property.Name.ToString(), new StandardAnalyzer(Version.LUCENE_CURRENT));
            }
        }

        internal ElasticSearchBooleanOperation RangeQueryInternal<T>(string[] fields, T? min, T? max,
            bool minInclusive = true, bool maxInclusive = true)
            where T : struct
        {
            Query.Add(new LateBoundQuery(() =>
            {
                //Strangely we need an inner and outer query. If we don't do this then the lucene syntax returned is incorrect 
                //since it doesn't wrap in parenthesis properly. I'm unsure if this is a lucene issue (assume so) since that is what
                //is producing the resulting lucene string syntax. It might not be needed internally within Lucene since it's an object
                //so it might be the ToString() that is the issue.
                var outer = new BooleanQuery();
                var inner = new BooleanQuery();

                var fieldsMapping = _searcher.AllProperties;

                foreach (var valueType in fieldsMapping.Where(e => fields.Contains(e.Key.Name)))
                {
                    if (FromElasticType(valueType.Value) is IIndexRangeValueType<T> type)
                    {
                        var q = type.GetQuery(min, max, minInclusive, maxInclusive);
                        if (q != null)
                        {
                            //CriteriaContext.FieldQueries.Add(new KeyValuePair<IIndexFieldValueType, Query>(type, q));
                            inner.Add(q, Occur.SHOULD);
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Could not perform a range query on the field {valueType.Key}, it's value type is {valueType.Value.Type}");
                    }
                }

                outer.Add(inner, Occur.SHOULD);

                return outer;
            }), Occurrence);


            return new ElasticSearchBooleanOperation(this);
        }

        private ElasticSearchBooleanOperation OrderByInternal(bool descending, params SortableField[] fields)
        {
            if (fields == null) throw new ArgumentNullException(nameof(fields));

            foreach (var f in fields)
            {
                var fieldName = f.FieldName;

                var defaultSort = SortField.STRING;

                switch (f.SortType)
                {
                    case SortType.Score:
                        defaultSort = SortField.SCORE;
                        break;
                    case SortType.DocumentOrder:
                        defaultSort = SortField.DOC;
                        break;
                    case SortType.String:
                        defaultSort = SortField.STRING;
                        break;
                    case SortType.Int:
                        defaultSort = SortField.INT;
                        break;
                    case SortType.Float:
                        defaultSort = SortField.FLOAT;
                        break;
                    case SortType.Long:
                        defaultSort = SortField.LONG;
                        break;
                    case SortType.Double:
                        defaultSort = SortField.DOUBLE;
                        break;
                    case SortType.Short:
                        defaultSort = SortField.SHORT;
                        break;
                    case SortType.Byte:
                        defaultSort = SortField.BYTE;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
          
                SortFields.Add(new SortField(fieldName, defaultSort, descending));
            }

            return new ElasticSearchBooleanOperation(this);
        }

        public IOrdering OrderBy(params SortableField[] fields) => OrderByInternal(false, fields);

        public IOrdering OrderByDescending(params SortableField[] fields) => OrderByInternal(true, fields);
    }
}