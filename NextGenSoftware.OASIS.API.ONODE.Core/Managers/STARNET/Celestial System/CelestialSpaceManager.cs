using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    //public class CelestialSpaceManager : STARNETManagerBase<STARCelestialSpace, DownloadedCelestialSpace, InstalledCelestialSpace, CelestialSpaceDNA>, ICelestialSpaceManager
    public class CelestialSpaceManager : STARNETManagerBase<STARCelestialSpace, DownloadedCelestialSpace, InstalledCelestialSpace, STARNETDNA>//, ICelestialSpaceManager
    {
        public CelestialSpaceManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId,
            OASISDNA,
            typeof(CelestialSpaceType),
            HolonType.STARCelestialSpace,
            HolonType.InstalledCelestialSpace,
            "CelestialSpace",
            //"CelestialSpaceId",
            "STARNETHolonId",
            "CelestialSpaceName",
            "CelestialSpaceType",
            "ocelestialspace",
            "oasis_celestialspaces",
            "CelestialSpaceDNA.json",
            "STARCelestialSpaceDNAJSON")
        { }

        public CelestialSpaceManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            OASISDNA,
            typeof(CelestialSpaceType),
            HolonType.STARCelestialSpace,
            HolonType.InstalledCelestialSpace,
            "CelestialSpace",
            //"CelestialSpaceId",
            "STARNETHolonId",
            "CelestialSpaceName",
            "CelestialSpaceType",
            "ocelestialspace",
            "oasis_celestialspaces",
            "CelestialSpaceDNA.json",
            "STARCelestialSpaceDNAJSON")
        { }

        //public async Task<OASISResult<ISTARCelestialSpace>> CreateCelestialSpaceAsync(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToCelestialSpaceSource,
        //    CelestialSpaceType celestialSpaceType,
        //    ICelestialSpace celestialSpace,
        //    bool checkIfSourcePathExists = true,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(await base.CreateAsync(avatarId, name, description, celestialSpaceType, fullPathToCelestialSpaceSource, null,
        //        new STARCelestialSpace()
        //        {
        //            CelestialSpaceType = celestialSpaceType,
        //            CelestialSpace = celestialSpace
        //        }, null, checkIfSourcePathExists: checkIfSourcePathExists,
        //    providerType));
        //}

        //public OASISResult<ISTARCelestialSpace> CreateCelestialSpace(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToCelestialSpaceSource,
        //    CelestialSpaceType celestialSpaceType,
        //    ICelestialSpace celestialSpace,
        //    bool checkIfSourcePathExists = true,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(base.Create(avatarId, name, description, celestialSpaceType, fullPathToCelestialSpaceSource, null,
        //        new STARCelestialSpace()
        //        {
        //            CelestialSpaceType = celestialSpaceType,
        //            CelestialSpace = celestialSpace
        //        }, null,
        //        checkIfSourcePathExists: checkIfSourcePathExists,
        //    providerType));
        //}

        //public async Task<OASISResult<ISTARCelestialSpace>> CreateCelestialSpaceAsync(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToCelestialSpaceSource,
        //    CelestialSpaceType celestialSpaceType,
        //    Guid celestialSpaceId,
        //    bool checkIfSourcePathExists = true,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(await base.CreateAsync(avatarId, name, description, celestialSpaceType, fullPathToCelestialSpaceSource, null,
        //        new STARCelestialSpace()
        //        {
        //            CelestialSpaceType = celestialSpaceType,
        //            CelestialSpaceId = celestialSpaceId
        //        },
        //        null, checkIfSourcePathExists: checkIfSourcePathExists,
        //    providerType));
        //}

        //public OASISResult<ISTARCelestialSpace> CreateCelestialSpace(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToCelestialSpaceSource,
        //    CelestialSpaceType celestialSpaceType,
        //    Guid celestialSpaceId,
        //    bool checkIfSourcePathExists = true,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(base.Create(avatarId, name, description, celestialSpaceType, fullPathToCelestialSpaceSource, null,
        //        new STARCelestialSpace()
        //        {
        //            CelestialSpaceType = celestialSpaceType,
        //            CelestialSpaceId = celestialSpaceId
        //        },
        //        null, checkIfSourcePathExists: checkIfSourcePathExists,
        //    providerType));
        //}

        //private OASISResult<ISTARCelestialSpace> ProcessResult(OASISResult<STARCelestialSpace> operationResult)
        //{
        //    OASISResult<ISTARCelestialSpace> result = new OASISResult<ISTARCelestialSpace>();
        //    result.Result = (ISTARCelestialSpace)operationResult.Result;
        //    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
        //    return result;
        //}
    }
}