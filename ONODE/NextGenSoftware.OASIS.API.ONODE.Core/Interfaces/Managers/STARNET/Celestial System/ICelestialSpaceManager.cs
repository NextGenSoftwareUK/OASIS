using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers
{
    public interface ICelestialSpaceManager : ISTARNETManagerBase<STARCelestialSpace, DownloadedCelestialSpace, InstalledCelestialSpace, CelestialSpaceDNA>
    {
        //OASISResult<ISTARCelestialSpace> CreateCelestialSpace(Guid avatarId, string name, string description, string fullPathToCelestialSpaceSource, CelestialSpaceType celestialSpaceType, Guid celestialSpaceId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
        //OASISResult<ISTARCelestialSpace> CreateCelestialSpace(Guid avatarId, string name, string description, string fullPathToCelestialSpaceSource, CelestialSpaceType celestialSpaceType, ICelestialSpace celestialSpace, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<ISTARCelestialSpace>> CreateCelestialSpaceAsync(Guid avatarId, string name, string description, string fullPathToCelestialSpaceSource, CelestialSpaceType celestialSpaceType, Guid celestialSpaceId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<ISTARCelestialSpace>> CreateCelestialSpaceAsync(Guid avatarId, string name, string description, string fullPathToCelestialSpaceSource, CelestialSpaceType celestialSpaceType, ICelestialSpace celestialSpace, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
    }
}