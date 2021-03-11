using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface ISearchResults : IHolon
    {
        List<string> SearchResultStrings { get; set; }
        List<Holon> SearchResultHolons { get; set; }
    }
}
