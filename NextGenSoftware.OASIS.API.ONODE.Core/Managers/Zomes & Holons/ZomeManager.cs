using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class ZomeManager : STARNETManagerBase<STARZome, DownloadedZome, InstalledZome>, IZomeManager
    {
        public ZomeManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId,
            OASISDNA,
            typeof(ZomeType),
            HolonType.STARZome,
            HolonType.InstalledZome,
            "Zome",
            "ZomeId",
            "ZomeName",
            "ZomeType",
            "zome",
            "oasis_zomes",
            "ZomeDNA.json",
            "ZomeDNAJSON")
        { }

        public ZomeManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            OASISDNA,
            typeof(ZomeType),
            HolonType.STARZome,
            HolonType.InstalledZome,
            "Zome",
            "ZomeId",
            "ZomeName",
            "ZomeType",
            "celestialbody",
            "oasis_celestialbodies",
            "ZomeDNA.json",
            "ZomeDNAJSON")
        { }

        public async Task<OASISResult<ISTARZome>> CreateZomeAsync(
            Guid avatarId,
            string name,
            string description,
            string fullPathToZomeSource,
            ZomeType zomeType,
            IZome zome,
            ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.CreateAsync(avatarId, name, description, zomeType, fullPathToZomeSource, null,
                new STARZome()
                {
                    ZomeType = zomeType,
                    Zome = zome
                },
            providerType));
        }

        public OASISResult<ISTARZome> CreateZome(
            Guid avatarId,
            string name,
            string description,
            string fullPathToZomeSource,
            ZomeType zomeType,
            IZome zome,
            ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Create(avatarId, name, description, zomeType, fullPathToZomeSource, null,
                new STARZome()
                {
                    ZomeType = zomeType,
                    Zome = zome
                },
            providerType));
        }

        public async Task<OASISResult<ISTARZome>> CreateZomeAsync(
            Guid avatarId,
            string name,
            string description,
            string fullPathToZomeSource,
            ZomeType zomeType,
            Guid zomeId,
            ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(await base.CreateAsync(avatarId, name, description, zomeType, fullPathToZomeSource, null,
                new STARZome()
                {
                    ZomeType = zomeType,
                    ZomeId = zomeId
                },
            providerType));
        }

        public OASISResult<ISTARZome> CreateZome(
            Guid avatarId,
            string name,
            string description,
            string fullPathToZomeSource,
            ZomeType zomeType,
            Guid zomeId,
            ProviderType providerType = ProviderType.Default)
        {
            return ProcessResult(base.Create(avatarId, name, description, zomeType, fullPathToZomeSource, null,
                new STARZome()
                {
                    ZomeType = zomeType,
                    ZomeId = zomeId
                },
            providerType));
        }

        private OASISResult<ISTARZome> ProcessResult(OASISResult<STARZome> operationResult)
        {
            OASISResult<ISTARZome> result = new OASISResult<ISTARZome>();
            result.Result = operationResult.Result;
            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
            return result;
        }
    }
}