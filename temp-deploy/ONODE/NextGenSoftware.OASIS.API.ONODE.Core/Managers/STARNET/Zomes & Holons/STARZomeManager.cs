using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    //public class STARZomeManager : STARNETManagerBase<STARZome, DownloadedZome, InstalledZome, ZomeDNA>//, ISTARZomeManager
    public class STARZomeManager : STARNETManagerBase<STARZome, DownloadedZome, InstalledZome, STARNETDNA>//, ISTARZomeManager
    {
        public STARZomeManager(Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(avatarId,
            STARDNA,
            OASISDNA,
            typeof(ZomeType),
            HolonType.STARZome,
            HolonType.InstalledZome,
            "Zome",
            //"ZomeId",
            "STARNETHolonId",
            "ZomeName",
            "ZomeType",
            "ozome",
            "oasis_zomes",
            "ZomeDNA.json",
            "STARZomeDNAJSON")
        { }

        public STARZomeManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            STARDNA,
            OASISDNA,
            typeof(ZomeType),
            HolonType.STARZome,
            HolonType.InstalledZome,
            "Zome",
            //"ZomeId",
            "STARNETHolonId",
            "ZomeName",
            "ZomeType",
            "ozome",
            "oasis_zomes",
            "ZomeDNA.json",
            "STARZomeDNAJSON")
        { }

        //public async Task<OASISResult<ISTARZome>> CreateZomeAsync(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToZomeSource,
        //    ZomeType zomeType,
        //    IZome zome,
        //    bool checkIfSourcePathExists = true,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(await base.CreateAsync(avatarId, name, description, zomeType, fullPathToZomeSource, null,
        //        new STARZome()
        //        {
        //            ZomeType = zomeType,
        //            Zome = zome
        //        }, null, checkIfSourcePathExists,
        //    providerType));
        //}

        //public OASISResult<ISTARZome> CreateZome(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToZomeSource,
        //    ZomeType zomeType,
        //    IZome zome,
        //    bool checkIfSourcePathExists = true,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(base.Create(avatarId, name, description, zomeType, fullPathToZomeSource, null,
        //        new STARZome()
        //        {
        //            ZomeType = zomeType,
        //            Zome = zome
        //        }, null, checkIfSourcePathExists,
        //    providerType));
        //}

        //public async Task<OASISResult<ISTARZome>> CreateZomeAsync(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToZomeSource,
        //    ZomeType zomeType,
        //    Guid zomeId,
        //    bool checkIfSourcePathExists = true,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(await base.CreateAsync(avatarId, name, description, zomeType, fullPathToZomeSource, null,
        //        new STARZome()
        //        {
        //            ZomeType = zomeType,
        //            ZomeId = zomeId
        //        }, null, checkIfSourcePathExists,
        //    providerType));
        //}

        //public OASISResult<ISTARZome> CreateZome(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToZomeSource,
        //    ZomeType zomeType,
        //    Guid zomeId,
        //    bool checkIfSourcePathExists = true,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(base.Create(avatarId, name, description, zomeType, fullPathToZomeSource, null,
        //        new STARZome()
        //        {
        //            ZomeType = zomeType,
        //            ZomeId = zomeId
        //        }, null, checkIfSourcePathExists,
        //    providerType));
        //}

        //private OASISResult<ISTARZome> ProcessResult(OASISResult<STARZome> operationResult)
        //{
        //    OASISResult<ISTARZome> result = new OASISResult<ISTARZome>();
        //    result.Result = (ISTARZome)operationResult.Result;
        //    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
        //    return result;
        //}
    }
}