using System.Collections.Generic;
using System.Linq;
using Examine;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Examine;

namespace Novicell.Examine.ElasticSearch.Populators
{
    public class MemberIndexPopulator : IndexPopulator<MemberElasticSearchIndex>
    {
        private readonly IMemberService _memberService;
        private readonly IValueSetBuilder<IMember> _valueSetBuilder;

        public MemberIndexPopulator(IMemberService memberService, IValueSetBuilder<IMember> valueSetBuilder)
        {
            _memberService = memberService;
            _valueSetBuilder = valueSetBuilder;
        }
        protected override void PopulateIndexes(IReadOnlyList<IIndex> indexes)
        {
            if (indexes.Count == 0) return;

            const int pageSize = 1000;
            var pageIndex = 0;

            IMember[] members;

            //no node types specified, do all members
            do
            {
                members = _memberService.GetAll(pageIndex, pageSize, out _).ToArray();

                if (members.Length > 0)
                {
                    // ReSharper disable once PossibleMultipleEnumeration
                    foreach (var index in indexes)
                        index.IndexItems(_valueSetBuilder.GetValueSets(members));
                }

                pageIndex++;
            } while (members.Length == pageSize);
        }
    }
}