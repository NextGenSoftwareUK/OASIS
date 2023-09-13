using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Objects.Search
{
    public class SearchParams : ISearchParams
    {
        public List<ISearchGroupBase> SearchGroups { get; set; }
    }
}