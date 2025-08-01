using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers
{
    public interface ISTARHolonManager : ISTARNETManagerBase<STARHolon, DownloadedHolon, InstalledHolon, HolonDNA>
    {
        //OASISResult<ISTARHolon> CreateHolon(Guid avatarId, string name, string description, string fullPathToHolonSource, HolonType holonType, Guid holonId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
        //OASISResult<ISTARHolon> CreateHolon(Guid avatarId, string name, string description, string fullPathToHolonSource, HolonType holonType, IHolon holon, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<ISTARHolon>> CreateHolonAsync(Guid avatarId, string name, string description, string fullPathToHolonSource, HolonType holonType, Guid holonId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<ISTARHolon>> CreateHolonAsync(Guid avatarId, string name, string description, string fullPathToHolonSource, HolonType holonType, IHolon holon, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
    }
}