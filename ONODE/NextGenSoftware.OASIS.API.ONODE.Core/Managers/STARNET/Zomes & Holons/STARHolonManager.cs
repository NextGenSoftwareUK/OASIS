using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    //public class STARHolonManager : STARNETManagerBase<STARHolon, DownloadedHolon, InstalledHolon, HolonDNA>, ISTARHolonManager
    public class STARHolonManager : STARNETManagerBase<STARHolon, DownloadedHolon, InstalledHolon, STARNETDNA>, ISTARHolonManager
    {
        public STARHolonManager(Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(avatarId,
            STARDNA,
            OASISDNA,
            //typeof(HolonType),
            typeof(STARHolonType),
            HolonType.STARHolon,
            HolonType.InstalledHolon,
            "Holon",
            //"HolonId",
            "STARNETHolonId",
            "HolonName",
            "HolonType",
            "oholon",
            "oasis_holons",
            "HolonDNA.json",
            "STARHolonDNAJSON")
        { }

        public STARHolonManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            STARDNA,
            OASISDNA,
            //typeof(HolonType),
            typeof(STARHolonType),
            HolonType.STARHolon,
            HolonType.InstalledHolon,
            "Holon",
            //"HolonId",
            "STARNETHolonId",
            "HolonName",
            "HolonType",
            "oholon",
            "oasis_holons",
            "HolonDNA.json",
            "HolonDNAJSON")
        { }

        //public async Task<OASISResult<ISTARHolon>> CreateHolonAsync(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToHolonSource,
        //    HolonType holonType,
        //    IHolon holon,
        //    bool checkIfSourcePathExists = true,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(await base.CreateAsync(avatarId, name, description, holonType, fullPathToHolonSource, null,
        //        new STARHolon()
        //        {
        //            HolonType = holonType,
        //            Holon = holon
        //        }, null, checkIfSourcePathExists,
        //    providerType));
        //}

        //public OASISResult<ISTARHolon> CreateHolon(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToHolonSource,
        //    HolonType holonType,
        //    IHolon holon,
        //    bool checkIfSourcePathExists = true,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(base.Create(avatarId, name, description, holonType, fullPathToHolonSource, null,
        //        new STARHolon()
        //        {
        //            HolonType = holonType,
        //            Holon = holon
        //        }, null, checkIfSourcePathExists,
        //    providerType));
        //}

        //public async Task<OASISResult<ISTARHolon>> CreateHolonAsync(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToHolonSource,
        //    HolonType holonType,
        //    Guid holonId,
        //    bool checkIfSourcePathExists = true,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(await base.CreateAsync(avatarId, name, description, holonType, fullPathToHolonSource, null,
        //        new STARHolon()
        //        {
        //            HolonType = holonType,
        //            HolonId = holonId
        //        }, null, checkIfSourcePathExists,
        //    providerType));
        //}

        //public OASISResult<ISTARHolon> CreateHolon(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToHolonSource,
        //    HolonType holonType,
        //    Guid holonId,
        //    bool checkIfSourcePathExists = true,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(base.Create(avatarId, name, description, holonType, fullPathToHolonSource, null,
        //        new STARHolon()
        //        {
        //            HolonType = holonType,
        //            HolonId = holonId
        //        }, null, checkIfSourcePathExists,
        //    providerType));
        //}

        //private OASISResult<ISTARHolon> ProcessResult(OASISResult<STARHolon> operationResult)
        //{
        //    OASISResult<ISTARHolon> result = new OASISResult<ISTARHolon>();
        //    result.Result = operationResult.Result;
        //    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
        //    return result;
        //}
    }
}