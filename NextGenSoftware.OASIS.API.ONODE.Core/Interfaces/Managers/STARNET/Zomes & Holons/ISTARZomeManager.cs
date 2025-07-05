using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers
{
    public interface ISTARZomeManager : ISTARNETManagerBase<STARZome, DownloadedZomeMetaDataDNA, InstalledZome>
    {
        //OASISResult<ISTARZome> CreateZome(Guid avatarId, string name, string description, string fullPathToZomeSource, ZomeType zomeType, Guid zomeId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
        //OASISResult<ISTARZome> CreateZome(Guid avatarId, string name, string description, string fullPathToZomeSource, ZomeType zomeType, IZome zome, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<ISTARZome>> CreateZomeAsync(Guid avatarId, string name, string description, string fullPathToZomeSource, ZomeType zomeType, Guid zomeId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<ISTARZome>> CreateZomeAsync(Guid avatarId, string name, string description, string fullPathToZomeSource, ZomeType zomeType, IZome zome, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
    }
}