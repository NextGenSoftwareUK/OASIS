using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Search
{
    public interface ISearchResults
    {
        int NumberOfResults { get; set; }
        int NumberOfDuplicates { get; set; }
        List<IAvatar> SearchResultAvatars { get; set; }
        List<IHolon> SearchResultHolons { get; set; }
    }
}