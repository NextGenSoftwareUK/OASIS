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
    //public class CelestialBodyManager : STARNETManagerBase<STARCelestialBody, DownloadedCelestialBody, InstalledCelestialBody, CelestialBodyDNA>, ICelestialBodyManager
    public class CelestialBodyManager : STARNETManagerBase<STARCelestialBody, DownloadedCelestialBody, InstalledCelestialBody, STARNETDNA>, ICelestialBodyManager
    {
        public CelestialBodyManager(Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(avatarId,
            STARDNA,
            OASISDNA,
            typeof(CelestialBodyType),
            HolonType.STARCelestialBody,
            HolonType.InstalledCelestialBody,
            "CelestialBody",
            //"CelestialBodyId",
            "STARNETHolonId",
            "CelestialBodyName",
            "CelestialBodyType",
            "ocelestialbody",
            "oasis_celestialbodies",
            "CelestialBodyDNA.json",
            "STARCelestialBodyDNAJSON")
        { }

        public CelestialBodyManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            STARDNA,
            OASISDNA,
            typeof(CelestialBodyType),
            HolonType.STARCelestialBody,
            HolonType.InstalledCelestialBody,
            "CelestialBody",
            //"CelestialBodyId",
            "STARNETHolonId",
            "CelestialBodyName",
            "CelestialBodyType",
            "ocelestialbody",
            "oasis_celestialbodies",
            "CelestialBodyDNA.json",
            "STARCelestialBodyDNAJSON")
        { }

        //public async Task<OASISResult<ISTARCelestialBody>> CreateCelestialBodyAsync(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToCelestialBodySource,
        //    CelestialBodyType celestialBodyType,
        //    ICelestialBody celestialBody,
        //    bool checkIfSourcePathExists,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(await base.CreateAsync(avatarId, name, description, celestialBodyType, fullPathToCelestialBodySource, null,
        //        new STARCelestialBody()
        //        {
        //            CelestialBodyType = celestialBodyType,
        //            CelestialBody = celestialBody
        //        }, null,
        //    checkIfSourcePathExists, providerType));
        //}

        //public OASISResult<ISTARCelestialBody> CreateCelestialBody(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToCelestialBodySource,
        //    CelestialBodyType celestialBodyType,
        //    ICelestialBody celestialBody,
        //    bool checkIfSourcePathExists,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(base.Create(avatarId, name, description, celestialBodyType, fullPathToCelestialBodySource, null,
        //        new STARCelestialBody()
        //        {
        //            CelestialBodyType = celestialBodyType,
        //            CelestialBody = celestialBody
        //        }, null,
        //    checkIfSourcePathExists, providerType));
        //}

        //public async Task<OASISResult<ISTARCelestialBody>> CreateCelestialBodyAsync(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToCelestialBodySource,
        //    CelestialBodyType celestialBodyType,
        //    Guid celestialBodyId,
        //    bool checkIfSourcePathExists,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(await base.CreateAsync(avatarId, name, description, celestialBodyType, fullPathToCelestialBodySource, null,
        //        new STARCelestialBody()
        //        {
        //            CelestialBodyType = celestialBodyType,
        //            CelestialBodyId = celestialBodyId
        //        }, null,
        //    checkIfSourcePathExists, providerType));
        //}

        //public OASISResult<ISTARCelestialBody> CreateCelestialBody(
        //    Guid avatarId,
        //    string name,
        //    string description,
        //    string fullPathToCelestialBodySource,
        //    CelestialBodyType celestialBodyType,
        //    Guid celestialBodyId,
        //    bool checkIfSourcePathExists,
        //    ProviderType providerType = ProviderType.Default)
        //{
        //    return ProcessResult(base.Create(avatarId, name, description, celestialBodyType, fullPathToCelestialBodySource, null,
        //        new STARCelestialBody()
        //        {
        //            CelestialBodyType = celestialBodyType,
        //            CelestialBodyId = celestialBodyId
        //        },
        //    null, checkIfSourcePathExists, providerType));
        //}

        //private OASISResult<ISTARCelestialBody> ProcessResult(OASISResult<STARCelestialBody> operationResult)
        //{
        //    OASISResult<ISTARCelestialBody> result = new OASISResult<ISTARCelestialBody>();
        //    result.Result = (ISTARCelestialBody)operationResult.Result;
        //    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(operationResult, result);
        //    return result;
        //}
    }
}