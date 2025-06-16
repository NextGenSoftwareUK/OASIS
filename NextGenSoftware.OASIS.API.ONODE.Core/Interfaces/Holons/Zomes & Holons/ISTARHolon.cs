using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public interface ISTARHolon
    {
        IHolon Holon { get; set; }
        Guid HolonId { get; set; }
        HolonType HolonType { get; set; }
    }
}