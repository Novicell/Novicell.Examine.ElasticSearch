using System;
using System.Collections.Generic;
using System.Linq;
using Examine;
using Examine.LuceneEngine.Indexing;
using Examine.LuceneEngine.Providers;
using Examine.LuceneEngine.Search;
using Examine.Search;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Nest;
using Novicell.Examine.ElasticSearch.Helpers;
using Novicell.Examine.ElasticSearch.Indexing;
using Novicell.Examine.ElasticSearch.Model;
using DateTimeType = Novicell.Examine.ElasticSearch.Indexing.DateTimeType;
using DoubleType = Novicell.Examine.ElasticSearch.Indexing.DoubleType;
using FullTextType = Novicell.Examine.ElasticSearch.Indexing.FullTextType;
using IIndexFieldValueType = Novicell.Examine.ElasticSearch.Indexing.IIndexFieldValueType;
using Int32Type = Novicell.Examine.ElasticSearch.Indexing.Int32Type;
using Int64Type = Novicell.Examine.ElasticSearch.Indexing.Int64Type;
using IQuery = Examine.Search.IQuery;
using KeywordAnalyzer = Lucene.Net.Analysis.KeywordAnalyzer;
using SingleType = Novicell.Examine.ElasticSearch.Indexing.SingleType;
using SortField = Lucene.Net.Search.SortField;
using StandardAnalyzer = Lucene.Net.Analysis.Standard.StandardAnalyzer;
using Version = Lucene.Net.Util.Version;

namespace Novicell.Examine.ElasticSearch.Queries
{
    public class ElasticSearchQuery : LuceneSearchQueryBase, IQueryExecutor, IQuery
    {
        public readonly ElasticSearchSearcher _searcher;
        public string _indexName;
        private readonly CustomMultiFieldQueryParser _queryParser;
        public new QueryParser QueryParser => _queryParser;

        internal readonly Stack<BooleanQuery> Queries = new Stack<BooleanQuery>();
        internal readonly List<SortField> SortFields = new List<SortField>();
        internal Analyzer DefaultAnalyzer { get; } = new StandardAnalyzer(Version.LUCENE_29);
        internal static readonly LuceneSearchOptions EmptyOptions = new LuceneSearchOptions();
        private const Version LuceneVersion = Version.LUCENE_30;
        public ElasticSearchQuery(ElasticSearchSearcher searcher, string category, string[] fields, BooleanOperation op,
            string indexName) : base(category, new StandardAnalyzer(Version.LUCENE_29), fields,
            EmptyOptions, op)
        {
            _searcher = searcher;
            _indexName = indexName;
            _queryParser = new CustomMultiFieldQueryParser(LuceneVersion, fields, new StandardAnalyzer(Version.LUCENE_29));
            _queryParser.AllowLeadingWildcard =true;
        }

        public ElasticSearchQuery(ElasticSearchQuery previous, BooleanOperation op)
            : base(previous.Category, previous.DefaultAnalyzer, previous._searcher.AllFields, EmptyOptions, op)
        {
            _searcher = previous._searcher;
            _indexName = previous._indexName;
        }

        public ISearchResults Execute(int maxResults = 500)
        {
            
            return new ElasticSearchSearchResults(_searcher._client.Value, Query, _indexName, SortFields, maxResults);
        }

        protected override LuceneBooleanOperationBase CreateOp()
        {
            return new ElasticSearchBooleanOperation(this);
        }

        public override IBooleanOperation Field<T>(string fieldName, T fieldValue)
            => RangeQueryInternal<T>(new[] {fieldName}, fieldValue, fieldValue);
        public IBooleanOperation Field(string fieldName, IExamineValue fieldValue)
            => FieldInternal(fieldName, fieldValue,Occurrence );
        public override IBooleanOperation ManagedQuery(string query, string[] fields = null)
        {
            //TODO: Instead of AllFields here we should have a reference to the FieldDefinitionCollection
            var fielddefintion =
                _searcher.AllProperties.Values.Where(x => x.Type == "text").Select(x => x.Name.Name);
            foreach (var field in fields ?? fielddefintion)
            {
                var fullTextQuery = FullTextType.GenerateQuery(field, query, DefaultAnalyzer);
                Query.Add(fullTextQuery,  Occur.SHOULD);
            }

            return new ElasticSearchBooleanOperation(this);
        }

       
        public override IBooleanOperation RangeQuery<T>(string[] fields, T? min1, T? max1, bool minInclusive = true,
            bool maxInclusive = true) => RangeQueryInternal(fields, min1, max1, minInclusive, maxInclusive);

        protected override INestedBooleanOperation FieldNested<T>(string fieldName, T fieldValue)
            => RangeQueryInternal<T>(new[] {fieldName}, fieldValue, fieldValue);

