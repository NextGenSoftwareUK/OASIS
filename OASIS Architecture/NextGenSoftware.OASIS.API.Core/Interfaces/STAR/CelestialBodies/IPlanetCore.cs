using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IPlanetCore : ICelestialBodyCore
    {
        IPlanet Planet { get; set; }

        OASISResult<IEnumerable<IMoon>> GetMoons(bool refresh = true, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = true, int version = 0, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<IMoon>>> GetMoonsAsync(bool refresh = true, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = true, int version = 0, ProviderType providerType = ProviderType.Default);
        OASISResult<IEnumerable<IMoon>> SaveMoons(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<IEnumerable<IMoon>>> SaveMoonsAsync(bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, ProviderType providerType = ProviderType.Default);
    }
}