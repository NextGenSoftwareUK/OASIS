
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core
{
    public class SearchResults : Holon, ISearchResults
    {
        public List<string> SearchResultStrings { get; set; }
        public List<Holon> SearchResultHolons { get; set; }
    }
}