        protected override INestedBooleanOperation ManagedQueryNested(string query, string[] fields = null)
        {
            
            //TODO: Instead of AllFields here we should have a reference to the FieldDefinitionCollection
            var fielddefintion =
                _searcher.AllProperties.Values.Where(x => x.Type == "text").Select(x => x.Name.Name);
            foreach (var field in fields ?? fielddefintion)
            {
                var fullTextQuery = FullTextType.GenerateQuery(field, query, DefaultAnalyzer);
                Query.Add(fullTextQuery, Occurrence);
            }

            return new ElasticSearchBooleanOperation(this);
        }

        protected override INestedBooleanOperation RangeQueryNested<T>(string[] fields, T? min1, T? max1,
            bool minInclusive = true,
            bool maxInclusive = true)
            => RangeQueryInternal(fields, min1, max1, minInclusive, maxInclusive);

        private IIndexFieldValueType FromElasticType(IProperty property)
        {
            switch (property.Type.ToLowerInvariant())
            {
                case "date":
                    return new DateTimeType(property.Name.Name, DateTools.Resolution.MILLISECOND);
                case "double":
                    return new DoubleType(property.Name.Name);

                case "float":
                    return new SingleType(property.Name.Name);

                case "long":
                    return new Int64Type(property.Name.Name);
                case "integer":
                    return new Int32Type(property.Name.Name);
                default:
                    return new FullTextType(property.Name.Name, new StandardAnalyzer(Version.LUCENE_CURRENT));
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
                  
                    if (FromElasticType(valueType.Value) is IIndexRangeValueType type)
                    {
                        var q = ((Indexing.IIndexRangeValueType<T>)type).GetQuery(min, max, minInclusive, maxInclusive);
                        if (q != null)
                        {
                            //CriteriaContext.FieldQueries.Add(new KeyValuePair<IIndexFieldValueType, Query>(type, q));
                            inner.Add(q, Occur.SHOULD);
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Could not perform a range query on the field {valueType.Key.Name}, it's value type is {valueType.Value.Type}");
                    }
                }

                outer.Add(inner, Occur.SHOULD);

                return outer;
            }), Occurrence);


