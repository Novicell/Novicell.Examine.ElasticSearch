using System;
using System.Collections.Generic;
using System.Linq;
using Examine;
using Umbraco.Core.Logging;
using Umbraco.Examine;

namespace Novicell.Examine.ElasticSearch.Umbraco.Indexers
{
    public class ContentElasticSearchIndex : ElasticSearchUmbracoIndex, IUmbracoContentIndex2
    {
        public ContentElasticSearchIndex(string name, ElasticSearchConfig connectionConfiguration, IProfilingLogger profilingLogger,  
            FieldDefinitionCollection fieldDefinitions = null, string analyzer = null,
            IValueSetValidator validator = null, bool publishedValuesOnly = false) : base(name, connectionConfiguration,
            profilingLogger, fieldDefinitions, analyzer, validator)
        {
            PublishedValuesOnly = publishedValuesOnly;
        }
        protected override void PerformIndexItems(IEnumerable<ValueSet> values, Action<IndexOperationEventArgs> onComplete)
        {
            //We don't want to re-enumerate this list, but we need to split it into 2x enumerables: invalid and valid items.
            // The Invalid items will be deleted, these are items that have invalid paths (i.e. moved to the recycle bin, etc...)
            // Then we'll index the Value group all together.
            // We return 0 or 1 here so we can order the results and do the invalid first and then the valid.
            var invalidOrValid = values.GroupBy(v =>
            {
                if (!v.Values.TryGetValue("path", out var paths) || paths.Count <= 0 || paths[0] == null)
                    return 0;

                //we know this is an IContentValueSetValidator
                var validator = (IContentValueSetValidator)ValueSetValidator;
                var path = paths[0].ToString();

                return (!validator.ValidatePath(path, v.Category)
                        || !validator.ValidateRecycleBin(path, v.Category)
                        || !validator.ValidateProtectedContent(path, v.Category))
                    ? 0
                    : 1;
            });

            var hasDeletes = false;
            var hasUpdates = false;
            foreach (var group in invalidOrValid.OrderBy(x => x.Key))
            {
                if (group.Key == 0)
                {
                    hasDeletes = true;
                    //these are the invalid items so we'll delete them
                    //since the path is not valid we need to delete this item in case it exists in the index already and has now
                    //been moved to an invalid parent.

                    base.PerformDeleteFromIndex(group.Select(x => x.Id), args => { /*noop*/ });
                }
                else
                {
                    hasUpdates = true;
                    //these are the valid ones, so just index them all at once
                    base.PerformIndexItems(group, onComplete);
                }
            }

            if (hasDeletes && !hasUpdates || !hasDeletes && !hasUpdates)
            {
                //we need to manually call the completed method
                onComplete(new IndexOperationEventArgs(this, 0));
            }
        }
    }
}