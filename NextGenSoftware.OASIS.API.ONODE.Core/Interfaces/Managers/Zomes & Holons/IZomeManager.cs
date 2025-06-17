using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public interface IZomeManager
    {
        OASISResult<ISTARZome> CreateZome(Guid avatarId, string name, string description, string fullPathToZomeSource, ZomeType zomeType, Guid zomeId, ProviderType providerType = ProviderType.Default);
        OASISResult<ISTARZome> CreateZome(Guid avatarId, string name, string description, string fullPathToZomeSource, ZomeType zomeType, IZome zome, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<ISTARZome>> CreateZomeAsync(Guid avatarId, string name, string description, string fullPathToZomeSource, ZomeType zomeType, Guid zomeId, ProviderType providerType = ProviderType.Default);
        Task<OASISResult<ISTARZome>> CreateZomeAsync(Guid avatarId, string name, string description, string fullPathToZomeSource, ZomeType zomeType, IZome zome, ProviderType providerType = ProviderType.Default);
    }
}