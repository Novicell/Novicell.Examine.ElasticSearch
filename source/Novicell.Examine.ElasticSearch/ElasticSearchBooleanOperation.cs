using System;
using Examine;
using Examine.LuceneEngine.Search;
using Examine.Search;
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
        public override IQuery And() => new ElasticSearchQuery(_search, BooleanOperation.And);
        public override IQuery Or() => new ElasticSearchQuery(_search, BooleanOperation.Or);

        /// <inheritdoc />
        public override IQuery Not() => new ElasticSearchQuery(_search, BooleanOperation.Not);

        protected override INestedQuery AndNested() => new ElasticSearchQuery(_search, BooleanOperation.And);

        protected override INestedQuery OrNested() => new ElasticSearchQuery(_search, BooleanOperation.Or);

        protected override INestedQuery NotNested() => new ElasticSearchQuery(_search, BooleanOperation.Not);

        public override ISearchResults Execute(int maxResults = 500) => _search.Execute(maxResults);
        #endregion
        
        #region IOrdering

        public override IOrdering OrderBy(params SortableField[] fields) => _search.OrderBy(fields);

        public override IOrdering OrderByDescending(params SortableField[] fields) => _search.OrderByDescending(fields);
        
        #endregion
        public override string ToString() => _search.ToString();
      
    }
}