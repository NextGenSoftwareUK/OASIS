using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers
{
    public interface ISTARHolonManager : ISTARNETManagerBase<STARHolon, DownloadedHolon, InstalledHolon>
    {
        //OASISResult<ISTARHolon> CreateHolon(Guid avatarId, string name, string description, string fullPathToHolonSource, HolonType holonType, Guid holonId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
        //OASISResult<ISTARHolon> CreateHolon(Guid avatarId, string name, string description, string fullPathToHolonSource, HolonType holonType, IHolon holon, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<ISTARHolon>> CreateHolonAsync(Guid avatarId, string name, string description, string fullPathToHolonSource, HolonType holonType, Guid holonId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<ISTARHolon>> CreateHolonAsync(Guid avatarId, string name, string description, string fullPathToHolonSource, HolonType holonType, IHolon holon, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
    }
}