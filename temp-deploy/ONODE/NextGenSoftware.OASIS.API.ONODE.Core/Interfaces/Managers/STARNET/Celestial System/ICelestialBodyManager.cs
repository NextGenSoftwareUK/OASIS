using System;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers
{
    //public interface ICelestialBodyManager : ISTARNETManagerBase<STARCelestialBody, DownloadedCelestialBody, InstalledCelestialBody, CelestialBodyDNA>
    public interface ICelestialBodyManager : ISTARNETManagerBase<STARCelestialBody, DownloadedCelestialBody, InstalledCelestialBody, STARNETDNA>
    {
        //OASISResult<ISTARCelestialBody> CreateCelestialBody(Guid avatarId, string name, string description, string fullPathToCelestialBodySource, CelestialBodyType celestialBodyType, Guid celestialBodyId, bool checkIfSourcePathExists, ProviderType providerType = ProviderType.Default);
        //OASISResult<ISTARCelestialBody> CreateCelestialBody(Guid avatarId, string name, string description, string fullPathToCelestialBodySource, CelestialBodyType celestialBodyType, ICelestialBody celestialBody, bool checkIfSourcePathExists, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<ISTARCelestialBody>> CreateCelestialBodyAsync(Guid avatarId, string name, string description, string fullPathToCelestialBodySource, CelestialBodyType celestialBodyType, Guid celestialBodyId, bool checkIfSourcePathExists, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<ISTARCelestialBody>> CreateCelestialBodyAsync(Guid avatarId, string name, string description, string fullPathToCelestialBodySource, CelestialBodyType celestialBodyType, ICelestialBody celestialBody, bool checkIfSourcePathExists, ProviderType providerType = ProviderType.Default);
    }
}