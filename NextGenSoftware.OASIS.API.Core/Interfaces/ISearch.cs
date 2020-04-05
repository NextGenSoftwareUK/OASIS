using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core
{
    public interface ISearch : IHolon
    {
        string SearchQuery { get; set; }
    }
}
