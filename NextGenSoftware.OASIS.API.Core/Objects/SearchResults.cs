using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class SearchResults : ISearchResults
    {
        public List<string> SearchResultStrings { get; set; }
        public List<Holon> SearchResultHolons { get; set; }
    }
}
