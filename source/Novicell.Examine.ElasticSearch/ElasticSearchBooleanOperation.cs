using System;
using Examine;
using Examine.LuceneEngine.Search;
using Examine.Search;
using Lucene.Net.Search;
using Novicell.Examine.ElasticSearch.Queries;

namespace Novicell.Examine.ElasticSearch
{
    public class ElasticSearchBooleanOperation : LuceneBooleanOperationBase
    {
        private readonly ElasticSearchQuery _search;
        internal ElasticSearchBooleanOperation(ElasticSearchQuery search)
            : base(search)
        {
           
            _search = search;
        }

        #region IBooleanOperation Members
        protected override INestedQuery AndNested() => new ElasticQuery(this._search, Occur.MUST);

        /// <inheritdoc />
        protected override INestedQuery OrNested() => new ElasticQuery(this._search, Occur.SHOULD);

        /// <inheritdoc />
        protected override INestedQuery NotNested() => new ElasticQuery(this._search, Occur.MUST_NOT);

        /// <inheritdoc />
        public override IQuery And() => new ElasticQuery(this._search, Occur.MUST);


        /// <inheritdoc />
        public override IQuery Or() => new ElasticQuery(this._search, Occur.SHOULD);


        /// <inheritdoc />
        public override IQuery Not() => new ElasticQuery(this._search, Occur.MUST_NOT);

        public override ISearchResults Execute(int maxResults = 500) => _search.Execute(maxResults);
        #endregion
        
        #region IOrdering

        public override IOrdering OrderBy(params SortableField[] fields) => _search.OrderBy(fields);

        public override IOrdering OrderByDescending(params SortableField[] fields) => _search.OrderByDescending(fields);
        
        #endregion
        public override string ToString() => _search.ToString();
        protected internal LuceneBooleanOperationBase Op(
            Func<INestedQuery, INestedBooleanOperation> inner,
            BooleanOperation outerOp,
            BooleanOperation? defaultInnerOp = null)
        {
            this._search.Queries.Push(new BooleanQuery());
            BooleanOperation booleanOperation1 = this._search.BooleanOperation;
            if (defaultInnerOp.HasValue)
                this._search.BooleanOperation = defaultInnerOp.Value;
            INestedBooleanOperation booleanOperation2 = inner((INestedQuery) this._search);
            if (defaultInnerOp.HasValue)
                this._search.BooleanOperation = booleanOperation1;
            return this._search.LuceneQuery((Query) this._search.Queries.Pop(), new BooleanOperation?(outerOp));
        }
      
    }
}