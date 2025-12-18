
using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Objects.Search
{
    public interface ISearchParams
    {
        Guid AvatarId { get; set; }
        Guid ParentId { get; set; }
        /// <summary>
        /// When true and ParentId is set, providers should search recursively under the parent,
        /// including children, grandchildren, etc. When false, only direct children of ParentId
        /// should be considered (where supported).
        /// </summary>
        bool Recursive { get; set; }
        bool SearchOnlyForCurrentAvatar { get; set; }
        List<ISearchGroupBase> SearchGroups { get; set; }
    }
}