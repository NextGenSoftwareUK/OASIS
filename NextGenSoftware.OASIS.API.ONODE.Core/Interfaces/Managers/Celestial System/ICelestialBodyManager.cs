using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers
{
    public interface ICelestialBodyManager
    {
        OASISResult<ISTARCelestialBody> CreateCelestialBody(Guid avatarId, string name, string description, string fullPathToCelestialBodySource, CelestialBodyType celestialBodyType, Guid celestialBodyId, ProviderType providerType = ProviderType.Default);
        OASISResult<ISTARCelestialBody> CreateCelestialBody(Guid avatarId, string name, string description, string fullPathToCelestialBodySource, CelestialBodyType celestialBodyType, ICelestialBody celestialBody, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<ISTARCelestialBody>> CreateCelestialBodyAsync(Guid avatarId, string name, string description, string fullPathToCelestialBodySource, CelestialBodyType celestialBodyType, Guid celestialBodyId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<ISTARCelestialBody>> CreateCelestialBodyAsync(Guid avatarId, string name, string description, string fullPathToCelestialBodySource, CelestialBodyType celestialBodyType, ICelestialBody celestialBody, ProviderType providerType = ProviderType.Default);
    }
}