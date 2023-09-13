
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Objects.Search
{
    public interface ISearchParams
    {
        List<ISearchGroupBase> SearchGroups { get; set; }
    }
}