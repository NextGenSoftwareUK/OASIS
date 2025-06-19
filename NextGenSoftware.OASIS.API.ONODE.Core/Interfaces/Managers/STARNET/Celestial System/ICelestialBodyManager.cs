using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers
{
    public interface ICelestialBodyManager : ISTARNETManagerBase<STARCelestialBody, DownloadedCelestialBody, InstalledCelestialBody>
    {
        OASISResult<ISTARCelestialBody> CreateCelestialBody(Guid avatarId, string name, string description, string fullPathToCelestialBodySource, CelestialBodyType celestialBodyType, Guid celestialBodyId, ProviderType providerType = ProviderType.Default);
        OASISResult<ISTARCelestialBody> CreateCelestialBody(Guid avatarId, string name, string description, string fullPathToCelestialBodySource, CelestialBodyType celestialBodyType, ICelestialBody celestialBody, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<ISTARCelestialBody>> CreateCelestialBodyAsync(Guid avatarId, string name, string description, string fullPathToCelestialBodySource, CelestialBodyType celestialBodyType, Guid celestialBodyId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<ISTARCelestialBody>> CreateCelestialBodyAsync(Guid avatarId, string name, string description, string fullPathToCelestialBodySource, CelestialBodyType celestialBodyType, ICelestialBody celestialBody, ProviderType providerType = ProviderType.Default);
    }
}