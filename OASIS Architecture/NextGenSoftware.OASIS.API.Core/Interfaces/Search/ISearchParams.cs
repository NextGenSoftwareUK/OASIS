
using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Objects.Search
{
    public interface ISearchParams
    {
        Guid AvatarId { get; set; }
        /// <summary>
        /// When true and ParentId is set, providers should search recursively under the parent,
        /// including children, grandchildren, etc. When false, only direct children of ParentId
        /// should be considered (where supported).
        /// </summary>
        Guid ParentId { get; set; }
        Dictionary<string, string> FilterByMetaData { get; set; }
        public MetaKeyValuePairMatchMode MetaKeyValuePairMatchMode { get; set; }
        bool Recursive { get; set; }
        bool SearchOnlyForCurrentAvatar { get; set; }
        List<ISearchGroupBase> SearchGroups { get; set; }
    }
}