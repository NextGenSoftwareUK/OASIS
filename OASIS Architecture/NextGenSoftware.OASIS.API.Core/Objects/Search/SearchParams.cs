using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Objects.Search
{
    public class SearchParams : ISearchParams
    {
        public Guid AvatarId { get; set; }
        public Guid ParentId { get; set; }
        public Dictionary<string, string> FilterByMetaData { get; set; }
        public MetaKeyValuePairMatchMode MetaKeyValuePairMatchMode { get; set; }
        /// <summary>
        /// When true and ParentId is set, providers should search recursively under the parent,
        /// including children, grandchildren, etc. When false, only direct children of ParentId
        /// should be considered (where supported).
        /// </summary>
        public bool Recursive { get; set; } = true;
        public bool SearchOnlyForCurrentAvatar { get; set; } = true;
        public List<ISearchGroupBase> SearchGroups { get; set; }
    }
}