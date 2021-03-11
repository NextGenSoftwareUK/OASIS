
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    public class SearchResults : Holon, ISearchResults
    {
        public List<string> SearchResultStrings { get; set; }
        public List<Holon> SearchResultHolons { get; set; }
    }
}
