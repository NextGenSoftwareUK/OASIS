using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Objects.Search
{
    public class SearchParams : ISearchParams
    {
        public Guid AvatarId { get; set; }
        public bool SearchOnlyForCurrentAvatar { get; set; } = true;
        public List<ISearchGroupBase> SearchGroups { get; set; }
    }
}