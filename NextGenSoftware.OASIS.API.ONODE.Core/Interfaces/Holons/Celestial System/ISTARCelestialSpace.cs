using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public interface ISTARCelestialSpace
    {
        ICelestialSpace CelestialSpace { get; set; }
        Guid CelestialSpaceId { get; set; }
        CelestialSpaceType CelestialSpaceType { get; set; }
    }
}