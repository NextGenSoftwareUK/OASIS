using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;

namespace NextGenSoftware.OASIS.API.Core.Objects.Search
{
    public class SearchResults : ISearchResults
    {
        public int NumberOfResults { get; set; }
        public int NumberOfDuplicates { get; set; }
        //public List<string> SearchResultStrings { get; set; }
        public List<IHolon> SearchResultHolons { get; set; } = new List<IHolon>();
        public List<IAvatar> SearchResultAvatars { get; set; } = new List<IAvatar>();
    }
}
