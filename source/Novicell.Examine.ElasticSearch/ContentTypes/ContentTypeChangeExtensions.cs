using System.Collections.Generic;
using Umbraco.Core.Models;
using Umbraco.Core.Services.Changes;

namespace Novicell.Examine.ElasticSearch.ContentTypes
{
    internal static class ContentTypeChangeExtensions
    {
        public static ContentTypeChange<TItem>.EventArgs ToEventArgs<TItem>(this IEnumerable<ContentTypeChange<TItem>> changes)
            where TItem : class, IContentTypeComposition
        {
            return new ContentTypeChange<TItem>.EventArgs(changes);
        }

        public static bool HasType(this ContentTypeChangeTypes change, ContentTypeChangeTypes type)
        {
            return (change & type) != ContentTypeChangeTypes.None;
        }

        public static bool HasTypesAll(this ContentTypeChangeTypes change, ContentTypeChangeTypes types)
        {
            return (change & types) == types;
        }

        public static bool HasTypesAny(this ContentTypeChangeTypes change, ContentTypeChangeTypes types)
        {
            return (change & types) != ContentTypeChangeTypes.None;
        }

        public static bool HasTypesNone(this ContentTypeChangeTypes change, ContentTypeChangeTypes types)
        {
            return (change & types) == ContentTypeChangeTypes.None;
        }
    }
}