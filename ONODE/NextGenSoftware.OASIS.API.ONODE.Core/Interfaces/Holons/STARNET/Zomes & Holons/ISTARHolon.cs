using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface ISTARHolon : ISTARNETHolon
    {
        IHolon Holon { get; set; }
        Guid HolonId { get; set; }
        HolonType HolonType { get; set; }
    }
}