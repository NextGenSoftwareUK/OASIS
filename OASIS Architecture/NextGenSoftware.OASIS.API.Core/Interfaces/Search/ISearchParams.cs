
using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Objects.Search
{
    public interface ISearchParams
    {
        Guid AvatarId { get; set; }
        bool SearchOnlyForCurrentAvatar { get; set; }
        List<ISearchGroupBase> SearchGroups { get; set; }
    }
}