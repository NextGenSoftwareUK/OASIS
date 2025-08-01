﻿using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class STARHolonManager : STARNETManagerBase<STARHolon, DownloadedHolon, InstalledHolon, HolonDNA>, ISTARHolonManager
    {
        public STARHolonManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId,
            OASISDNA,
            typeof(HolonType),
            HolonType.STARHolon,
            HolonType.InstalledHolon,
            "Holon",
            "HolonId",
            "HolonName",
            "HolonType",
            "oholon",
            "oasis_holons",
            "HolonDNA.json",
            "STARHolonDNAJSON")
        { }

        public STARHolonManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            OASISDNA,
            typeof(HolonType),
            HolonType.STARHolon,
            HolonType.InstalledHolon,
            "Holon",
            "HolonId",
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