
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core
{
    public interface ISearchResults : IHolon
    {
        List<string> SearchResultStrings { get; set; }
        List<Holon> SearchResultHolons { get; set; }
    }
}