            return new ElasticSearchBooleanOperation(this);
        }
        public new IBooleanOperation GroupedAnd(IEnumerable<string> fields, params string[] query)
            => this.GroupedAndInternal(fields.ToArray(), query.Select(f => new ExamineValue(Examineness.Explicit, f)).Cast<IExamineValue>().ToArray(), Occurrence);

        public new IBooleanOperation GroupedAnd(IEnumerable<string> fields, params IExamineValue[] query)
            => this.GroupedAndInternal(fields.ToArray(), query, Occurrence);

        public new IBooleanOperation GroupedOr(IEnumerable<string> fields, params string[] query)
            => this.GroupedOrInternal(fields.ToArray(), query.Select(f => new ExamineValue(Examineness.Explicit, f)).Cast<IExamineValue>().ToArray(), Occurrence);

        public new IBooleanOperation GroupedOr(IEnumerable<string> fields, params IExamineValue[] query)
            => this.GroupedOrInternal(fields.ToArray(), query, Occurrence);

        public new IBooleanOperation GroupedNot(IEnumerable<string> fields, params string[] query)
            => this.GroupedNotInternal(fields.ToArray(), query.Select(f => new ExamineValue(Examineness.Explicit, f)).Cast<IExamineValue>().ToArray());

        public new IBooleanOperation GroupedNot(IEnumerable<string> fields, params IExamineValue[] query)
            => this.GroupedNotInternal(fields.ToArray(), query);
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
        
        #region examineprivateorinternalmethods
        
        protected internal new LuceneBooleanOperationBase IdInternal(
            string id,
            Occur occurrence)
        {
            if (id == null)
                throw new ArgumentNullException(nameof (id));
            this.Query.Add(this._queryParser.GetFieldQueryInternal("__NodeId", id),occurrence);
            return this.CreateOp();
        }
        public ElasticSearchBooleanOperation ManagedQueryInternal(string query, string[] fields = null)
        {
            Query.Add(new LateBoundQuery(() =>
            {
                //if no fields are specified then use all fields
                fields = fields ?? AllFields;


                //Strangely we need an inner and outer query. If we don't do this then the lucene syntax returned is incorrect 
                //since it doesn't wrap in parenthesis properly. I'm unsure if this is a lucene issue (assume so) since that is what
                //is producing the resulting lucene string syntax. It might not be needed internally within Lucene since it's an object
                //so it might be the ToString() that is the issue.
                var outer = new BooleanQuery();
                var inner = new BooleanQuery();
                var fielddefintion =
                    _searcher.AllProperties.Values.Where(x => x.Type == "text").Select(x => x.Name.Name);
                foreach (var field in fielddefintion)
                {
                    var q =   FullTextType.GenerateQuery(field, query, DefaultAnalyzer);
                    if (q != null)
                    {
                        //CriteriaContext.ManagedQueries.Add(new KeyValuePair<IIndexFieldValueType, Query>(type, q));
                        inner.Add(q, Occur.SHOULD);
                    }

                }

                outer.Add(inner, Occur.SHOULD);

                return outer;
            }), Occurrence);


            return new ElasticSearchBooleanOperation(this);
        }

        protected internal new LuceneBooleanOperationBase FieldInternal(string fieldName, IExamineValue fieldValue, Occur occurrence)
        {
            if (fieldName == null) throw new ArgumentNullException(nameof(fieldName));
            if (fieldValue == null) throw new ArgumentNullException(nameof(fieldValue));
            return FieldInternal(fieldName, fieldValue, occurrence, true);
        }

        private LuceneBooleanOperationBase FieldInternal(string fieldName, IExamineValue fieldValue, Occur occurrence, bool useQueryParser)
        {
            Query queryToAdd = GetFieldInternalQuery(fieldName, fieldValue, useQueryParser);

            if (queryToAdd != null)
                Query.Add(queryToAdd, occurrence);

            return CreateOp();
        }

        protected internal new LuceneBooleanOperationBase GroupedAndInternal(string[] fields, IExamineValue[] fieldVals, Occur occurrence)
        {
            if (fields == null) throw new ArgumentNullException(nameof(fields));
            if (fieldVals == null) throw new ArgumentNullException(nameof(fieldVals));

            //if there's only 1 query text we want to build up a string like this:
            //(+field1:query +field2:query +field3:query)
            //but Lucene will bork if you provide an array of length 1 (which is != to the field length)

            Query.Add(GetMultiFieldQuery(fields, fieldVals, Occur.MUST), occurrence);

            return CreateOp();
        }

        protected internal new LuceneBooleanOperationBase GroupedNotInternal(string[] fields, IExamineValue[] fieldVals)
        {
            if (fields == null) throw new ArgumentNullException(nameof(fields));
            if (fieldVals == null) throw new ArgumentNullException(nameof(fieldVals));

            //if there's only 1 query text we want to build up a string like this:
            //(!field1:query !field2:query !field3:query)
            //but Lucene will bork if you provide an array of length 1 (which is != to the field length)

            Query.Add(GetMultiFieldQuery(fields, fieldVals, Occur.MUST_NOT, true),
                //NOTE: This is important because we cannot prefix a + to a group of NOT's, that doesn't work. 
                // for example, it cannot be:  +(-id:1 -id:2 -id:3)
                // it just needs to be          (-id:1 -id:2 -id:3)
                Occur.SHOULD);

            return CreateOp();
        }

        protected internal new LuceneBooleanOperationBase GroupedOrInternal(string[] fields, IExamineValue[] fieldVals, Occur occurrence)
        {
            if (fields == null) throw new ArgumentNullException(nameof(fields));
            if (fieldVals == null) throw new ArgumentNullException(nameof(fieldVals));

            //if there's only 1 query text we want to build up a string like this:
            //(field1:query field2:query field3:query)
            //but Lucene will bork if you provide an array of length 1 (which is != to the field length)

            Query.Add(GetMultiFieldQuery(fields, fieldVals, Occur.SHOULD, true), occurrence);

            return CreateOp();
        }
        
        private BooleanQuery GetMultiFieldQuery(
            IReadOnlyList<string> fields,
            IExamineValue[] fieldVals,
            Occur occurrence,
            bool matchAllCombinations = false)
        {

            var qry = new BooleanQuery();
            if (matchAllCombinations)
            {
                foreach (var f in fields)
                {
                    foreach (var val in fieldVals)
                    {
                        var q = GetFieldInternalQuery(f, val, true);
                        if (q != null)
                        {
                            qry.Add(q, occurrence);
                        }
                    }
                }
            }
            else
            {
                var queryVals = new IExamineValue[fields.Count];
                if (fieldVals.Length == 1)
                {
                    for (int i = 0; i < queryVals.Length; i++)
                        queryVals[i] = fieldVals[0];
                }
                else
                {
                    queryVals = fieldVals;
                }

                for (int i = 0; i < fields.Count; i++)
                {
                    var q = GetFieldInternalQuery(fields[i], queryVals[i], true);
                    if (q != null)
                    {
                        qry.Add(q, occurrence);
                    }
                }
            }

            return qry;
        }
         private Query GetFieldInternalQuery(string fieldName, IExamineValue fieldValue, bool useQueryParser)
        {
            Query queryToAdd;

            switch (fieldValue.Examineness)
            {
                case Examineness.Fuzzy:
                    if (useQueryParser)
                    {
                        queryToAdd = _queryParser.GetFuzzyQueryInternal(fieldName, fieldValue.Value.Replace("-","\\-"), fieldValue.Level);
                    }
                    else
                    {
                        //REFERENCE: http://lucene.apache.org/java/2_4_0/queryparsersyntax.html#Fuzzy%20Searches
                        var proxQuery = fieldName + ":" + fieldValue.Value.Replace("-","\\-") + "~" + Convert.ToInt32(fieldValue.Level);
                        queryToAdd = ParseRawQuery(proxQuery);
                    }
                    break;
                case Examineness.SimpleWildcard:
                case Examineness.ComplexWildcard:
                    if (useQueryParser)
                    {
                        queryToAdd = _queryParser.GetWildcardQueryInternal(fieldName, fieldValue.Value.Replace("-","\\-"));
                    }
                    else
                    {
                        //this will already have a * or a . suffixed based on the extension methods
                        //REFERENCE: http://lucene.apache.org/java/2_4_0/queryparsersyntax.html#Wildcard%20Searches
                        var proxQuery = fieldName + ":" + fieldValue.Value.Replace("-","\\-");
                        queryToAdd = ParseRawQuery(proxQuery);
                    }
                    break;
                case Examineness.Boosted:
                    if (useQueryParser)
                    {
                        queryToAdd = _queryParser.GetFieldQueryInternal(fieldName, fieldValue.Value.Replace("-","\\-"));
                        queryToAdd.Boost = fieldValue.Level;
                    }
                    else
                    {
                        //REFERENCE: http://lucene.apache.org/java/2_4_0/queryparsersyntax.html#Boosting%20a%20Term
                        var proxQuery = fieldName + ":\"" + fieldValue.Value.Replace("-","\\-") + "\"^" + Convert.ToInt32(fieldValue.Level).ToString();
                        queryToAdd = ParseRawQuery(proxQuery);
                    }
                    break;
                case Examineness.Proximity:

                    //This is how you are supposed to do this based on this doc here:
                    //http://lucene.apache.org/java/2_4_1/api/org/apache/lucene/search/spans/package-summary.html#package_description
                    //but i think that lucene.net has an issue with it's internal parser since it parses to a very strange query
                    //we'll just manually make it instead below

                    //var spans = new List<SpanQuery>();
                    //foreach (var s in fieldValue.Value.Split(' '))
                    //{
                    //    spans.Add(new SpanTermQuery(new Term(fieldName, s)));
                    //}
                    //queryToAdd = new SpanNearQuery(spans.ToArray(), Convert.ToInt32(fieldValue.Level), true);

                    var qry = fieldName + ":\"" + fieldValue.Value.Replace("-","\\-") + "\"~" + Convert.ToInt32(fieldValue.Level);
                    if (useQueryParser)
                    {
                        queryToAdd = _queryParser.Parse(qry);
                    }
                    else
                    {
                        queryToAdd = ParseRawQuery(qry);
                    }
                    break;
                case Examineness.Escaped:

                    //This uses the KeywordAnalyzer to parse the 'phrase'
                    var stdQuery = fieldName + ":" + fieldValue.Value.Replace("-","\\-");

                    //NOTE: We used to just use this but it's more accurate/exact with the below usage of phrase query
                    //queryToAdd = ParseRawQuery(stdQuery);

                    //This uses the PhraseQuery to parse the phrase, the results seem identical
                    queryToAdd = ParseRawQuery(fieldName, fieldValue.Value.Replace("-","\\-"));

                    break;
                case Examineness.Explicit:
                default:
                    if (useQueryParser)
                    {
                        queryToAdd = _queryParser.GetFieldQueryInternal(fieldName, fieldValue.Value.Replace("-","\\-"));
                    }
                    else
                    {
                        //standard query 
                        var proxQuery = fieldName + ":" + fieldValue.Value.Replace("-","\\-");
                        queryToAdd = ParseRawQuery(proxQuery);
                    }
                    break;
            }
            return queryToAdd;
        }
         private Query ParseRawQuery(string rawQuery)
         {
             var parser = new QueryParser(LuceneVersion, "", new KeywordAnalyzer());
             return parser.Parse(rawQuery);
         }

        
         private static Query ParseRawQuery(string field, string txt)
         {
             var phraseQuery = new PhraseQuery { Slop = 0 };
             foreach (var val in txt.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
             {
                 phraseQuery.Add(new Term(field, val));
             }
             return phraseQuery;
         }
        #endregion
    }
}