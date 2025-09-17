using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface ISampleHolon : IHolon
    {
        Guid AvatarId { get; set; }
        DateTime CustomDate { get; set; }
        long CustomLongNumber { get; set; }
        int CustomNumber { get; set; }
        string CustomProperty { get; set; }
        string CustomProperty2 { get; set; }
    }
}