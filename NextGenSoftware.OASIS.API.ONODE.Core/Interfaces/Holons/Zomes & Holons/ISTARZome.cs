using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public interface ISTARZome
    {
        IZome Zome { get; set; }
        Guid ZomeId { get; set; }
        ZomeType ZomeType { get; set; }
    }
}