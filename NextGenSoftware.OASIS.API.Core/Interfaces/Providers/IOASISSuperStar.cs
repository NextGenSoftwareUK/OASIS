
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IOASISSuperStar : IOASISStorageProvider
    {
        bool NativeCodeGenesis(ICelestialBody celestialBody);
    }
}
