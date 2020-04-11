
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core
{
    public class SearchResults : Holon, ISearchResults
    {
        public List<string> SearchResult { get; set; }
    }
}
