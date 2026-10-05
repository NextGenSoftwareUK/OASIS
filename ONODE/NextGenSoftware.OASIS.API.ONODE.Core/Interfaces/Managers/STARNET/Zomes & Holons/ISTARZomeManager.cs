using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers
{
    public interface ISTARZomeManager : ISTARNETManagerBase<STARZome, DownloadedZomeMetaDataDNA, InstalledZome, ZomeDNA>
    {
        //OASISResult<ISTARZome> CreateZome(Guid avatarId, string name, string description, string fullPathToZomeSource, ZomeType zomeType, Guid zomeId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
        //OASISResult<ISTARZome> CreateZome(Guid avatarId, string name, string description, string fullPathToZomeSource, ZomeType zomeType, IZome zome, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<ISTARZome>> CreateZomeAsync(Guid avatarId, string name, string description, string fullPathToZomeSource, ZomeType zomeType, Guid zomeId, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
        //Task<OASISResult<ISTARZome>> CreateZomeAsync(Guid avatarId, string name, string description, string fullPathToZomeSource, ZomeType zomeType, IZome zome, bool checkIfSourcePathExists = true, ProviderType providerType = ProviderType.Default);
    }
}