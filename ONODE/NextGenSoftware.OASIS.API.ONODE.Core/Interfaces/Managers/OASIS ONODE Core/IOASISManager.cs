using System;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers
{
    public interface IOASISManager
    {
        Guid AvatarId { get; set; }
        HolonManager Data { get; set; }
        OASISDNA OASISDNA { get; set; }
    }
}