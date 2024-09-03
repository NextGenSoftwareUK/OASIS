using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONode.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons
{
    public interface IOAPP
    {
        ICelestialBody CelestialBody { get; set; }
        GenesisType GenesisType { get; set; }
        bool IsPublished { get; set; }
        OAPPType OAPPType { get; set; }
    }
}