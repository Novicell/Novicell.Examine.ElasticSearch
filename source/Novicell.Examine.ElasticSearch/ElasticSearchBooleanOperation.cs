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

        public override IQuery And() => new ElasticSearchQuery(_search, BooleanOperation.And);
        public override IQuery Or() => new ElasticSearchQuery(_search, BooleanOperation.Or);

        /// <inheritdoc />
        public override IQuery Not() => new ElasticSearchQuery(_search, BooleanOperation.Not);

        protected override INestedQuery AndNested() => new ElasticSearchQuery(_search, BooleanOperation.And);

        protected override INestedQuery OrNested() => new ElasticSearchQuery(_search, BooleanOperation.Or);

        protected override INestedQuery NotNested() => new ElasticSearchQuery(_search, BooleanOperation.Not);

        public override ISearchResults Execute(int maxResults = 500) => _search.Execute(maxResults);

        #region IOrdering

        public override IOrdering OrderBy(params SortableField[] fields)
        {
            throw new NotImplementedException();
            //return _search.OrderBy(fields);
        }

        public override IOrdering OrderByDescending(params SortableField[] fields)
        {
            throw new NotImplementedException();
            //return _search.OrderByDescending(fields);
        }

        #endregion
    }
}