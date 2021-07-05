
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IOASISSuperStar : IOASISStorage
    {
        bool NativeCodeGenesis(ICelestialBody celestialBody);
    }
}
