using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core
{
    public interface ISearchResults : IHolon
    {
        List<string> SearchResult { get; set; }
    }
}
