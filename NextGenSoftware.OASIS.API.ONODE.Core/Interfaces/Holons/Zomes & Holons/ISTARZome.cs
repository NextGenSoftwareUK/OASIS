using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public interface ISTARZome : ISTARNETHolon
    {
        IZome Zome { get; set; }
        Guid ZomeId { get; set; }
        ZomeType ZomeType { get; set; }
    }
}