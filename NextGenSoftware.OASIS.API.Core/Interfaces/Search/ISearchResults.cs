using NextGenSoftware.OASIS.API.Core.Holons;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Search
{
    public interface ISearchResults
    {
        bool NumberOfResults { get; }
       // List<string> SearchResultStrings { get; set; }
        List<IAvatar> SearchResultAvatars { get; set; }
        List<IHolon> SearchResultHolons { get; set; }
    }
}