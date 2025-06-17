using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public interface ISTARHolonManager
    {
        OASISResult<ISTARHolon> CreateHolon(Guid avatarId, string name, string description, string fullPathToHolonSource, HolonType holonType, Guid holonId, ProviderType providerType = ProviderType.Default);
        OASISResult<ISTARHolon> CreateHolon(Guid avatarId, string name, string description, string fullPathToHolonSource, HolonType holonType, IHolon holon, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<ISTARHolon>> CreateHolonAsync(Guid avatarId, string name, string description, string fullPathToHolonSource, HolonType holonType, Guid holonId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<ISTARHolon>> CreateHolonAsync(Guid avatarId, string name, string description, string fullPathToHolonSource, HolonType holonType, IHolon holon, ProviderType providerType = ProviderType.Default);
    }
}