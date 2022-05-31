
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class SearchParams : ISearchParams
    {
        public string SearchQuery { get; set; }
        public bool SearchAllProviders { get; set; }
        public bool SearchAvatarsOnly { get; set; }
    }
}
